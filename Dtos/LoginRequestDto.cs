using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class LoginRequestDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string password { get; set; } = string.Empty;
}
