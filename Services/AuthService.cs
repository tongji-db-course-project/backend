using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.Dtos;
using backend.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

/// <summary>
/// 登录鉴权：BCrypt 校验密码 + 签发 JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthService(AppDbContext db, IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _jwt = jwtOptions.Value;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        var normalizedUsername = username.Trim();

        var user = await _db.SYS_USERs
            .AsNoTracking()
            .Include(u => u.ROLE)
            .FirstOrDefaultAsync(u => u.USERNAME == normalizedUsername);

        if (user is null || !VerifyPassword(password, user.PASSWORD))
            return null;

        if (user.STATUS != "启用")
            return null;

        var expiresAt = DateTime.UtcNow.AddHours(_jwt.ExpireHours);
        var token = GenerateToken(user, expiresAt);

        return new LoginResponseDto
        {
            token = token,
            userId = user.USER_ID,
            username = user.USERNAME,
            realName = user.REAL_NAME,
            roleName = user.ROLE?.ROLE_NAME
        };
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _db.SYS_USERs
            .AsNoTracking()
            .Include(u => u.ROLE)
            .FirstOrDefaultAsync(u => u.USER_ID == userId);

        return user is null ? null : MapUser(user);
    }

    public async Task<IReadOnlyList<MenuDto>> GetAccessibleMenusAsync(int userId)
    {
        var roleId = await _db.SYS_USERs
            .AsNoTracking()
            .Where(u => u.USER_ID == userId)
            .Select(u => u.ROLE_ID)
            .FirstOrDefaultAsync();

        if (roleId is null)
            return Array.Empty<MenuDto>();

        return await _db.SYS_ROLE_MENUs
            .AsNoTracking()
            .Where(rm => rm.ROLE_ID == roleId)
            .Select(rm => rm.MENU)
            .OrderBy(m => m.MENU_ORDER)
            .ThenBy(m => m.MENU_ID)
            .Select(m => new MenuDto
            {
                menuId = m.MENU_ID,
                menuName = m.MENU_NAME,
                menuUrl = m.MENU_URL,
                parentId = m.PARENT_ID,
                menuOrder = m.MENU_ORDER
            })
            .ToListAsync();
    }

    private static bool VerifyPassword(string plainPassword, string storedPassword)
    {
        if (storedPassword.StartsWith("$2"))
            return BCrypt.Net.BCrypt.Verify(plainPassword, storedPassword);

        return plainPassword == storedPassword;
    }

    private string GenerateToken(Models.SYS_USER user, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.USER_ID.ToString()),
            new(ClaimTypes.Name, user.USERNAME),
        };

        if (user.ROLE_ID.HasValue)
            claims.Add(new Claim(ClaimTypes.Role, user.ROLE_ID.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserInfoDto MapUser(Models.SYS_USER user) => new()
    {
        userId = user.USER_ID,
        username = user.USERNAME,
        realName = user.REAL_NAME,
        roleId = user.ROLE_ID,
        roleName = user.ROLE?.ROLE_NAME,
        status = user.STATUS
    };
}
