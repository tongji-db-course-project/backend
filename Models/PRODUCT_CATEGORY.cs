using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 商品类别表
/// </summary>
public partial class PRODUCT_CATEGORY
{
    public int CATEGORY_ID { get; set; }

    /// <summary>
    /// 类别名称，如食品、日用品等
    /// </summary>
    public string CATEGORY_NAME { get; set; } = null!;

    public string? CATEGORY_DESC { get; set; }

    public string? STATUS { get; set; }

    public virtual ICollection<PRODUCT> PRODUCTs { get; set; } = new List<PRODUCT>();
}
