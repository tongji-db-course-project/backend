using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize]
[ApiController]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet("/api/menus", Name = "listMenus")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MenuListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMenus()
    {
        var result = await _menuService.ListMenusAsync();
        return Ok(ApiResponse<IEnumerable<MenuListItemDto>>.Ok(result));
    }
}
