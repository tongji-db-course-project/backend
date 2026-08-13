using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace backend.Services;

public class CategoryService : ICategoryService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "启用",
        "禁用"
    };

    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<Category>> ListCategoriesAsync(
        int page, int size, string? keyword, string? status)
    {
        NormalizePaging(ref page, ref size);

        var query = _db.PRODUCT_CATEGORies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(category => category.CATEGORY_NAME.Contains(normalizedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(category => category.STATUS == normalizedStatus);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(category => category.CATEGORY_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(category => new Category
            {
                CategoryId = category.CATEGORY_ID,
                CategoryName = category.CATEGORY_NAME,
                CategoryDesc = category.CATEGORY_DESC,
                Status = category.STATUS
            })
            .ToListAsync();

        return new PageResult<Category>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<Category> CreateCategoryAsync(CategoryDto dto)
    {
        var categoryName = RequireText(dto.CategoryName, "分类名称不能为空");
        var categoryDesc = NormalizeOptional(dto.CategoryDesc);
        var status = NormalizeOptional(dto.Status) ?? "启用";

        ValidateLengths(categoryName, categoryDesc);
        ValidateStatus(status);
        await EnsureNameUniqueAsync(categoryName);

        var category = new PRODUCT_CATEGORY
        {
            CATEGORY_NAME = categoryName,
            CATEGORY_DESC = categoryDesc,
            STATUS = status
        };

        _db.PRODUCT_CATEGORies.Add(category);
        await SaveChangesAsync();

        return ToDto(category);
    }

    public async Task<Category?> GetCategoryAsync(int categoryId)
    {
        return await _db.PRODUCT_CATEGORies
            .AsNoTracking()
            .Where(category => category.CATEGORY_ID == categoryId)
            .Select(category => new Category
            {
                CategoryId = category.CATEGORY_ID,
                CategoryName = category.CATEGORY_NAME,
                CategoryDesc = category.CATEGORY_DESC,
                Status = category.STATUS
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Category?> UpdateCategoryAsync(int categoryId, CategoryDto dto)
    {
        var category = await _db.PRODUCT_CATEGORies
            .FirstOrDefaultAsync(item => item.CATEGORY_ID == categoryId);

        if (category == null)
            return null;

        var categoryName = RequireText(dto.CategoryName, "分类名称不能为空");
        var categoryDesc = NormalizeOptional(dto.CategoryDesc);
        var status = NormalizeOptional(dto.Status) ?? category.STATUS ?? "启用";

        ValidateLengths(categoryName, categoryDesc);
        ValidateStatus(status);
        await EnsureNameUniqueAsync(categoryName, categoryId);

        category.CATEGORY_NAME = categoryName;
        category.CATEGORY_DESC = categoryDesc;
        category.STATUS = status;

        await SaveChangesAsync();
        return ToDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _db.PRODUCT_CATEGORies
            .FirstOrDefaultAsync(item => item.CATEGORY_ID == categoryId);

        if (category == null)
            return false;

        category.STATUS = "禁用";
        await SaveChangesAsync();
        return true;
    }

    private async Task EnsureNameUniqueAsync(string categoryName, int? excludeCategoryId = null)
    {
        var exists = await _db.PRODUCT_CATEGORies
            .AsNoTracking()
            .AnyAsync(category =>
                category.CATEGORY_NAME == categoryName &&
                (!excludeCategoryId.HasValue || category.CATEGORY_ID != excludeCategoryId.Value));

        if (exists)
            throw new BusinessException(400, "分类名称已存在");
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new BusinessException(400, "商品分类信息不符合业务规则");
        }
    }

    private static Category ToDto(PRODUCT_CATEGORY category)
    {
        return new Category
        {
            CategoryId = category.CATEGORY_ID,
            CategoryName = category.CATEGORY_NAME,
            CategoryDesc = category.CATEGORY_DESC,
            Status = category.STATUS
        };
    }

    private static void ValidateLengths(string categoryName, string? categoryDesc)
    {
        if (categoryName.Length > 50)
            throw new BusinessException(400, "分类名称不能超过50个字符");

        if (categoryDesc?.Length > 200)
            throw new BusinessException(400, "分类说明不能超过200个字符");
    }

    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new BusinessException(400, "分类状态只能是启用或禁用");
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
}
