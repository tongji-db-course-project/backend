using System.Data;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PointService : IPointService
{
    private readonly AppDbContext _db;
    public PointService(AppDbContext db) => _db = db;

    public async Task<PageResult<PointRecordDto>> ListAsync(int page, int size, int? memberId, string? changeType)
    {
        (page, size) = NormalizePage(page, size);
        var query = _db.POINT_RECORDs.AsNoTracking().AsQueryable();
        if (memberId.HasValue) query = query.Where(x => x.MEMBER_ID == memberId.Value);
        if (!string.IsNullOrWhiteSpace(changeType))
        {
            var normalizedType = changeType.Trim();
            if (normalizedType is not ("增加" or "扣减" or "抵现"))
                throw new ArgumentException("积分变动类型只能是：增加、扣减、抵现");
            query = query.Where(x => x.CHANGE_TYPE == normalizedType);
        }
        var total = await query.CountAsync();
        var list = await Project(query.OrderByDescending(x => x.RECORD_TIME).ThenByDescending(x => x.POINT_RECORD_ID))
            .Skip((page - 1) * size).Take(size).ToListAsync();
        return new PageResult<PointRecordDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<MemberPointsDto> GetMemberPointsAsync(int memberId, int page, int size)
    {
        (page, size) = NormalizePage(page, size);
        var member = await _db.MEMBERs.AsNoTracking().FirstOrDefaultAsync(x => x.MEMBER_ID == memberId)
            ?? throw new KeyNotFoundException("会员不存在");
        var query = _db.POINT_RECORDs.AsNoTracking().Where(x => x.MEMBER_ID == memberId);
        var total = await query.CountAsync();
        var records = await Project(query.OrderByDescending(x => x.RECORD_TIME).ThenByDescending(x => x.POINT_RECORD_ID))
            .Skip((page - 1) * size).Take(size).ToListAsync();
        return new MemberPointsDto
        {
            memberId = member.MEMBER_ID,
            memberName = member.MEMBER_NAME,
            points = member.POINTS ?? 0,
            records = new PageResult<PointRecordDto> { list = records, total = total, page = page, size = size }
        };
    }

    public async Task<MemberPointsDto> AdjustAsync(int memberId, AdjustPointsRequest request)
    {
        if (request.changePoints == 0) throw new ArgumentException("积分变动值不能为 0");
        if (string.IsNullOrWhiteSpace(request.remark)) throw new ArgumentException("调整原因不能为空");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var nextPointRecordId = (await _db.POINT_RECORDs.MaxAsync(x => (int?)x.POINT_RECORD_ID) ?? 0) + 1;
        var member = await _db.MEMBERs.FirstOrDefaultAsync(x => x.MEMBER_ID == memberId)
            ?? throw new KeyNotFoundException("会员不存在");
        if (member.STATUS != "启用") throw new InvalidOperationException("会员状态不可用");
        var remain = (member.POINTS ?? 0) + request.changePoints;
        if (remain < 0) throw new InvalidOperationException("会员积分不足");
        member.POINTS = remain;
        _db.POINT_RECORDs.Add(new POINT_RECORD
        {
            POINT_RECORD_ID = nextPointRecordId,
            MEMBER_ID = memberId,
            CHANGE_TYPE = request.changePoints > 0 ? "增加" : "扣减",
            CHANGE_POINTS = request.changePoints,
            REMAIN_POINTS = remain,
            RECORD_TIME = DateTime.Now,
            REMARK = request.remark.Trim()
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetMemberPointsAsync(memberId, 1, 10);
    }

    private static IQueryable<PointRecordDto> Project(IQueryable<POINT_RECORD> query) => query.Select(x => new PointRecordDto
    {
        pointRecordId = x.POINT_RECORD_ID,
        memberId = x.MEMBER_ID,
        memberName = x.MEMBER.MEMBER_NAME,
        saleId = x.SALE_ID,
        saleNo = x.SALE == null ? null : x.SALE.SALE_NO,
        changeType = x.CHANGE_TYPE,
        changePoints = x.CHANGE_POINTS,
        remainPoints = x.REMAIN_POINTS,
        recordTime = x.RECORD_TIME,
        remark = x.REMARK
    });

    private static (int page, int size) NormalizePage(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
}
