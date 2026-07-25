namespace backend.Dtos;

/// <summary>
/// 菜单项，对应 Apifox 查询当前用户可访问菜单
/// </summary>
public class MenuDto
{
    public int menuId { get; set; }

    public string menuName { get; set; } = string.Empty;

    public string? menuUrl { get; set; }

    public int? parentId { get; set; }

    public short? menuOrder { get; set; }
}
