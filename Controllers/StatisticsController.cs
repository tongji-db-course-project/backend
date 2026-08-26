using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// 统计分析控制器
/// </summary>
[ApiController]
[Route("statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// 日销售统计 - 按日期统计销售数据（按天分组返回）
    /// </summary>
    /// <param name="startDate">开始日期 (格式: yyyy-MM-dd)</param>
    /// <param name="endDate">结束日期 (格式: yyyy-MM-dd)</param>
    /// <returns>每日销售统计列表</returns>
    [HttpGet("sales/daily")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesStatistics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDailySalesStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(ApiResponse<string>.Fail(400, "开始日期不能大于结束日期"));

        var result = await _statisticsService.GetDailySalesStatisticsAsync(startDate, endDate);
        return Ok(ApiResponse<List<SalesStatistics>>.Ok(result));
    }

    /// <summary>
    /// 月销售统计 - 按月份统计销售数据（按月分组返回）
    /// </summary>
    /// <param name="startDate">开始日期 (格式: yyyy-MM-dd)</param>
    /// <param name="endDate">结束日期 (格式: yyyy-MM-dd)</param>
    /// <returns>每月销售统计列表</returns>
    [HttpGet("sales/monthly")]
    [ProducesResponseType(typeof(ApiResponse<List<MonthlySalesStatistics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlySalesStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(ApiResponse<string>.Fail(400, "开始日期不能大于结束日期"));

        var result = await _statisticsService.GetMonthlySalesStatisticsAsync(startDate, endDate);
        return Ok(ApiResponse<List<MonthlySalesStatistics>>.Ok(result));
    }

    /// <summary>
    /// 商品销量排行 - 查询商品销量排行
    /// </summary>
    /// <param name="startDate">开始日期 (格式: yyyy-MM-dd)</param>
    /// <param name="endDate">结束日期 (格式: yyyy-MM-dd)</param>
    /// <returns>商品销量排行列表</returns>
    [HttpGet("products/rank")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductRank>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductRank(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(ApiResponse<string>.Fail(400, "开始日期不能大于结束日期"));

        var result = await _statisticsService.GetProductRankAsync(startDate, endDate);
        return Ok(ApiResponse<List<ProductRank>>.Ok(result));
    }

    /// <summary>
    /// 商品毛利分析 - 分析商品毛利
    /// </summary>
    /// <param name="startDate">开始日期 (格式: yyyy-MM-dd)</param>
    /// <param name="endDate">结束日期 (格式: yyyy-MM-dd)</param>
    /// <returns>毛利统计数据</returns>
    [HttpGet("profit")]
    [ProducesResponseType(typeof(ApiResponse<ProfitStatistics>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProfitStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(ApiResponse<string>.Fail(400, "开始日期不能大于结束日期"));

        var result = await _statisticsService.GetProfitStatisticsAsync(startDate, endDate);
        return Ok(ApiResponse<ProfitStatistics>.Ok(result));
    }

    /// <summary>
    /// 库存统计 - 统计库存总量和低库存数量
    /// </summary>
    /// <param name="startDate">开始日期 (格式: yyyy-MM-dd)</param>
    /// <param name="endDate">结束日期 (格式: yyyy-MM-dd)</param>
    /// <returns>库存统计数据</returns>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(ApiResponse<InventoryStatistics>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var result = await _statisticsService.GetInventoryStatisticsAsync(startDate, endDate);
        return Ok(ApiResponse<InventoryStatistics>.Ok(result));
    }

    /// <summary>
    /// 会员消费统计 - 统计会员消费数据
    /// </summary>
    /// <param name="startDate">开始日期 (格式: yyyy-MM-dd)</param>
    /// <param name="endDate">结束日期 (格式: yyyy-MM-dd)</param>
    /// <returns>会员消费统计数据</returns>
    [HttpGet("members")]
    [ProducesResponseType(typeof(ApiResponse<MemberStatistics>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMemberStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            return BadRequest(ApiResponse<string>.Fail(400, "开始日期不能大于结束日期"));

        var result = await _statisticsService.GetMemberStatisticsAsync(startDate, endDate);
        return Ok(ApiResponse<MemberStatistics>.Ok(result));
    }

    [HttpGet("products/profit-rank")]
    public async Task<IActionResult> GetProductProfitRank(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate) return BadRequest(ApiResponse<object>.Fail(400, "开始日期不能大于结束日期"));
        return Ok(ApiResponse<List<ProductProfitRankDto>>.Ok(await _statisticsService.GetProductProfitRankAsync(startDate, endDate)));
    }

    [HttpGet("inventory/turnover")]
    public async Task<IActionResult> GetInventoryTurnover(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate) return BadRequest(ApiResponse<object>.Fail(400, "开始日期不能大于结束日期"));
        return Ok(ApiResponse<List<InventoryTurnoverDto>>.Ok(await _statisticsService.GetInventoryTurnoverAsync(startDate, endDate)));
    }

    [HttpPost("daily-settlements/{date:datetime}")]
    public async Task<IActionResult> GenerateDailySettlement(DateTime date) =>
        Ok(ApiResponse<DailySettlementDto>.Ok(await _statisticsService.GenerateDailySettlementAsync(date)));

    [HttpGet("daily-settlements/{date:datetime}")]
    public async Task<IActionResult> GetDailySettlement(DateTime date)
    {
        try { return Ok(ApiResponse<DailySettlementDto>.Ok(await _statisticsService.GetDailySettlementAsync(date))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
    }
}
