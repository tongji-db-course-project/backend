using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserService : IUserService
{
    private const string EnabledStatus = "启用";
    private const string DisabledStatus = "禁用";

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        EnabledStatus,
        DisabledStatus
    };

    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<UserListItemDto>> ListUsersAsync(
        int page,
        int size,
        string? keyword,
        string? status)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.SYS_USERs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(u =>
                u.USERNAME.Contains(kw) ||
                (u.REAL_NAME != null && u.REAL_NAME.Contains(kw)) ||
                (u.PHONE != null && u.PHONE.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(u => u.STATUS == st);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.USER_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(u => new UserListItemDto
            {
                userId = u.USER_ID,
                roleId = u.ROLE_ID,
                username = u.USERNAME,
                realName = u.REAL_NAME,
                gender = u.GENDER,
                phone = u.PHONE,
                status = u.STATUS,
                createTime = u.CREATE_TIME
            })
            .ToListAsync();

        return new PageResult<UserListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<UserDetailDto> CreateUserAsync(CreateUserRequest request)
    {
        var username = NormalizeRequired(request.username, "用户名不能为空");
        var password = NormalizeRequired(request.password, "密码不能为空");
        var status = NormalizeStatus(request.status);

        if (await _db.SYS_USERs.AnyAsync(u => u.USERNAME == username))
        {
            throw new InvalidOperationException("用户名已存在");
        }

        await EnsureRoleExistsAsync(request.roleId);

        var nextUserId = await _db.SYS_USERs
            .Select(u => (int?)u.USER_ID)
            .MaxAsync() ?? 0;

        var user = new SYS_USER
        {
            USER_ID = nextUserId + 1,
            ROLE_ID = request.roleId,
            USERNAME = username,
            PASSWORD = BCrypt.Net.BCrypt.HashPassword(password),
            REAL_NAME = NormalizeNullable(request.realName),
            GENDER = NormalizeNullable(request.gender),
            PHONE = NormalizeNullable(request.phone),
            STATUS = status,
            CREATE_TIME = DateTime.Now
        };

        _db.SYS_USERs.Add(user);
        await _db.SaveChangesAsync();

        return await GetUserAsync(user.USER_ID);
    }

    public async Task<UserDetailDto> GetUserAsync(int userId)
    {
        var user = await _db.SYS_USERs
            .AsNoTracking()
            .Where(u => u.USER_ID == userId)
            .Select(u => ToDetailDto(u))
            .FirstOrDefaultAsync();

        if (user is null)
        {
            throw new KeyNotFoundException("用户不存在");
        }

        return user;
    }

    public async Task<UserDetailDto> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var user = await _db.SYS_USERs.FirstOrDefaultAsync(u => u.USER_ID == userId);
        if (user is null)
        {
            throw new KeyNotFoundException("用户不存在");
        }

        await EnsureRoleExistsAsync(request.roleId);

        user.ROLE_ID = request.roleId;
        user.REAL_NAME = NormalizeNullable(request.realName);
        user.GENDER = NormalizeNullable(request.gender);
        user.PHONE = NormalizeNullable(request.phone);
        user.STATUS = NormalizeStatus(request.status);

        await _db.SaveChangesAsync();
        return await GetUserAsync(userId);
    }

    public async Task<UserDetailDto> ChangeUserStatusAsync(int userId, ChangeUserStatusRequest request)
    {
        var status = NormalizeStatus(request.status);
        var user = await _db.SYS_USERs.FirstOrDefaultAsync(u => u.USER_ID == userId);
        if (user is null)
        {
            throw new KeyNotFoundException("用户不存在");
        }

        user.STATUS = status;
        await _db.SaveChangesAsync();

        return await GetUserAsync(userId);
    }

    public async Task<IEnumerable<MenuListItemDto>> ListUserMenusAsync(int userId)
    {
        var user = await _db.SYS_USERs
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.USER_ID == userId);

        if (user is null)
        {
            throw new KeyNotFoundException("用户不存在");
        }

        if (user.ROLE_ID is null)
        {
            return new List<MenuListItemDto>();
        }

        return await _db.SYS_ROLE_MENUs
            .AsNoTracking()
            .Where(rm => rm.ROLE_ID == user.ROLE_ID)
            .OrderBy(rm => rm.MENU.MENU_ORDER)
            .ThenBy(rm => rm.MENU.MENU_ID)
            .Select(rm => new MenuListItemDto
            {
                menuId = rm.MENU.MENU_ID,
                menuName = rm.MENU.MENU_NAME,
                menuUrl = rm.MENU.MENU_URL,
                parentId = rm.MENU.PARENT_ID ?? 0,
                menuOrder = rm.MENU.MENU_ORDER
            })
            .ToListAsync();
    }

    private async Task EnsureRoleExistsAsync(int? roleId)
    {
        if (roleId is null)
        {
            return;
        }

        var exists = await _db.SYS_ROLEs.AnyAsync(r => r.ROLE_ID == roleId);
        if (!exists)
        {
            throw new ArgumentException("角色不存在");
        }
    }

    private static UserDetailDto ToDetailDto(SYS_USER user)
    {
        return new UserDetailDto
        {
            userId = user.USER_ID,
            roleId = user.ROLE_ID,
            username = user.USERNAME,
            realName = user.REAL_NAME,
            gender = user.GENDER,
            phone = user.PHONE,
            status = user.STATUS,
            createTime = user.CREATE_TIME
        };
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(errorMessage);
        }

        return value.Trim();
    }

    private static string NormalizeStatus(string? status)
    {
        var value = string.IsNullOrWhiteSpace(status) ? EnabledStatus : status.Trim();
        if (!ValidStatuses.Contains(value))
        {
            throw new ArgumentException("状态只能是：启用、禁用");
        }

        return value;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
