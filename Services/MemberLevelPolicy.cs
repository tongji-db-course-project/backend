using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class MemberLevelPolicy
{
    public static async Task ApplyAmountChangeAsync(AppDbContext db, int memberId, decimal amountChange)
    {
        var member = await db.MEMBERs.FirstAsync(x => x.MEMBER_ID == memberId);
        var amount = Math.Max(0, (member.TOTAL_AMOUNT ?? 0) + amountChange);
        member.TOTAL_AMOUNT = amount;
        member.LEVEL_NAME = amount >= 5000 ? "钻石会员" : amount >= 1000 ? "黄金会员" : "普通会员";
    }
}
