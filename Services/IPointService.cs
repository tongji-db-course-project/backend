using backend.Dtos;

namespace backend.Services;

public interface IPointService
{
    Task<PageResult<PointRecordDto>> ListAsync(int page, int size, int? memberId, string? changeType, string? keyword);
    Task<MemberPointsDto> GetMemberPointsAsync(int memberId, int page, int size);
    Task<MemberPointsDto> AdjustAsync(int memberId, AdjustPointsRequest request);
}
