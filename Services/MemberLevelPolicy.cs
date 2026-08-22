using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class MemberLevelPolicy
{
    public static async Task RefreshAsync(AppDbContext db, int memberId, DateTime now)
    {
        var member = await db.MEMBERs.FirstAsync(x => x.MEMBER_ID == memberId);
        var since = now.AddYears(-1);
        var sales = await db.SALE_ORDERs.Where(x => x.MEMBER_ID == memberId && x.STATUS == "已完成" && x.SALE_DATE >= since)
            .SumAsync(x => (decimal?)x.PAID_AMOUNT) ?? 0;
        var refunds = await db.RETURN_ORDERs.Where(x => x.MEMBER_ID == memberId && x.STATUS == "已完成" && x.RETURN_DATE >= since)
            .SumAsync(x => (decimal?)x.REFUND_AMOUNT) ?? 0;
        var amount = Math.Max(0, sales - refunds);
        member.TOTAL_AMOUNT = amount;
        member.LEVEL_NAME = amount >= 5000 ? "钻石会员" : amount >= 1000 ? "黄金会员" : "普通会员";
    }
}
