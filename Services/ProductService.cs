using System.Linq.Expressions;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace backend.Services;

public class ProductService : IProductService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "在售",
        "下架",
        "停售"
    };

    private static readonly Expression<Func<PRODUCT, Product>> ProductProjection = product => new Product
    {
        ProductId = product.PRODUCT_ID,
        CategoryId = product.CATEGORY_ID,
        SupplierId = product.SUPPLIER_ID,
        ProductName = product.PRODUCT_NAME,
        Barcode = product.BARCODE,
        Specification = product.SPECIFICATION,
        PurchasePrice = product.PURCHASE_PRICE,
        SalePrice = product.SALE_PRICE,
        StockWarning = product.STOCK_WARNING,
        Unit = product.UNIT,
        Status = product.STATUS
    };

    private static readonly Expression<Func<PRODUCT, ProductListItemDto>> ProductListProjection = product => new ProductListItemDto
    {
        ProductId = product.PRODUCT_ID,
        ProductName = product.PRODUCT_NAME,
        Barcode = product.BARCODE,
        Specification = product.SPECIFICATION,
        PurchasePrice = product.PURCHASE_PRICE,
        SalePrice = product.SALE_PRICE,
        StockWarning = product.STOCK_WARNING,
        Unit = product.UNIT,
        Status = product.STATUS,
        CategoryId = product.CATEGORY_ID,
        CategoryName = product.CATEGORY.CATEGORY_NAME,
        SupplierId = product.SUPPLIER_ID,
        SupplierName = product.SUPPLIER.SUPPLIER_NAME,
        CurrentStock = product.INVENTORies.Sum(inventory => (int?)inventory.CURRENT_STOCK) ?? 0
    };

    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<ProductListItemDto>> ListProductsAsync(
        int page, int size, string? keyword, string? status)
    {
        NormalizePaging(ref page, ref size);
        var query = ApplyFilters(_db.PRODUCTs.AsNoTracking(), keyword, status);
        return await ToPageResultAsync(query, page, size);
    }

    public async Task<Product> CreateProductAsync(ProductDto dto)
    {
        var values = await ValidateDtoAsync(dto);

        var product = new PRODUCT
        {
            CATEGORY_ID = values.CategoryId,
            SUPPLIER_ID = values.SupplierId,
            PRODUCT_NAME = values.ProductName,
            BARCODE = values.Barcode,
            SPECIFICATION = values.Specification,
            PURCHASE_PRICE = values.PurchasePrice,
            SALE_PRICE = values.SalePrice,
            IS_PROMOTION = "否",
            PROMOTION_PRICE = null,
            STOCK_WARNING = values.StockWarning,
            UNIT = values.Unit,
            STATUS = values.Status
        };

        _db.PRODUCTs.Add(product);
        await SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        return await _db.PRODUCTs
            .AsNoTracking()
            .Where(product => product.PRODUCT_ID == productId)
            .Select(ProductProjection)
            .FirstOrDefaultAsync();
    }

    public async Task<Product?> UpdateProductAsync(int productId, ProductDto dto)
    {
        var product = await _db.PRODUCTs
            .FirstOrDefaultAsync(item => item.PRODUCT_ID == productId);

        if (product == null)
            return null;

        var values = await ValidateDtoAsync(dto, productId, product.STATUS);

        product.CATEGORY_ID = values.CategoryId;
        product.SUPPLIER_ID = values.SupplierId;
        product.PRODUCT_NAME = values.ProductName;
        product.BARCODE = values.Barcode;
        product.SPECIFICATION = values.Specification;
        product.PURCHASE_PRICE = values.PurchasePrice;
        product.SALE_PRICE = values.SalePrice;
        product.STOCK_WARNING = values.StockWarning;
        product.UNIT = values.Unit;
        product.STATUS = values.Status;

        await SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _db.PRODUCTs
            .FirstOrDefaultAsync(item => item.PRODUCT_ID == productId);

        if (product == null)
            return false;

        product.STATUS = "停售";
        await SaveChangesAsync();
        return true;
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        var normalizedBarcode = NormalizeOptional(barcode);
        if (normalizedBarcode == null)
            return null;

        return await _db.PRODUCTs
            .AsNoTracking()
            .Where(product => product.BARCODE == normalizedBarcode)
            .Select(ProductProjection)
            .FirstOrDefaultAsync();
    }

    public async Task<PageResult<ProductListItemDto>> ListWarningStockProductsAsync(
        int page, int size, string? keyword, string? status)
    {
        NormalizePaging(ref page, ref size);

        var query = ApplyFilters(_db.PRODUCTs.AsNoTracking(), keyword, status)
            .Where(product =>
                product.STOCK_WARNING.HasValue &&
                (product.INVENTORies.Sum(inventory => (int?)inventory.CURRENT_STOCK) ?? 0) <= product.STOCK_WARNING.Value);

        return await ToPageResultAsync(query, page, size);
    }

    private async Task<ValidatedProduct> ValidateDtoAsync(
        ProductDto dto,
        int? excludeProductId = null,
        string? currentStatus = null)
    {
        var categoryId = RequirePositiveId(dto.CategoryId, "商品分类不能为空");
        var supplierId = RequirePositiveId(dto.SupplierId, "供应商不能为空");
        var productName = RequireText(dto.ProductName, "商品名称不能为空");
        var barcode = RequireText(dto.Barcode, "商品条码不能为空");
        var specification = NormalizeOptional(dto.Specification);
        var unit = NormalizeOptional(dto.Unit);
        var status = NormalizeOptional(dto.Status) ?? currentStatus ?? "在售";

        if (!dto.PurchasePrice.HasValue)
            throw new BusinessException(400, "采购价格不能为空");
        if (!dto.SalePrice.HasValue)
            throw new BusinessException(400, "销售价格不能为空");
        if (dto.PurchasePrice < 0 || dto.SalePrice < 0)
            throw new BusinessException(400, "商品价格不能小于0");
        if (dto.StockWarning < 0)
            throw new BusinessException(400, "库存预警值不能小于0");

        ValidateLengths(productName, barcode, specification, unit);
        ValidateStatus(status);

        var categoryExists = await _db.PRODUCT_CATEGORies
            .AsNoTracking()
            .AnyAsync(category => category.CATEGORY_ID == categoryId);
        if (!categoryExists)
            throw new BusinessException(400, "商品分类不存在");

        var supplierExists = await _db.SUPPLIERs
            .AsNoTracking()
            .AnyAsync(supplier => supplier.SUPPLIER_ID == supplierId);
        if (!supplierExists)
            throw new BusinessException(400, "供应商不存在");

        var barcodeExists = await _db.PRODUCTs
            .AsNoTracking()
            .AnyAsync(product =>
                product.BARCODE == barcode &&
                (!excludeProductId.HasValue || product.PRODUCT_ID != excludeProductId.Value));
        if (barcodeExists)
            throw new BusinessException(400, "商品条码已存在");

        return new ValidatedProduct(
            categoryId,
            supplierId,
            productName,
            barcode,
            specification,
            dto.PurchasePrice.Value,
            dto.SalePrice.Value,
            dto.StockWarning ?? 10,
            unit,
            status);
    }

    private static IQueryable<PRODUCT> ApplyFilters(
        IQueryable<PRODUCT> query,
        string? keyword,
        string? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(product =>
                product.PRODUCT_NAME.Contains(normalizedKeyword) ||
                (product.BARCODE != null && product.BARCODE.Contains(normalizedKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(product => product.STATUS == normalizedStatus);
        }

        return query;
    }

    private static async Task<PageResult<ProductListItemDto>> ToPageResultAsync(
        IQueryable<PRODUCT> query,
        int page,
        int size)
    {
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(product => product.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ProductListProjection)
            .ToListAsync();

        return new PageResult<ProductListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
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

    private static Product ToDto(PRODUCT product)
    {
        return new Product
        {
            ProductId = product.PRODUCT_ID,
            CategoryId = product.CATEGORY_ID,
            SupplierId = product.SUPPLIER_ID,
            ProductName = product.PRODUCT_NAME,
            Barcode = product.BARCODE,
            Specification = product.SPECIFICATION,
            PurchasePrice = product.PURCHASE_PRICE,
            SalePrice = product.SALE_PRICE,
            StockWarning = product.STOCK_WARNING,
            Unit = product.UNIT,
            Status = product.STATUS
        };
    }

    private static void ValidateLengths(
        string productName,
        string barcode,
        string? specification,
        string? unit)
    {
        if (productName.Length > 100)
            throw new BusinessException(400, "商品名称不能超过100个字符");
        if (barcode.Length > 50)
            throw new BusinessException(400, "商品条码不能超过50个字符");
        if (specification?.Length > 100)
            throw new BusinessException(400, "商品规格不能超过100个字符");
        if (unit?.Length > 20)
            throw new BusinessException(400, "商品单位不能超过20个字符");
    }

    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new BusinessException(400, "商品状态只能是在售、下架或停售");
    }

    private static int RequirePositiveId(int? value, string message)
    {
        if (!value.HasValue || value.Value <= 0)
            throw new BusinessException(400, message);

        return value.Value;
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

    private static void NormalizePaging(ref int page, ref int size)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;
    }

    private sealed record ValidatedProduct(
        int CategoryId,
        int SupplierId,
        string ProductName,
        string Barcode,
        string? Specification,
        decimal PurchasePrice,
        decimal SalePrice,
        int StockWarning,
        string? Unit,
        string Status);
}
