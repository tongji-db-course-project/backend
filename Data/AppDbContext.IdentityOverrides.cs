using Microsoft.EntityFrameworkCore;

namespace backend.Data;

/// <summary>
/// 本地实体配置补充。
/// 测试种子数据显式写入主键，但数据库 Identity 未同步，因此销售模块统一由应用分配主键。
/// 独立于 DB-First 生成文件，重新 scaffold 时不会覆盖此配置。
/// </summary>
public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.SALE_ORDER>()
            .Property(x => x.SALE_ID)
            .ValueGeneratedNever();

        modelBuilder.Entity<Models.SALE_ORDER_DETAIL>()
            .Property(x => x.SALE_DETAIL_ID)
            .ValueGeneratedNever();

        modelBuilder.Entity<Models.INVENTORY_RECORD>()
            .Property(x => x.RECORD_ID)
            .ValueGeneratedNever();

        modelBuilder.Entity<Models.POINT_RECORD>()
            .Property(x => x.POINT_RECORD_ID)
            .ValueGeneratedNever();
    }
}
