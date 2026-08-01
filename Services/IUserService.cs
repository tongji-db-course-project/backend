using backend.Dtos;

namespace backend.Services;

public interface IUserService
{
    Task<PageResult<UserListItemDto>> ListUsersAsync(
        int page, int size, string? keyword, string? status);

    Task<UserDetailDto> CreateUserAsync(CreateUserRequest request);

    Task<UserDetailDto> GetUserAsync(int userId);

    Task<UserDetailDto> UpdateUserAsync(int userId, UpdateUserRequest request);

    Task<UserDetailDto> ChangeUserStatusAsync(int userId, ChangeUserStatusRequest request);

    Task<IEnumerable<MenuListItemDto>> ListUserMenusAsync(int userId);
}
