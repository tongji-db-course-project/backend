using backend.Data;
using backend.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _db;

    public MenuService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<MenuListItemDto>> ListMenusAsync()
    {
        return await _db.SYS_MENUs
            .AsNoTracking()
            .OrderBy(m => m.MENU_ORDER)
            .ThenBy(m => m.MENU_ID)
            .Select(m => new MenuListItemDto
            {
                menuId = m.MENU_ID,
                menuName = m.MENU_NAME,
                menuUrl = m.MENU_URL,
                parentId = m.PARENT_ID ?? 0,
                menuOrder = m.MENU_ORDER
            })
            .ToListAsync();
    }
}
