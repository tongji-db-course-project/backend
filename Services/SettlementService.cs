using System.Data;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SettlementService : ISettlementService
{
    private readonly AppDbContext _db;
    public SettlementService(AppDbContext db) => _db = db;

    public async Task<PageResult<SettlementDto>> ListAsync(int page, int size, string? keyword, string? status, int? supplierId)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var query = _db.SUPPLIER_SETTLEMENTs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.SUPPLIER.SUPPLIER_NAME.Contains(value) || x.PURCHASE.ORDER_CODE.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.STATUS == status.Trim());
        if (supplierId.HasValue) query = query.Where(x => x.SUPPLIER_ID == supplierId.Value);
        var total = await query.CountAsync();
        var list = await Project(query.OrderByDescending(x => x.SETTLEMENT_DATE).ThenByDescending(x => x.SETTLEMENT_ID))
            .Skip((page - 1) * size).Take(size).ToListAsync();
        MarkOverdue(list);
        return new PageResult<SettlementDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<SettlementDto> GetAsync(int settlementId)
    {
        var result = await Project(_db.SUPPLIER_SETTLEMENTs.AsNoTracking().Where(x => x.SETTLEMENT_ID == settlementId)).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("结算记录不存在");
        MarkOverdue(new[] { result });
        return result;
    }

    public async Task<SettlementDto> CreateAsync(CreateSettlementRequest request)
    {
        var purchase = await _db.PURCHASE_ORDERs.AsNoTracking().FirstOrDefaultAsync(x => x.ORDER_ID == request.purchaseId)
            ?? throw new KeyNotFoundException("采购单不存在");
        if (purchase.SUPPLIER_ID != request.supplierId) throw new ArgumentException("采购单与供应商不匹配");
        if (request.paidAmount > request.settlementAmount) throw new ArgumentException("已付金额不能超过结算金额");
        if (await _db.SUPPLIER_SETTLEMENTs.AnyAsync(x => x.PURCHASE_ID == request.purchaseId))
            throw new InvalidOperationException("该采购单已生成结算记录");
        var record = new SUPPLIER_SETTLEMENT
        {
            SUPPLIER_ID = request.supplierId, PURCHASE_ID = request.purchaseId,
            SETTLEMENT_DATE = request.settlementDate ?? DateTime.Today,
            SETTLEMENT_AMOUNT = request.settlementAmount, PAID_AMOUNT = request.paidAmount,
            UNPAID_AMOUNT = request.settlementAmount - request.paidAmount,
            STATUS = Status(request.settlementAmount, request.paidAmount), REMARK = request.remark?.Trim()
        };
        _db.SUPPLIER_SETTLEMENTs.Add(record);
        await _db.SaveChangesAsync();
        return await GetAsync(record.SETTLEMENT_ID);
    }

    public async Task<SettlementDto> PayAsync(int settlementId, PaySettlementRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM SUPPLIER_SETTLEMENT WHERE SETTLEMENT_ID = {0} FOR UPDATE", settlementId);
        var record = await _db.SUPPLIER_SETTLEMENTs.FirstOrDefaultAsync(x => x.SETTLEMENT_ID == settlementId)
            ?? throw new KeyNotFoundException("结算记录不存在");
        var paid = (record.PAID_AMOUNT ?? 0) + request.paidAmount;
        if (paid > record.SETTLEMENT_AMOUNT) throw new InvalidOperationException("累计付款金额不能超过应付金额");
        record.PAID_AMOUNT = paid;
        record.UNPAID_AMOUNT = record.SETTLEMENT_AMOUNT - paid;
        record.STATUS = Status(record.SETTLEMENT_AMOUNT, paid);
        if (!string.IsNullOrWhiteSpace(request.remark)) record.REMARK = request.remark.Trim();
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(settlementId);
    }

    private static IQueryable<SettlementDto> Project(IQueryable<SUPPLIER_SETTLEMENT> query) => query.Select(x => new SettlementDto
    {
        settlementId = x.SETTLEMENT_ID, supplierId = x.SUPPLIER_ID, supplierName = x.SUPPLIER.SUPPLIER_NAME,
        purchaseId = x.PURCHASE_ID, purchaseCode = x.PURCHASE.ORDER_CODE, settlementDate = x.SETTLEMENT_DATE,
        dueDate = x.SETTLEMENT_DATE.HasValue ? x.SETTLEMENT_DATE.Value.AddDays(x.SUPPLIER.PAYMENT_CYCLE ?? 0) : null,
        settlementAmount = x.SETTLEMENT_AMOUNT, paidAmount = x.PAID_AMOUNT ?? 0, unpaidAmount = x.UNPAID_AMOUNT,
        status = x.STATUS, remark = x.REMARK
    });

    private static string Status(decimal total, decimal paid) => paid <= 0 ? "未结算" : paid >= total ? "已结算" : "部分结算";
    private static void MarkOverdue(IEnumerable<SettlementDto> items)
    {
        foreach (var item in items) item.overdue = item.unpaidAmount > 0 && item.dueDate.HasValue && item.dueDate.Value.Date < DateTime.Today;
    }
}
