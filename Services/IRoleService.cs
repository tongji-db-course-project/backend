using backend.Dtos;

namespace backend.Services;

public interface IRoleService
{
    Task<PageResult<RoleListItemDto>> ListRolesAsync(
        int page, int size, string? keyword);

    Task<RoleDetailDto> GetRoleAsync(int roleId);

    Task<RoleDetailDto> UpdateRoleAsync(int roleId, UpdateRoleRequest request);

    Task DeleteRoleAsync(int roleId);

    Task AssignRoleMenusAsync(int roleId, AssignRoleMenusRequest request);
}
