using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 采购订单业务实现
/// 状态机：创建(待审批) → 审批通过(已审批) → 入库(已入库)
///                └→ 驳回(回到待审批可改) └→ 作废(已作废)
/// </summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    // 状态值，与数据库 CHECK 约束一致
    private const string StatusPendingApproval = "待审批";
    private const string StatusRejected = "已驳回";
    private const string StatusApproved = "已审批";
    private const string StatusStockedIn = "已入库";
    private const string StatusVoided = "已作废";

    private readonly AppDbContext _db;

    public PurchaseOrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<PurchaseOrderDto>> ListOrdersAsync(
        int page, int size, string? keyword, string? status, int? supplierId)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.PURCHASE_ORDERs
            .AsNoTracking()
            .AsQueryable();

        // 关键词：单据编码或供应商名称模糊匹配（对应前端"搜索单号、关键词"）
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(o =>
                o.ORDER_CODE.Contains(kw) ||
                (o.SUPPLIER != null && o.SUPPLIER.SUPPLIER_NAME.Contains(kw)));
        }

        // 状态精确过滤
        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(o => o.STATUS == st);
        }

        // 供应商过滤
        if (supplierId.HasValue)
        {
            query = query.Where(o => o.SUPPLIER_ID == supplierId.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.ORDER_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(o => new PurchaseOrderDto
            {
                orderId = o.ORDER_ID,
                orderCode = o.ORDER_CODE,
                supplierId = o.SUPPLIER_ID,
                supplierName = o.SUPPLIER != null ? o.SUPPLIER.SUPPLIER_NAME : null,
                purchaseDate = o.PURCHASE_DATE,
                totalAmount = o.TOTAL_AMOUNT,
                applicantId = o.APPLICANT_ID,
                approverId = o.APPROVER_ID,
                status = o.STATUS,
                createTime = o.CREATE_TIME,
                updateTime = o.UPDATE_TIME
                // details 列表接口不返回
            })
            .ToListAsync();

        return new PageResult<PurchaseOrderDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<PurchaseOrderDto> CreateOrderAsync(CreatePurchaseOrderRequest request)
    {
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.applicantId))
            throw new KeyNotFoundException($"申请人不存在：{request.applicantId}");

        // 校验供应商存在
        var supplierExists = await _db.SUPPLIERs.AnyAsync(s => s.SUPPLIER_ID == request.supplierId);
        if (!supplierExists)
        {
            throw new KeyNotFoundException($"供应商不存在：{request.supplierId}");
        }

        var details = request.details
            .DistinctBy(d => d.productId)
            .ToList();

        if (details.Count != request.details.Count)
            throw new ArgumentException("采购明细不能包含重复商品");

        if (details.Count == 0)
        {
            throw new ArgumentException("采购明细不能为空");
        }

        // 校验商品存在
        var productIds = details.Select(d => d.productId).ToList();
        var existingProducts = await _db.PRODUCTs
            .Where(p => productIds.Contains(p.PRODUCT_ID))
            .Select(p => p.PRODUCT_ID)
            .ToListAsync();
        var missingIds = productIds.Except(existingProducts).ToList();
        if (missingIds.Count > 0)
        {
            throw new ArgumentException($"商品不存在：{string.Join(",", missingIds)}");
        }
        var invalidSupplierProducts = await _db.PRODUCTs.AsNoTracking()
            .Where(p => productIds.Contains(p.PRODUCT_ID) && p.SUPPLIER_ID != request.supplierId)
            .Select(p => p.PRODUCT_ID).ToListAsync();
        if (invalidSupplierProducts.Count > 0)
            throw new ArgumentException($"商品不属于所选供应商：{string.Join(",", invalidSupplierProducts)}");

        var now = DateTime.Now;

        var order = new PURCHASE_ORDER
        {
            ORDER_CODE = await GenerateOrderCodeAsync(now),
            SUPPLIER_ID = request.supplierId,
            PURCHASE_DATE = request.purchaseDate ?? now.Date,
            TOTAL_AMOUNT = details.Sum(d => d.purchasePrice * d.purchaseQuantity),
            APPLICANT_ID = request.applicantId,
            STATUS = StatusPendingApproval, // 创建即待审批（与数据库 DEFAULT 一致）
            CREATE_TIME = now,
            UPDATE_TIME = now
        };

        _db.PURCHASE_ORDERs.Add(order);
        await _db.SaveChangesAsync(); // 先保存拿 ORDER_ID

        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "采购单",
            ORDER_ID = order.ORDER_ID,
            OLD_STATUS = null,
            NEW_STATUS = StatusPendingApproval,
            OPERATOR_ID = request.applicantId,
            CHANGE_TIME = now,
            REMARK = string.IsNullOrWhiteSpace(request.remark) ? "创建并提交采购申请" : request.remark.Trim()
        });

        foreach (var d in details)
        {
            _db.PURCHASE_ORDER_DETAILs.Add(new PURCHASE_ORDER_DETAIL
            {
                PURCHASE_ID = order.ORDER_ID,
                PRODUCT_ID = d.productId,
                PURCHASE_QUANTITY = d.purchaseQuantity,
                PURCHASE_PRICE = d.purchasePrice
            });
        }

        await _db.SaveChangesAsync();
        return await GetOrderAsync(order.ORDER_ID);
    }

    public async Task<PurchaseOrderDto> GetOrderAsync(int orderId)
    {
        var order = await _db.PURCHASE_ORDERs
            .AsNoTracking()
            .Where(o => o.ORDER_ID == orderId)
            .Select(o => new PurchaseOrderDto
            {
                orderId = o.ORDER_ID,
                orderCode = o.ORDER_CODE,
                supplierId = o.SUPPLIER_ID,
                supplierName = o.SUPPLIER != null ? o.SUPPLIER.SUPPLIER_NAME : null,
                purchaseDate = o.PURCHASE_DATE,
                totalAmount = o.TOTAL_AMOUNT,
                applicantId = o.APPLICANT_ID,
                approverId = o.APPROVER_ID,
                status = o.STATUS,
                createTime = o.CREATE_TIME,
                updateTime = o.UPDATE_TIME,
                details = o.PURCHASE_ORDER_DETAILs
                    .Select(d => new PurchaseOrderDetailDto
                    {
                        productId = d.PRODUCT_ID,
                        productName = d.PRODUCT.PRODUCT_NAME,
                        purchaseQuantity = d.PURCHASE_QUANTITY,
                        purchasePrice = d.PURCHASE_PRICE,
                        lineTotal = d.PURCHASE_QUANTITY * d.PURCHASE_PRICE
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (order is null)
        {
            throw new KeyNotFoundException($"采购订单不存在：{orderId}");
        }

        return order;
    }

    public async Task<PurchaseOrderDto> UpdateOrderAsync(int orderId, CreatePurchaseOrderRequest request)
    {
        var order = await _db.PURCHASE_ORDERs
            .Include(o => o.PURCHASE_ORDER_DETAILs)
            .FirstOrDefaultAsync(o => o.ORDER_ID == orderId);
        if (order is null)
        {
            throw new KeyNotFoundException($"采购订单不存在：{orderId}");
        }

        // 待审批或已驳回状态可修改
        if (order.STATUS is not (StatusPendingApproval or StatusRejected))
        {
            throw new InvalidOperationException($"当前状态({order.STATUS})不允许修改，仅待审批或已驳回可修改");
        }

        var supplierExists = await _db.SUPPLIERs.AnyAsync(s => s.SUPPLIER_ID == request.supplierId);
        if (!supplierExists)
        {
            throw new KeyNotFoundException($"供应商不存在：{request.supplierId}");
        }

        var details = request.details
            .DistinctBy(d => d.productId)
            .ToList();
        if (details.Count != request.details.Count)
            throw new ArgumentException("采购明细不能包含重复商品");
        if (details.Count == 0)
        {
            throw new ArgumentException("采购明细不能为空");
        }

        var productIds = details.Select(d => d.productId).ToList();
        var existingProducts = await _db.PRODUCTs
            .Where(p => productIds.Contains(p.PRODUCT_ID))
            .Select(p => p.PRODUCT_ID)
            .ToListAsync();
        var missingIds = productIds.Except(existingProducts).ToList();
        if (missingIds.Count > 0)
        {
            throw new ArgumentException($"商品不存在：{string.Join(",", missingIds)}");
        }
        var invalidSupplierProducts = await _db.PRODUCTs.AsNoTracking()
            .Where(p => productIds.Contains(p.PRODUCT_ID) && p.SUPPLIER_ID != request.supplierId)
            .Select(p => p.PRODUCT_ID).ToListAsync();
        if (invalidSupplierProducts.Count > 0)
            throw new ArgumentException($"商品不属于所选供应商：{string.Join(",", invalidSupplierProducts)}");

        // 更新主表
        order.SUPPLIER_ID = request.supplierId;
        order.PURCHASE_DATE = request.purchaseDate;
        order.TOTAL_AMOUNT = details.Sum(d => d.purchasePrice * d.purchaseQuantity);
        order.UPDATE_TIME = DateTime.Now;

        // 已驳回的单据修改保存后自动回到待审批，重新进入审批流
        if (order.STATUS == StatusRejected)
        {
            order.STATUS = StatusPendingApproval;
            _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
            {
                ORDER_TYPE = "采购单",
                ORDER_ID = orderId,
                OLD_STATUS = StatusRejected,
                NEW_STATUS = StatusPendingApproval,
                OPERATOR_ID = request.applicantId,
                CHANGE_TIME = DateTime.Now,
                REMARK = "修改后重新提交审批"
            });
        }

        // 替换明细：删旧插新
        _db.PURCHASE_ORDER_DETAILs.RemoveRange(order.PURCHASE_ORDER_DETAILs);
        foreach (var d in details)
        {
            _db.PURCHASE_ORDER_DETAILs.Add(new PURCHASE_ORDER_DETAIL
            {
                PURCHASE_ID = orderId,
                PRODUCT_ID = d.productId,
                PURCHASE_QUANTITY = d.purchaseQuantity,
                PURCHASE_PRICE = d.purchasePrice
            });
        }

        await _db.SaveChangesAsync();
        return await GetOrderAsync(orderId);
    }

    public async Task CancelOrderAsync(int orderId)
    {
        var order = await _db.PURCHASE_ORDERs.FirstOrDefaultAsync(o => o.ORDER_ID == orderId);
        if (order is null)
        {
            throw new KeyNotFoundException($"采购订单不存在：{orderId}");
        }

        if (order.STATUS == StatusStockedIn)
        {
            throw new InvalidOperationException("已入库的采购订单不能作废");
        }

        if (order.STATUS == StatusVoided)
        {
            throw new InvalidOperationException("采购订单已作废，请勿重复操作");
        }

        var oldStatus = order.STATUS;
        var now = DateTime.Now;
        order.STATUS = StatusVoided;
        order.UPDATE_TIME = now;
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "采购单",
            ORDER_ID = orderId,
            OLD_STATUS = oldStatus,
            NEW_STATUS = StatusVoided,
            CHANGE_TIME = now,
            REMARK = "作废采购单"
        });
        await _db.SaveChangesAsync();
    }

    public async Task<OrderStatusResultDto> SubmitOrderAsync(int orderId)
    {
        var order = await GetOrderForStatusChangeAsync(orderId);
        if (order.STATUS is not (StatusPendingApproval or StatusRejected))
            throw new InvalidOperationException($"当前状态({order.STATUS})不允许提交");

        var now = DateTime.Now;
        var oldStatus = order.STATUS;
        var alreadySubmitted = oldStatus == StatusPendingApproval && await _db.ORDER_STATUS_LOGs.AnyAsync(x =>
            x.ORDER_TYPE == "采购单" && x.ORDER_ID == orderId && x.NEW_STATUS == StatusPendingApproval);
        if (!alreadySubmitted)
        {
            order.STATUS = StatusPendingApproval;
            order.UPDATE_TIME = now;
            _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
            {
                ORDER_TYPE = "采购单",
                ORDER_ID = orderId,
                OLD_STATUS = oldStatus,
                NEW_STATUS = StatusPendingApproval,
                OPERATOR_ID = order.APPLICANT_ID,
                CHANGE_TIME = now,
                REMARK = "提交采购申请"
            });
            await _db.SaveChangesAsync();
        }
        return new OrderStatusResultDto
        {
            orderId = order.ORDER_ID,
            orderCode = order.ORDER_CODE,
            status = StatusPendingApproval,
            operatorId = order.APPLICANT_ID,
            changeTime = now
        };
    }

    public async Task<OrderStatusResultDto> ApproveOrderAsync(int orderId, ApprovalRequest request)
    {
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.approverId))
            throw new KeyNotFoundException($"审批人不存在：{request.approverId}");
        var order = await GetOrderForStatusChangeAsync(orderId);

        if (order.STATUS != StatusPendingApproval)
        {
            throw new InvalidOperationException($"当前状态({order.STATUS})不允许审批通过，仅待审批可审批");
        }

        order.STATUS = StatusApproved;
        order.APPROVER_ID = request.approverId;
        order.UPDATE_TIME = DateTime.Now;

        var now = DateTime.Now;
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "采购单",
            ORDER_ID = orderId,
            OLD_STATUS = StatusPendingApproval,
            NEW_STATUS = StatusApproved,
            OPERATOR_ID = request.approverId,
            CHANGE_TIME = now,
            REMARK = request.remark
        });

        await _db.SaveChangesAsync();

        return new OrderStatusResultDto
        {
            orderId = order.ORDER_ID,
            orderCode = order.ORDER_CODE,
            status = StatusApproved,
            operatorId = request.approverId,
            changeTime = now
        };
    }

    public async Task<OrderStatusResultDto> RejectOrderAsync(int orderId, ApprovalRequest request)
    {
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.approverId))
            throw new KeyNotFoundException($"审批人不存在：{request.approverId}");
        var order = await GetOrderForStatusChangeAsync(orderId);

        if (order.STATUS != StatusPendingApproval)
        {
            throw new InvalidOperationException($"当前状态({order.STATUS})不允许驳回，仅待审批可驳回");
        }

        order.STATUS = StatusRejected;
        order.APPROVER_ID = request.approverId;
        order.UPDATE_TIME = DateTime.Now;

        var now = DateTime.Now;
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "采购单",
            ORDER_ID = orderId,
            OLD_STATUS = StatusPendingApproval,
            NEW_STATUS = StatusRejected,
            OPERATOR_ID = request.approverId,
            CHANGE_TIME = now,
            REMARK = request.remark
        });

        await _db.SaveChangesAsync();

        return new OrderStatusResultDto
        {
            orderId = order.ORDER_ID,
            orderCode = order.ORDER_CODE,
            status = StatusRejected,
            operatorId = request.approverId,
            changeTime = now
        };
    }

    public async Task<PurchaseStockInResultDto> StockInAsync(int orderId, PurchaseStockInRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_ORDER WHERE ORDER_ID = {0} FOR UPDATE", orderId);
        var order = await _db.PURCHASE_ORDERs
            .Include(o => o.PURCHASE_ORDER_DETAILs)
            .FirstOrDefaultAsync(o => o.ORDER_ID == orderId);
        if (order is null)
        {
            throw new KeyNotFoundException($"采购订单不存在：{orderId}");
        }

        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.operatorId))
            throw new KeyNotFoundException($"入库操作人不存在：{request.operatorId}");

        if (order.STATUS != StatusApproved)
        {
            throw new InvalidOperationException($"当前状态({order.STATUS})不允许入库，仅已审批可入库");
        }

        // 仓库必须存在
        var warehouseExists = await _db.WAREHOUSEs.AnyAsync(w => w.WAREHOUSE_ID == request.warehouseId);
        if (!warehouseExists)
        {
            throw new KeyNotFoundException($"仓库不存在：{request.warehouseId}");
        }

        // 入库商品必须是订单明细中的商品
        var orderProductIds = order.PURCHASE_ORDER_DETAILs
            .Select(d => d.PRODUCT_ID)
            .ToList();
        var invalidIds = request.details
            .Select(d => d.productId)
            .Except(orderProductIds)
            .ToList();
        if (invalidIds.Count > 0)
        {
            throw new ArgumentException($"入库商品不在采购订单明细中：{string.Join(",", invalidIds)}");
        }

        var stockInQuantities = request.details
            .GroupBy(x => x.productId)
            .ToDictionary(x => x.Key, x => x.Sum(item => item.stockInQuantity));
        var orderedQuantities = order.PURCHASE_ORDER_DETAILs
            .ToDictionary(x => x.PRODUCT_ID, x => x.PURCHASE_QUANTITY ?? 0);
        if (stockInQuantities.Count != orderedQuantities.Count ||
            orderedQuantities.Any(x => !stockInQuantities.TryGetValue(x.Key, out var quantity) || quantity != x.Value))
            throw new ArgumentException("入库商品及数量必须与采购订单明细完全一致");

        var now = DateTime.Now;

        // 事务：加库存 + 记流水 + 生成结算 + 更新状态，保证原子性
        // 1. 处理每个入库商品的库存
        var stockProductIds = stockInQuantities.Keys.OrderBy(x => x).ToList();
        var inList = string.Join(",", stockProductIds);
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE",
            request.warehouseId);
        var inventories = await _db.INVENTORies
            .Where(i => request.details.Select(d => d.productId).Contains(i.PRODUCT_ID)
                        && i.WAREHOUSE_ID == request.warehouseId)
            .ToListAsync();

        foreach (var detail in request.details)
        {
            var inventory = inventories.FirstOrDefault(i => i.PRODUCT_ID == detail.productId);
            int newStock;
            if (inventory is null)
            {
                // 该仓库无此商品库存记录 → 新建
                inventory = new INVENTORY
                {
                    PRODUCT_ID = detail.productId,
                    WAREHOUSE_ID = request.warehouseId,
                    CURRENT_STOCK = detail.stockInQuantity,
                    LAST_UPDATE_TIME = now
                };
                _db.INVENTORies.Add(inventory);
                newStock = detail.stockInQuantity;
            }
            else
            {
                inventory.CURRENT_STOCK += detail.stockInQuantity;
                inventory.LAST_UPDATE_TIME = now;
                newStock = inventory.CURRENT_STOCK;
            }

            // 记库存流水（inventory_record 表无仓库字段）
            _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
            {
                PRODUCT_ID = detail.productId,
                RECORD_TYPE = "入库",
                SOURCE_NO = order.ORDER_CODE,
                CHANGE_QTY = detail.stockInQuantity,
                REMAIN_QTY = newStock,
                OPERATOR_ID = request.operatorId,
                RECORD_TIME = now,
                REMARK = request.remark
            });
        }

        // 2. 生成供应商结算（未结算：全额未付）
        _db.SUPPLIER_SETTLEMENTs.Add(new SUPPLIER_SETTLEMENT
        {
            SUPPLIER_ID = order.SUPPLIER_ID!.Value,
            PURCHASE_ID = orderId,
            SETTLEMENT_DATE = now,
            SETTLEMENT_AMOUNT = order.TOTAL_AMOUNT ?? 0,
            PAID_AMOUNT = 0,
            UNPAID_AMOUNT = order.TOTAL_AMOUNT ?? 0,
            STATUS = "未结算",
            REMARK = request.remark
        });

        // 3. 更新订单状态 + 状态日志
        order.STATUS = StatusStockedIn;
        order.UPDATE_TIME = now;

        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "采购单",
            ORDER_ID = orderId,
            OLD_STATUS = StatusApproved,
            NEW_STATUS = StatusStockedIn,
            OPERATOR_ID = request.operatorId,
            CHANGE_TIME = now,
            REMARK = request.remark
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new PurchaseStockInResultDto
        {
            orderId = order.ORDER_ID,
            orderCode = order.ORDER_CODE,
            status = StatusStockedIn,
            stockInTime = now,
            totalAmount = order.TOTAL_AMOUNT ?? 0
        };
    }

    public async Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int orderId)
    {
        if (!await _db.PURCHASE_ORDERs.AsNoTracking().AnyAsync(x => x.ORDER_ID == orderId))
            throw new KeyNotFoundException($"采购订单不存在：{orderId}");
        return await _db.ORDER_STATUS_LOGs.AsNoTracking()
            .Where(x => x.ORDER_TYPE == "采购单" && x.ORDER_ID == orderId)
            .OrderBy(x => x.CHANGE_TIME).ThenBy(x => x.LOG_ID)
            .Select(x => new OrderStatusLogDto
            {
                logId = x.LOG_ID,
                orderType = x.ORDER_TYPE,
                orderId = x.ORDER_ID,
                oldStatus = x.OLD_STATUS,
                newStatus = x.NEW_STATUS,
                operatorId = x.OPERATOR_ID,
                changeTime = x.CHANGE_TIME,
                remark = x.REMARK
            }).ToListAsync();
    }

    private async Task<PURCHASE_ORDER> GetOrderForStatusChangeAsync(int orderId)
    {
        var order = await _db.PURCHASE_ORDERs.FirstOrDefaultAsync(o => o.ORDER_ID == orderId);
        if (order is null)
        {
            throw new KeyNotFoundException($"采购订单不存在：{orderId}");
        }
        return order;
    }

    /// <summary>
    /// 生成单据编码：CG + yyyyMMdd + 4位当日序号，如 CG202608120001
    /// </summary>
    private async Task<string> GenerateOrderCodeAsync(DateTime date)
    {
        var prefix = "CG" + date.ToString("yyyyMMdd");
        var maxCode = await _db.PURCHASE_ORDERs
            .Where(o => o.ORDER_CODE.StartsWith(prefix))
            .OrderByDescending(o => o.ORDER_CODE)
            .Select(o => o.ORDER_CODE)
            .FirstOrDefaultAsync();

        int seq = 1;
        if (!string.IsNullOrEmpty(maxCode) &&
            int.TryParse(maxCode[prefix.Length..], out var maxSeq))
        {
            seq = maxSeq + 1;
        }

        return prefix + seq.ToString("D4");
    }
}
