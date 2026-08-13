using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace backend.Dtos;

public class Category
{
    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("categoryDesc")]
    public string? CategoryDesc { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class CategoryDto
{
    [Required(ErrorMessage = "分类名称不能为空")]
    [StringLength(50, ErrorMessage = "分类名称不能超过50个字符")]
    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [StringLength(200, ErrorMessage = "分类说明不能超过200个字符")]
    [JsonPropertyName("categoryDesc")]
    public string? CategoryDesc { get; set; }

    [StringLength(20, ErrorMessage = "分类状态不能超过20个字符")]
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
