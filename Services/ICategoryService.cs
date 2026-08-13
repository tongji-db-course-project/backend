using backend.Dtos;

namespace backend.Services;

public interface ICategoryService
{
    Task<PageResult<Category>> ListCategoriesAsync(
        int page, int size, string? keyword, string? status);

    Task<Category> CreateCategoryAsync(CategoryDto dto);

    Task<Category?> GetCategoryAsync(int categoryId);

    Task<Category?> UpdateCategoryAsync(int categoryId, CategoryDto dto);

    Task<bool> DeleteCategoryAsync(int categoryId);
}
