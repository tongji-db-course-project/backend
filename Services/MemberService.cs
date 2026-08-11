using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace backend.Services;

/// <summary>
/// 会员业务实现
/// </summary>
public class MemberService : IMemberService
{
    private static readonly HashSet<string> ValidGenders = new(StringComparer.Ordinal) { "男", "女", "未知" };
    private static readonly HashSet<string> ValidLevels = new(StringComparer.Ordinal) { "普通会员", "黄金会员", "钻石会员" };
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal) { "启用", "禁用" };

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
            list = items,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<Member> CreateMemberAsync(MemberDto dto)
    {
        var memberName = RequireText(dto.memberName, "会员姓名不能为空");
        var phone = RequireText(dto.phone, "手机号不能为空");
        var gender = NormalizeOptional(dto.gender);
        var levelName = NormalizeOptional(dto.levelName) ?? "普通会员";
        var status = NormalizeOptional(dto.status) ?? "启用";

        ValidateValue(gender, ValidGenders, "性别只能是男、女或未知");
        ValidateValue(levelName, ValidLevels, "会员等级只能是普通会员、黄金会员或钻石会员");
        ValidateValue(status, ValidStatuses, "会员状态只能是启用或禁用");
        await EnsurePhoneUniqueAsync(phone);

        var member = new MEMBER
        {
            MEMBER_NAME = memberName,
            PHONE = phone,
            GENDER = gender,
            LEVEL_NAME = levelName,
            STATUS = status,
            POINTS = 0,
            TOTAL_AMOUNT = 0,
            CREATE_TIME = DateTime.Now
        };

        _db.MEMBERs.Add(member);
        await SaveChangesAsync();

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

        if (dto.memberName != null)
            member.MEMBER_NAME = RequireText(dto.memberName, "会员姓名不能为空");

        if (dto.phone != null)
        {
            var phone = RequireText(dto.phone, "手机号不能为空");
            await EnsurePhoneUniqueAsync(phone, memberId);
            member.PHONE = phone;
        }

        if (dto.gender != null)
        {
            var gender = NormalizeOptional(dto.gender);
            ValidateValue(gender, ValidGenders, "性别只能是男、女或未知");
            member.GENDER = gender;
        }

        if (dto.levelName != null)
        {
            var levelName = RequireText(dto.levelName, "会员等级不能为空");
            ValidateValue(levelName, ValidLevels, "会员等级只能是普通会员、黄金会员或钻石会员");
            member.LEVEL_NAME = levelName;
        }

        if (dto.status != null)
        {
            var status = RequireText(dto.status, "会员状态不能为空");
            ValidateValue(status, ValidStatuses, "会员状态只能是启用或禁用");
            member.STATUS = status;
        }

        await SaveChangesAsync();

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

    public async Task<(PageResult<SaleListItemDto>? Result, bool MemberExists)> GetMemberOrdersAsync(int memberId, int page, int size)
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
            .Select(o => new SaleListItemDto
            {
                saleId = o.SALE_ID,
                saleNo = o.SALE_NO,
                memberId = o.MEMBER_ID,
                memberName = o.MEMBER == null ? null : o.MEMBER.MEMBER_NAME,
                userId = o.USER_ID,
                cashierName = o.USER.REAL_NAME,
                saleDate = o.SALE_DATE,
                totalAmount = o.TOTAL_AMOUNT ?? 0,
                discountAmount = o.DISCOUNT_AMOUNT ?? 0,
                paidAmount = o.PAID_AMOUNT ?? 0,
                payType = o.PAY_TYPE,
                status = o.STATUS
            })
            .ToListAsync();

        return (new PageResult<SaleListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        }, true);
    }

    private async Task EnsurePhoneUniqueAsync(string phone, int? excludeMemberId = null)
    {
        var exists = await _db.MEMBERs
            .AsNoTracking()
            .AnyAsync(m => m.PHONE == phone && (!excludeMemberId.HasValue || m.MEMBER_ID != excludeMemberId.Value));

        if (exists)
            throw new BusinessException(400, "手机号已存在");
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new BusinessException(400, "会员信息不符合业务规则");
        }
    }

    private static string RequireText(string? value, string message)
    {
        var text = NormalizeOptional(value);
        if (text == null)
            throw new BusinessException(400, message);

        return text;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value == null)
            return null;

        var text = value.Trim();
        return text.Length == 0 ? null : text;
    }

    private static void ValidateValue(string? value, HashSet<string> allowedValues, string message)
    {
        if (value != null && !allowedValues.Contains(value))
            throw new BusinessException(400, message);
    }

    private static Member ToMember(MEMBER member)
    {
        return new Member
        {
            memberId = member.MEMBER_ID,
            memberName = member.MEMBER_NAME,
            phone = member.PHONE,
            gender = member.GENDER,
            levelName = member.LEVEL_NAME,
            points = member.POINTS,
            totalAmount = member.TOTAL_AMOUNT,
            registerTime = member.CREATE_TIME,
            status = member.STATUS
        };
    }
}