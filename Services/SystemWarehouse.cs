using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 单仓库模式的唯一仓库解析入口。
/// </summary>
public static class SystemWarehouse
{
    public static async Task<int> GetIdAsync(AppDbContext db, int? compatibleWarehouseId = null)
    {
        if (compatibleWarehouseId <= 0)
            throw new BusinessException(400, "仓库编号必须大于0");

        var warehouseIds = await db.WAREHOUSEs
            .AsNoTracking()
            .Where(x => x.STATUS == "启用")
            .OrderBy(x => x.WAREHOUSE_ID)
            .Select(x => x.WAREHOUSE_ID)
            .Take(2)
            .ToListAsync();

        var systemWarehouseId = warehouseIds.Count switch
        {
            0 => throw new BusinessException(409, "系统未配置启用仓库"),
            > 1 => throw new BusinessException(409, "单仓库模式下只能配置一个启用仓库"),
            _ => warehouseIds[0]
        };

        if (compatibleWarehouseId.HasValue && compatibleWarehouseId.Value != systemWarehouseId)
            throw new BusinessException(409, "warehouseId 已弃用，且只能传入系统唯一启用仓库编号");

        return systemWarehouseId;
    }
}
