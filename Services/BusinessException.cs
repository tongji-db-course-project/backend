namespace backend.Services;

/// <summary>
/// 可预期业务错误，用于返回统一响应 code
/// </summary>
public class BusinessException : Exception
{
    public int Code { get; }

    public BusinessException(int code, string message) : base(message)
    {
        Code = code;
    }
}
