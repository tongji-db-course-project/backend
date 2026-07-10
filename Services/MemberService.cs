using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 会员业务实现
/// </summary>
public class MemberService : IMemberService
{
    private readonly AppDbContext _db;

    public MemberService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<Member>> ListMembersAsync(int page, int size, string? keyword, string? status)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var query = _db.MEMBERs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(m =>
                m.MEMBER_NAME.Contains(kw) ||
                (m.PHONE != null && m.PHONE.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(m => m.STATUS == st);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(m => m.MEMBER_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(m => ToMember(m))
            .ToListAsync();

        return new PageResult<Member>
        {
            list  = items,
            total = total,
            page  = page,
            size  = size
        };
    }

    public async Task<Member> CreateMemberAsync(MemberDto dto)
    {
        var member = new MEMBER
        {
            MEMBER_NAME = dto.memberName,
            PHONE = dto.phone,
            GENDER = dto.gender,
            LEVEL_NAME = dto.levelName ?? "普通会员",
            STATUS = dto.status ?? "启用",
            POINTS = 0,
            TOTAL_AMOUNT = 0,
            CREATE_TIME = DateTime.Now
        };

        _db.MEMBERs.Add(member);
        await _db.SaveChangesAsync();

        return ToMember(member);
    }

    public async Task<Member?> GetMemberByIdAsync(int memberId)
    {
        var member = await _db.MEMBERs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MEMBER_ID == memberId);

        return member != null ? ToMember(member) : null;
    }

    public async Task<Member?> UpdateMemberAsync(int memberId, MemberDto dto)
    {
        var member = await _db.MEMBERs
            .FirstOrDefaultAsync(m => m.MEMBER_ID == memberId);

        if (member == null)
            return null;

        if (!string.IsNullOrWhiteSpace(dto.memberName))
            member.MEMBER_NAME = dto.memberName;

        if (!string.IsNullOrWhiteSpace(dto.phone))
            member.PHONE = dto.phone;

        if (dto.gender != null)
            member.GENDER = dto.gender;

        if (dto.levelName != null)
            member.LEVEL_NAME = dto.levelName;

        if (dto.status != null)
            member.STATUS = dto.status;

        await _db.SaveChangesAsync();

        return ToMember(member);
    }

    public async Task<bool> DeleteMemberAsync(int memberId)
    {
        var member = await _db.MEMBERs
            .FirstOrDefaultAsync(m => m.MEMBER_ID == memberId);

        if (member == null)
            return false;

        member.STATUS = member.STATUS == "启用" ? "禁用" : member.STATUS;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<Member?> GetMemberByPhoneAsync(string phone)
    {
        var member = await _db.MEMBERs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.PHONE == phone);

        return member != null ? ToMember(member) : null;
    }

    public async Task<(PageResult<SaleOrderListItem>? Result, bool MemberExists)> GetMemberOrdersAsync(int memberId, int page, int size)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        var memberExists = await _db.MEMBERs.AnyAsync(m => m.MEMBER_ID == memberId);
        if (!memberExists)
            return (null, false);

        var query = _db.SALE_ORDERs
            .AsNoTracking()
            .Where(o => o.MEMBER_ID == memberId)
            .AsQueryable();

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.SALE_DATE)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(o => new SaleOrderListItem
            {
                saleId         = o.SALE_ID,
                saleNo         = o.SALE_NO,
                saleDate       = o.SALE_DATE,
                totalAmount    = o.TOTAL_AMOUNT,
                discountAmount = o.DISCOUNT_AMOUNT,
                paidAmount     = o.PAID_AMOUNT,
                payType        = o.PAY_TYPE,
                status         = o.STATUS
            })
            .ToListAsync();

        return (new PageResult<SaleOrderListItem>
        {
            list  = items,
            total = total,
            page  = page,
            size  = size
        }, true);
    }

    private static Member ToMember(MEMBER member)
    {
        return new Member
        {
            memberId     = member.MEMBER_ID,
            memberName   = member.MEMBER_NAME,
            phone        = member.PHONE,
            gender       = member.GENDER,
            levelName    = member.LEVEL_NAME,
            points       = member.POINTS,
            totalAmount  = member.TOTAL_AMOUNT,
            registerTime = member.CREATE_TIME,
            status       = member.STATUS
        };
    }
}