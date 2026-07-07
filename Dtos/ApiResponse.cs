namespace backend.Dtos;

/// <summary>
/// 统一响应结构：{ code, message, data }
/// </summary>
public class ApiResponse<T>
{
    public int code { get; set; }

    public string message { get; set; } = string.Empty;

    public T? data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "成功")
        => new() { code = 200, message = message, data = data };
}
