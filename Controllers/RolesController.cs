using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize(Roles = "1")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("/roles", Name = "listRoles")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<RoleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null)
    {
        var result = await _roleService.ListRolesAsync(page, size, keyword);
        return Ok(ApiResponse<PageResult<RoleListItemDto>>.Ok(result));
    }

    [HttpGet("/roles/{roleId:int}", Name = "getRole")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRole([FromRoute] int roleId)
    {
        try
        {
            var result = await _roleService.GetRoleAsync(roleId);
            return Ok(ApiResponse<RoleDetailDto>.Ok(result));
        }
        catch (Exception ex) when (ex is KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    [HttpPost("/roles", Name = "createRole")]
    public async Task<IActionResult> CreateRole([FromBody] UpdateRoleRequest request)
    {
        try { return Ok(ApiResponse<RoleDetailDto>.Ok(await _roleService.CreateRoleAsync(request))); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Error(ex); }
    }

    [HttpPut("/roles/{roleId:int}", Name = "updateRole")]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole([FromRoute] int roleId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var result = await _roleService.UpdateRoleAsync(roleId, request);
            return Ok(ApiResponse<RoleDetailDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    [HttpDelete("/roles/{roleId:int}", Name = "deleteRole")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRole([FromRoute] int roleId)
    {
        try
        {
            await _roleService.DeleteRoleAsync(roleId);
            return Ok(ApiResponse<object?>.Ok(null));
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    [HttpPut("/roles/{roleId:int}/menus", Name = "assignRoleMenus")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRoleMenus(
        [FromRoute] int roleId,
        [FromBody] AssignRoleMenusRequest request)
    {
        try
        {
            await _roleService.AssignRoleMenusAsync(roleId, request);
            return Ok(ApiResponse<object?>.Ok(null));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    [HttpPost("/roles/{roleId:int}/menus", Name = "assignRoleMenusPost")]
    public Task<IActionResult> AssignRoleMenusPost(
        [FromRoute] int roleId,
        [FromBody] AssignRoleMenusRequest request) => AssignRoleMenus(roleId, request);

    private ObjectResult Error(Exception ex)
    {
        var statusCode = ex switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new ApiResponse<object>
        {
            code = statusCode,
            message = ex.Message,
            data = null
        });
    }
}
