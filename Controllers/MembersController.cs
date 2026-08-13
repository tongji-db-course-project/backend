using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    /// <summary>
    /// 查询会员列表（分页 + 关键词 + 状态过滤）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Member>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMembers(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        var result = await _memberService.ListMembersAsync(page, size, keyword, status);
        return Ok(ApiResponse<PageResult<Member>>.Ok(result));
    }

    /// <summary>
    /// 新增会员档案
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Member>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateMember([FromBody] MemberDto dto)
    {
        try
        {
            var result = await _memberService.CreateMemberAsync(dto);
            return Ok(ApiResponse<Member>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    /// <summary>
    /// 查询会员详情
    /// </summary>
    [HttpGet("{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<Member>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMemberById(int memberId)
    {
        var result = await _memberService.GetMemberByIdAsync(memberId);

        if (result == null)
            return NotFound(ApiResponse<string>.Fail(400, "会员不存在"));

        return Ok(ApiResponse<Member>.Ok(result));
    }

    /// <summary>
    /// 修改会员信息
    /// </summary>
    [HttpPut("{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<Member>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember(int memberId, [FromBody] MemberDto dto)
    {
        try
        {
            var result = await _memberService.UpdateMemberAsync(memberId, dto);

            if (result == null)
                return NotFound(ApiResponse<string>.Fail(400, "会员不存在"));

            return Ok(ApiResponse<Member>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    /// <summary>
    /// 逻辑删除会员（改为停用）
    /// </summary>
    [HttpDelete("{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMember(int memberId)
    {
        var success = await _memberService.DeleteMemberAsync(memberId);

        if (!success)
            return NotFound(ApiResponse<string>.Fail(400, "会员不存在"));

        return Ok(ApiResponse<string>.Ok("删除成功"));
    }

    /// <summary>
    /// 根据手机号查询会员
    /// </summary>
    [HttpGet("phone/{phone}")]
    [ProducesResponseType(typeof(ApiResponse<Member>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMemberByPhone(string phone)
    {
        var result = await _memberService.GetMemberByPhoneAsync(phone);

        if (result == null)
            return NotFound(ApiResponse<string>.Fail(400, "会员不存在"));

        return Ok(ApiResponse<Member>.Ok(result));
    }

    /// <summary>
    /// 查询会员消费记录
    /// </summary>
    [HttpGet("{memberId}/orders")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<SaleListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMemberOrders(
        int memberId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var (result, memberExists) = await _memberService.GetMemberOrdersAsync(memberId, page, size);

        if (!memberExists)
            return NotFound(ApiResponse<string>.Fail(400, "会员不存在"));

        return Ok(ApiResponse<PageResult<SaleListItemDto>>.Ok(result!));
    }
}
