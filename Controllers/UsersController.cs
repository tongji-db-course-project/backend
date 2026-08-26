using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize(Roles = "1")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("/users", Name = "listUsers")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<UserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        var result = await _userService.ListUsersAsync(page, size, keyword, status);
        return Ok(ApiResponse<PageResult<UserListItemDto>>.Ok(result));
    }

    [HttpPost("/users", Name = "createUser")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var result = await _userService.CreateUserAsync(request);
            return Ok(ApiResponse<UserDetailDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    [HttpGet("/users/{userId:int}", Name = "getUser")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser([FromRoute] int userId)
    {
        try
        {
            var result = await _userService.GetUserAsync(userId);
            return Ok(ApiResponse<UserDetailDto>.Ok(result));
        }
        catch (Exception ex) when (ex is KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    [HttpPut("/users/{userId:int}", Name = "updateUser")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser([FromRoute] int userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(userId, request);
            return Ok(ApiResponse<UserDetailDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    [HttpPatch("/users/{userId:int}/status", Name = "changeUserStatus")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeUserStatus(
        [FromRoute] int userId,
        [FromBody] ChangeUserStatusRequest request)
    {
        try
        {
            var result = await _userService.ChangeUserStatusAsync(userId, request);
            return Ok(ApiResponse<UserDetailDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    [HttpGet("/users/{userId:int}/menus", Name = "listUserMenus")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MenuListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUserMenus([FromRoute] int userId)
    {
        try
        {
            var result = await _userService.ListUserMenusAsync(userId);
            return Ok(ApiResponse<IEnumerable<MenuListItemDto>>.Ok(result));
        }
        catch (Exception ex) when (ex is KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    [HttpDelete("/users/{userId:int}", Name = "deleteUser")]
    public async Task<IActionResult> DeleteUser([FromRoute] int userId)
    {
        try
        {
            await _userService.DeleteUserAsync(userId);
            return Ok(ApiResponse<object?>.Ok(null, "删除成功"));
        }
        catch (KeyNotFoundException ex) { return Error(ex); }
    }

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
