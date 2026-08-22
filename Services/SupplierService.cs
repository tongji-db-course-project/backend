using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    public SupplierService(AppDbContext db) => _db = db;

    public async Task<PageResult<SupplierDto>> ListAsync(int page, int size, string? keyword, string? status)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);
        var query = _db.SUPPLIERs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.SUPPLIER_NAME.Contains(value) ||
                (x.CONTACT_NAME != null && x.CONTACT_NAME.Contains(value)) ||
                (x.PHONE != null && x.PHONE.Contains(value)));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.STATUS == status.Trim());
        var total = await query.CountAsync();
        var list = await Project(query.OrderBy(x => x.SUPPLIER_ID))
            .Skip((page - 1) * size).Take(size).ToListAsync();
        return new PageResult<SupplierDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<SupplierDto> GetAsync(int supplierId) =>
        await Project(_db.SUPPLIERs.AsNoTracking().Where(x => x.SUPPLIER_ID == supplierId)).FirstOrDefaultAsync()
        ?? throw new KeyNotFoundException("供应商不存在");

    public async Task<SupplierDto> CreateAsync(SaveSupplierRequest request)
    {
        var name = RequireName(request.supplierName);
        await EnsureNameUniqueAsync(name, null);
        var supplier = new SUPPLIER();
        Apply(supplier, request, name);
        _db.SUPPLIERs.Add(supplier);
        await _db.SaveChangesAsync();
        return await GetAsync(supplier.SUPPLIER_ID);
    }

    public async Task<SupplierDto> UpdateAsync(int supplierId, SaveSupplierRequest request)
    {
        var supplier = await _db.SUPPLIERs.FirstOrDefaultAsync(x => x.SUPPLIER_ID == supplierId)
            ?? throw new KeyNotFoundException("供应商不存在");
        var name = RequireName(request.supplierName);
        await EnsureNameUniqueAsync(name, supplierId);
        Apply(supplier, request, name);
        await _db.SaveChangesAsync();
        return await GetAsync(supplierId);
    }

    public async Task DeleteAsync(int supplierId)
    {
        var supplier = await _db.SUPPLIERs.FirstOrDefaultAsync(x => x.SUPPLIER_ID == supplierId)
            ?? throw new KeyNotFoundException("供应商不存在");
        supplier.STATUS = "禁用";
        await _db.SaveChangesAsync();
    }

    public async Task<PageResult<ProductListItemDto>> ListProductsAsync(int supplierId, int page, int size)
    {
        if (!await _db.SUPPLIERs.AsNoTracking().AnyAsync(x => x.SUPPLIER_ID == supplierId))
            throw new KeyNotFoundException("供应商不存在");
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);
        var query = _db.PRODUCTs.AsNoTracking().Where(x => x.SUPPLIER_ID == supplierId);
        var total = await query.CountAsync();
        var list = await query.OrderBy(x => x.PRODUCT_ID).Skip((page - 1) * size).Take(size)
            .Select(x => new ProductListItemDto
            {
                productId = x.PRODUCT_ID, productName = x.PRODUCT_NAME, barcode = x.BARCODE,
                specification = x.SPECIFICATION, purchasePrice = x.PURCHASE_PRICE, salePrice = x.SALE_PRICE,
                stockWarning = x.STOCK_WARNING, unit = x.UNIT, status = x.STATUS,
                categoryId = x.CATEGORY_ID, categoryName = x.CATEGORY.CATEGORY_NAME,
                supplierId = x.SUPPLIER_ID, supplierName = x.SUPPLIER.SUPPLIER_NAME,
                isPromotion = x.IS_PROMOTION, promotionPrice = x.PROMOTION_PRICE,
                currentStock = x.INVENTORies.Sum(i => (int?)i.CURRENT_STOCK) ?? 0
            }).ToListAsync();
        return new PageResult<ProductListItemDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<SupplierPerformanceDto> GetPerformanceAsync(int supplierId, bool updateCreditLevel)
    {
        var supplier = await _db.SUPPLIERs.FirstOrDefaultAsync(x => x.SUPPLIER_ID == supplierId)
            ?? throw new KeyNotFoundException("供应商不存在");
        const int onTimeThresholdDays = 7;
        var stockedOrders = await _db.PURCHASE_ORDERs.AsNoTracking()
            .Where(x => x.SUPPLIER_ID == supplierId && x.STATUS == "已入库")
            .Select(x => new { x.ORDER_ID, x.PURCHASE_DATE })
            .ToListAsync();
        var stockedOrderIds = stockedOrders.Select(x => x.ORDER_ID).ToList();
        var stockInTimes = await _db.ORDER_STATUS_LOGs.AsNoTracking()
            .Where(x => x.ORDER_TYPE == "采购单" && stockedOrderIds.Contains(x.ORDER_ID) && x.NEW_STATUS == "已入库")
            .GroupBy(x => x.ORDER_ID)
            .Select(x => new { orderId = x.Key, stockInTime = x.Min(y => y.CHANGE_TIME) })
            .ToDictionaryAsync(x => x.orderId, x => x.stockInTime);
        var stocked = stockedOrders.Count;
        var onTimeCount = stockedOrders.Count(x => x.PURCHASE_DATE.HasValue &&
            stockInTimes.TryGetValue(x.ORDER_ID, out var stockInTime) &&
            stockInTime.HasValue && stockInTime.Value <= x.PURCHASE_DATE.Value.Date.AddDays(onTimeThresholdDays + 1));
        var returned = await _db.PURCHASE_RETURN_ORDERs.CountAsync(x => x.SUPPLIER_ID == supplierId && x.STATUS != "已作废");
        var returnRate = stocked == 0 ? 0 : Math.Round((decimal)returned / stocked, 4);
        var onTimeRate = stocked == 0 ? 0 : Math.Round((decimal)onTimeCount / stocked, 4);
        var level = onTimeRate >= 0.95m && returnRate <= 0.02m ? "A" :
            onTimeRate >= 0.85m && returnRate <= 0.05m ? "B" :
            onTimeRate >= 0.70m && returnRate <= 0.10m ? "C" : "D";
        if (updateCreditLevel && supplier.CREDIT_LEVEL != level)
        {
            supplier.CREDIT_LEVEL = level;
            await _db.SaveChangesAsync();
        }
        return new SupplierPerformanceDto
        {
            supplierId = supplierId, supplierName = supplier.SUPPLIER_NAME, stockedOrderCount = stocked,
            returnedOrderCount = returned, returnRate = returnRate, onTimeRate = onTimeRate, creditLevel = level
        };
    }

    private static IQueryable<SupplierDto> Project(IQueryable<SUPPLIER> query) => query.Select(x => new SupplierDto
    {
        supplierId = x.SUPPLIER_ID, supplierName = x.SUPPLIER_NAME, contactPerson = x.CONTACT_NAME,
        phone = x.PHONE, email = x.EMAIL, address = x.ADDRESS, creditLevel = x.CREDIT_LEVEL,
        paymentCycle = x.PAYMENT_CYCLE, minOrderQty = x.MIN_ORDER_QTY, bankName = x.BANK_NAME,
        bankAccount = x.BANK_ACCOUNT, status = x.STATUS
    });

    private async Task EnsureNameUniqueAsync(string name, int? excludedId)
    {
        if (await _db.SUPPLIERs.AnyAsync(x => x.SUPPLIER_NAME == name && (!excludedId.HasValue || x.SUPPLIER_ID != excludedId)))
            throw new InvalidOperationException("供应商名称已存在");
    }

    private static string RequireName(string value)
    {
        var name = value?.Trim();
        return string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("供应商名称不能为空") : name;
    }

    private static void Apply(SUPPLIER supplier, SaveSupplierRequest request, string name)
    {
        supplier.SUPPLIER_NAME = name;
        supplier.CONTACT_NAME = request.contactPerson?.Trim(); supplier.PHONE = request.phone?.Trim();
        supplier.EMAIL = request.email?.Trim(); supplier.ADDRESS = request.address?.Trim();
        supplier.CREDIT_LEVEL = request.creditLevel?.Trim(); supplier.PAYMENT_CYCLE = request.paymentCycle;
        supplier.MIN_ORDER_QTY = request.minOrderQty; supplier.BANK_NAME = request.bankName?.Trim();
        supplier.BANK_ACCOUNT = request.bankAccount?.Trim(); supplier.STATUS = request.status?.Trim() ?? supplier.STATUS ?? "启用";
    }
}
