namespace backend.Dtos;

/// <summary>
/// 通用分页结果
/// </summary>
public class PageResult<T>
{
    /// <summary>当前页数据列表</summary>
    public IEnumerable<T> list { get; set; } = new List<T>();

    /// <summary>总记录数</summary>
    public int total { get; set; }

    /// <summary>页码</summary>
    public int page { get; set; }

    /// <summary>每页数量</summary>
    public int size { get; set; }
}
