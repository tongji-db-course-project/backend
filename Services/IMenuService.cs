using backend.Dtos;

namespace backend.Services;

public interface IMenuService
{
    Task<IEnumerable<MenuListItemDto>> ListMenusAsync();
}
