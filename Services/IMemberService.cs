using backend.Dtos;

namespace backend.Services;

/// <summary>
/// 会员业务接口
/// </summary>
public interface IMemberService
{
    /// <summary>
    /// 分页查询会员列表
    /// </summary>
    Task<PageResult<Member>> ListMembersAsync(int page, int size, string? keyword, string? status);

    /// <summary>
    /// 新增会员
    /// </summary>
    Task<Member> CreateMemberAsync(MemberDto dto);

    /// <summary>
    /// 根据ID查询会员
    /// </summary>
    Task<Member?> GetMemberByIdAsync(int memberId);

    /// <summary>
    /// 修改会员信息
    /// </summary>
    Task<Member?> UpdateMemberAsync(int memberId, MemberDto dto);

    /// <summary>
    /// 逻辑删除会员（改为停用）
    /// </summary>
    Task<bool> DeleteMemberAsync(int memberId);

    /// <summary>
    /// 根据手机号查询会员
    /// </summary>
    Task<Member?> GetMemberByPhoneAsync(string phone);

    /// <summary>
    /// 查询会员消费记录
    /// </summary>
    Task<(PageResult<SaleOrderListItem>? Result, bool MemberExists)> GetMemberOrdersAsync(int memberId, int page, int size);
}