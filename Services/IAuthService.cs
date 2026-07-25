using backend.Dtos;

namespace backend.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(string username, string password);

    Task<UserInfoDto?> GetCurrentUserAsync(int userId);

    Task<IReadOnlyList<MenuDto>> GetAccessibleMenusAsync(int userId);
}
