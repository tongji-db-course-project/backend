namespace backend.Dtos;

public class MenuListItemDto
{
    public int menuId { get; set; }

    public string menuName { get; set; } = string.Empty;

    public string? menuUrl { get; set; }

    public int parentId { get; set; }

    public short? menuOrder { get; set; }
}
