using System.Security.Claims;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 用户登录，返回 JWT Token（Apifox: POST /auth/login）
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request.username, request.password);
        if (result is null)
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail(401, "用户名或密码错误"));

        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    /// <summary>
    /// 获取当前登录用户信息（Apifox: GET /auth/me）
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<UserInfoDto>.Fail(401, "未登录或登录状态失效"));

        var user = await _authService.GetCurrentUserAsync(userId.Value);
        if (user is null)
            return Unauthorized(ApiResponse<UserInfoDto>.Fail(401, "未登录或登录状态失效"));

        return Ok(ApiResponse<UserInfoDto>.Ok(user));
    }

    /// <summary>
    /// 查询当前用户可访问菜单（Apifox: GET /auth/menus）
    /// </summary>
    [HttpGet("menus")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MenuDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Menus()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<IReadOnlyList<MenuDto>>.Fail(401, "未登录或登录状态失效"));

        var menus = await _authService.GetAccessibleMenusAsync(userId.Value);
        return Ok(ApiResponse<IReadOnlyList<MenuDto>>.Ok(menus));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
