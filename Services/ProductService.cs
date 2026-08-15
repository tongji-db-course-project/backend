using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 商品业务实现
/// </summary>
public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 构建商品查询的基础查询（Include 导航属性 + 投影到 DTO）
    /// </summary>
    private IQueryable<ProductListItemDto> BuildProductQuery()
    {
        return _db.PRODUCTs
            .AsNoTracking()
            .Include(p => p.CATEGORY)
            .Include(p => p.SUPPLIER)
            .Select(p => new ProductListItemDto
            {
                productId = p.PRODUCT_ID,
                productName = p.PRODUCT_NAME,
                barcode = p.BARCODE,
                specification = p.SPECIFICATION,
                purchasePrice = p.PURCHASE_PRICE,
                salePrice = p.SALE_PRICE,
                isPromotion = p.IS_PROMOTION,
                promotionPrice = p.PROMOTION_PRICE,
                stockWarning = p.STOCK_WARNING,
                unit = p.UNIT,
                status = p.STATUS,
                categoryId = p.CATEGORY_ID,
                categoryName = p.CATEGORY.CATEGORY_NAME,
                supplierId = p.SUPPLIER_ID,
                supplierName = p.SUPPLIER.SUPPLIER_NAME,
                currentStock = p.INVENTORies
                                    .Select(i => (int?)i.CURRENT_STOCK)
                                    .FirstOrDefault() ?? 0
            });
    }

    public async Task<PageResult<ProductListItemDto>> ListProductsAsync(
        int page, int size, string? keyword, string? status)
    {
        // 防御性校验
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.PRODUCTs
            .AsNoTracking()
            .Include(p => p.CATEGORY)
            .Include(p => p.SUPPLIER)
            .AsQueryable();

        // 关键词：商品名称或条码模糊匹配
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p =>
                p.PRODUCT_NAME.Contains(kw) ||
                (p.BARCODE != null && p.BARCODE.Contains(kw)));
        }

        // 状态精确过滤
        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(p => p.STATUS == st);
        }

        // 先取总数，再按主键排序分页
        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new ProductListItemDto
            {
                productId = p.PRODUCT_ID,
                productName = p.PRODUCT_NAME,
                barcode = p.BARCODE,
                specification = p.SPECIFICATION,
                purchasePrice = p.PURCHASE_PRICE,
                salePrice = p.SALE_PRICE,
                isPromotion = p.IS_PROMOTION,
                promotionPrice = p.PROMOTION_PRICE,
                stockWarning = p.STOCK_WARNING,
                unit = p.UNIT,
                status = p.STATUS,
                categoryId = p.CATEGORY_ID,
                categoryName = p.CATEGORY.CATEGORY_NAME,
                supplierId = p.SUPPLIER_ID,
                supplierName = p.SUPPLIER.SUPPLIER_NAME,
                // 一个商品对应一条库存记录，取其当前库存；无记录时为 0
                currentStock = p.INVENTORies
                                    .Select(i => (int?)i.CURRENT_STOCK)
                                    .FirstOrDefault() ?? 0
            })
            .ToListAsync();

        return new PageResult<ProductListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<ProductListItemDto?> GetProductByIdAsync(int productId)
    {
        return await BuildProductQuery()
            .FirstOrDefaultAsync(p => p.productId == productId);
    }

    public async Task<ProductListItemDto?> GetProductByBarcodeAsync(string barcode)
    {
        return await BuildProductQuery()
            .FirstOrDefaultAsync(p => p.barcode == barcode);
    }

    public async Task<PageResult<ProductListItemDto>> GetWarningStockProductsAsync(int page, int size)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        // 查询库存低于预警线的商品：当前库存 <= STOCK_WARNING 且 STOCK_WARNING 不为空
        var query = _db.PRODUCTs
            .AsNoTracking()
            .Include(p => p.CATEGORY)
            .Include(p => p.SUPPLIER)
            .Where(p => p.STOCK_WARNING != null)
            .Select(p => new
            {
                Product = p,
                CurrentStock = p.INVENTORies
                    .Select(i => (int?)i.CURRENT_STOCK)
                    .FirstOrDefault() ?? 0
            })
            .Where(x => x.CurrentStock <= x.Product.STOCK_WARNING)
            .Select(x => new ProductListItemDto
            {
                productId = x.Product.PRODUCT_ID,
                productName = x.Product.PRODUCT_NAME,
                barcode = x.Product.BARCODE,
                specification = x.Product.SPECIFICATION,
                purchasePrice = x.Product.PURCHASE_PRICE,
                salePrice = x.Product.SALE_PRICE,
                stockWarning = x.Product.STOCK_WARNING,
                unit = x.Product.UNIT,
                status = x.Product.STATUS,
                categoryId = x.Product.CATEGORY_ID,
                categoryName = x.Product.CATEGORY.CATEGORY_NAME,
                supplierId = x.Product.SUPPLIER_ID,
                supplierName = x.Product.SUPPLIER.SUPPLIER_NAME,
                currentStock = x.CurrentStock
            });

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.currentStock)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PageResult<ProductListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<ProductListItemDto> CreateProductAsync(ProductDto dto)
    {
        var productName = RequireText(dto.productName, "商品名称不能为空");

        // 验证条码唯一性
        if (!string.IsNullOrWhiteSpace(dto.barcode))
        {
            var barcode = dto.barcode.Trim();
            var exists = await _db.PRODUCTs
                .AsNoTracking()
                .AnyAsync(p => p.BARCODE == barcode);

            if (exists)
                throw new BusinessException(400, "商品条码已存在");
        }

        // 验证分类和供应商存在
        await EnsureCategoryExistsAsync(dto.categoryId);
        await EnsureSupplierExistsAsync(dto.supplierId);

        var product = new PRODUCT
        {
            PRODUCT_NAME = productName,
            CATEGORY_ID = dto.categoryId,
            SUPPLIER_ID = dto.supplierId,
            BARCODE = dto.barcode?.Trim(),
            SPECIFICATION = dto.specification?.Trim(),
            PURCHASE_PRICE = dto.purchasePrice,
            SALE_PRICE = dto.salePrice,
            STOCK_WARNING = dto.stockWarning,
            UNIT = dto.unit?.Trim(),
            STATUS = dto.status?.Trim() ?? "在售"
        };

        _db.PRODUCTs.Add(product);
        await SaveChangesAsync();

        // 返回新建的完整商品信息
        return (await GetProductByIdAsync(product.PRODUCT_ID))!;
    }

    public async Task<ProductListItemDto?> UpdateProductAsync(int productId, ProductDto dto)
    {
        var product = await _db.PRODUCTs
            .FirstOrDefaultAsync(p => p.PRODUCT_ID == productId);

        if (product == null)
            return null;

        // 更新非空字段
        if (dto.productName != null)
            product.PRODUCT_NAME = RequireText(dto.productName, "商品名称不能为空");

        if (dto.categoryId > 0)
        {
            await EnsureCategoryExistsAsync(dto.categoryId);
            product.CATEGORY_ID = dto.categoryId;
        }

        if (dto.supplierId > 0)
        {
            await EnsureSupplierExistsAsync(dto.supplierId);
            product.SUPPLIER_ID = dto.supplierId;
        }

        if (dto.barcode != null)
        {
            var barcode = dto.barcode.Trim();
            // 条码唯一性检查（排除自身）
            var exists = await _db.PRODUCTs
                .AsNoTracking()
                .AnyAsync(p => p.BARCODE == barcode && p.PRODUCT_ID != productId);

            if (exists)
                throw new BusinessException(400, "商品条码已存在");

            product.BARCODE = barcode.Length == 0 ? null : barcode;
        }

        if (dto.specification != null)
            product.SPECIFICATION = dto.specification.Trim();

        if (dto.purchasePrice.HasValue)
            product.PURCHASE_PRICE = dto.purchasePrice;

        if (dto.salePrice.HasValue)
            product.SALE_PRICE = dto.salePrice;

        if (dto.stockWarning.HasValue)
            product.STOCK_WARNING = dto.stockWarning;

        if (dto.unit != null)
            product.UNIT = dto.unit.Trim();

        if (dto.status != null)
            product.STATUS = dto.status.Trim();

        await SaveChangesAsync();

        return await GetProductByIdAsync(product.PRODUCT_ID);
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _db.PRODUCTs
            .FirstOrDefaultAsync(p => p.PRODUCT_ID == productId);

        if (product == null)
            return false;

        // 逻辑删除：改为「停售」
        product.STATUS = "停售";
        await _db.SaveChangesAsync();

        return true;
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var exists = await _db.PRODUCT_CATEGORies
            .AsNoTracking()
            .AnyAsync(c => c.CATEGORY_ID == categoryId);

        if (!exists)
            throw new BusinessException(400, "商品分类不存在");
    }

    private async Task EnsureSupplierExistsAsync(int supplierId)
    {
        var exists = await _db.SUPPLIERs
            .AsNoTracking()
            .AnyAsync(s => s.SUPPLIER_ID == supplierId);

        if (!exists)
            throw new BusinessException(400, "供应商不存在");
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new BusinessException(400, "商品信息不符合业务规则");
        }
    }

    private static string RequireText(string? value, string message)
    {
        var text = NormalizeOptional(value);
        if (text == null)
            throw new BusinessException(400, message);

        return text;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value == null)
            return null;

        var text = value.Trim();
        return text.Length == 0 ? null : text;
    }
}