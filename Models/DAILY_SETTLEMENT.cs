using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 每日营业结转表：记录每日闭店后的销售汇总与各项优惠拆分
/// </summary>
public partial class DAILY_SETTLEMENT
{
    public int SETTLEMENT_ID { get; set; }

    /// <summary>
    /// 结转日期（唯一，一天一条）
    /// </summary>
    public DateTime SETTLEMENT_DATE { get; set; }

    /// <summary>
    /// 当日销售总额（实收合计）
    /// </summary>
    public decimal? TOTAL_SALES { get; set; }

    /// <summary>
    /// 现金实收金额
    /// </summary>
    public decimal? CASH_AMOUNT { get; set; }

    /// <summary>
    /// 微信实收金额
    /// </summary>
    public decimal? WECHAT_AMOUNT { get; set; }

    /// <summary>
    /// 支付宝实收金额
    /// </summary>
    public decimal? ALIPAY_AMOUNT { get; set; }

    /// <summary>
    /// 限时特价让利总额
    /// </summary>
    public decimal? PROMOTION_DISCOUNT { get; set; }

    /// <summary>
    /// 会员折扣让利总额
    /// </summary>
    public decimal? MEMBER_DISCOUNT { get; set; }

    /// <summary>
    /// 优惠券核销总额
    /// </summary>
    public decimal? COUPON_DEDUCT { get; set; }

    /// <summary>
    /// 积分抵扣金额
    /// </summary>
    public decimal? POINT_DEDUCT { get; set; }

    /// <summary>
    /// 消耗积分总数
    /// </summary>
    public int? POINT_CONSUMED { get; set; }

    /// <summary>
    /// 当日订单总数
    /// </summary>
    public int? ORDER_COUNT { get; set; }

    /// <summary>
    /// 结转状态
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime? CREATE_TIME { get; set; }
}
