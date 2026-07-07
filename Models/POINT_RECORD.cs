using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 积分流水表：记录会员积分的每一次增减变动历史
/// </summary>
public partial class POINT_RECORD
{
    public int POINT_RECORD_ID { get; set; }

    public int MEMBER_ID { get; set; }

    /// <summary>
    /// 关联销售单，记录是哪笔交易产生的积分变动；若是其他活动导致积分变化时可为空
    /// </summary>
    public int? SALE_ID { get; set; }

    public string CHANGE_TYPE { get; set; } = null!;

    public int CHANGE_POINTS { get; set; }

    public int REMAIN_POINTS { get; set; }

    public DateTime? RECORD_TIME { get; set; }

    public string? REMARK { get; set; }

    public virtual MEMBER MEMBER { get; set; } = null!;

    public virtual SALE_ORDER? SALE { get; set; }
}
