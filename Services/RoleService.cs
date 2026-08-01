using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;

    public RoleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<RoleListItemDto>> ListRolesAsync(
        int page,
        int size,
        string? keyword)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.SYS_ROLEs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(r =>
                r.ROLE_NAME.Contains(kw) ||
                (r.ROLE_DESC != null && r.ROLE_DESC.Contains(kw)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(r => r.ROLE_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(r => new RoleListItemDto
            {
                roleId = r.ROLE_ID,
                roleName = r.ROLE_NAME,
                roleDesc = r.ROLE_DESC
            })
            .ToListAsync();

        return new PageResult<RoleListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<RoleDetailDto> GetRoleAsync(int roleId)
    {
        var role = await _db.SYS_ROLEs
            .AsNoTracking()
            .Where(r => r.ROLE_ID == roleId)
            .Select(r => new RoleDetailDto
            {
                roleId = r.ROLE_ID,
                roleName = r.ROLE_NAME,
                roleDesc = r.ROLE_DESC
            })
            .FirstOrDefaultAsync();

        if (role is null)
        {
            throw new KeyNotFoundException("角色不存在");
        }

        return role;
    }

    public async Task<RoleDetailDto> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        var roleName = NormalizeRequired(request.roleName, "角色名称不能为空");

        var role = await _db.SYS_ROLEs.FirstOrDefaultAsync(r => r.ROLE_ID == roleId);
        if (role is null)
        {
            throw new KeyNotFoundException("角色不存在");
        }

        var exists = await _db.SYS_ROLEs.AnyAsync(r => r.ROLE_NAME == roleName && r.ROLE_ID != roleId);
        if (exists)
        {
            throw new InvalidOperationException("角色名称已存在");
        }

        role.ROLE_NAME = roleName;
        role.ROLE_DESC = NormalizeNullable(request.roleDesc);

        await _db.SaveChangesAsync();
        return await GetRoleAsync(roleId);
    }

    public async Task DeleteRoleAsync(int roleId)
    {
        var role = await _db.SYS_ROLEs.FirstOrDefaultAsync(r => r.ROLE_ID == roleId);
        if (role is null)
        {
            throw new KeyNotFoundException("角色不存在");
        }

        var hasUsers = await _db.SYS_USERs.AnyAsync(u => u.ROLE_ID == roleId);
        if (hasUsers)
        {
            throw new InvalidOperationException("该角色已分配给用户，不能删除");
        }

        var roleMenus = await _db.SYS_ROLE_MENUs
            .Where(rm => rm.ROLE_ID == roleId)
            .ToListAsync();

        _db.SYS_ROLE_MENUs.RemoveRange(roleMenus);
        _db.SYS_ROLEs.Remove(role);
        await _db.SaveChangesAsync();
    }

    public async Task AssignRoleMenusAsync(int roleId, AssignRoleMenusRequest request)
    {
        var roleExists = await _db.SYS_ROLEs.AnyAsync(r => r.ROLE_ID == roleId);
        if (!roleExists)
        {
            throw new KeyNotFoundException("角色不存在");
        }

        var menuIds = request.menuIds
            .Distinct()
            .ToList();

        if (menuIds.Any(id => id <= 0))
        {
            throw new ArgumentException("菜单编号必须是正整数");
        }

        if (menuIds.Count > 0)
        {
            var existingMenuIds = await _db.SYS_MENUs
                .Where(m => menuIds.Contains(m.MENU_ID))
                .Select(m => m.MENU_ID)
                .ToListAsync();

            var missingMenuIds = menuIds.Except(existingMenuIds).ToList();
            if (missingMenuIds.Count > 0)
            {
                throw new ArgumentException($"菜单不存在：{string.Join(",", missingMenuIds)}");
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.Database.ExecuteSqlRawAsync("LOCK TABLE SYS_ROLE_MENU IN EXCLUSIVE MODE");

        var oldRoleMenus = await _db.SYS_ROLE_MENUs
            .Where(rm => rm.ROLE_ID == roleId)
            .ToListAsync();

        _db.SYS_ROLE_MENUs.RemoveRange(oldRoleMenus);
        await _db.SaveChangesAsync();

        foreach (var menuId in menuIds)
        {
            _db.SYS_ROLE_MENUs.Add(new SYS_ROLE_MENU
            {
                ROLE_ID = roleId,
                MENU_ID = menuId
            });
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(errorMessage);
        }

        return value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
