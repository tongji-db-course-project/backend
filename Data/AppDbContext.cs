using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<INVENTORY> INVENTORies { get; set; }

    public virtual DbSet<INVENTORY_RECORD> INVENTORY_RECORDs { get; set; }

    public virtual DbSet<MEMBER> MEMBERs { get; set; }

    public virtual DbSet<POINT_RECORD> POINT_RECORDs { get; set; }

    public virtual DbSet<PRODUCT> PRODUCTs { get; set; }

    public virtual DbSet<PRODUCT_CATEGORY> PRODUCT_CATEGORies { get; set; }

    public virtual DbSet<PURCHASE_ORDER> PURCHASE_ORDERs { get; set; }

    public virtual DbSet<PURCHASE_ORDER_DETAIL> PURCHASE_ORDER_DETAILs { get; set; }

    public virtual DbSet<RETURN_ORDER> RETURN_ORDERs { get; set; }

    public virtual DbSet<RETURN_ORDER_DETAIL> RETURN_ORDER_DETAILs { get; set; }

    public virtual DbSet<SALE_ORDER> SALE_ORDERs { get; set; }

    public virtual DbSet<SALE_ORDER_DETAIL> SALE_ORDER_DETAILs { get; set; }

    public virtual DbSet<SUPPLIER> SUPPLIERs { get; set; }

    public virtual DbSet<SUPPLIER_SETTLEMENT> SUPPLIER_SETTLEMENTs { get; set; }

    public virtual DbSet<SYS_MENU> SYS_MENUs { get; set; }

    public virtual DbSet<SYS_ROLE> SYS_ROLEs { get; set; }

    public virtual DbSet<SYS_ROLE_MENU> SYS_ROLE_MENUs { get; set; }

    public virtual DbSet<SYS_USER> SYS_USERs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("RETAIL_ADMIN")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<INVENTORY>(entity =>
        {
            entity.HasKey(e => e.INVENTORY_ID).HasName("SYS_C008707");

            entity.ToTable("INVENTORY");

            entity.HasIndex(e => e.PRODUCT_ID, "IDX_INVENTORY_PRODUCT");

            entity.Property(e => e.INVENTORY_ID)
                .HasPrecision(10)
                .HasComment("库存编号");
            entity.Property(e => e.CURRENT_STOCK)
                .HasPrecision(10)
                .HasComment("当前库存量");
            entity.Property(e => e.LAST_UPDATE_TIME)
                .HasComment("最后更新时间")
                .HasColumnType("DATE");
            entity.Property(e => e.PRODUCT_ID)
                .HasPrecision(10)
                .HasComment("商品编号，关联商品表");

            entity.HasOne(d => d.PRODUCT).WithMany(p => p.INVENTORies)
                .HasForeignKey(d => d.PRODUCT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INVENTORY_PRODUCT");
        });

        modelBuilder.Entity<INVENTORY_RECORD>(entity =>
        {
            entity.HasKey(e => e.RECORD_ID).HasName("SYS_C008716");

            entity.ToTable("INVENTORY_RECORD");

            entity.HasIndex(e => e.OPERATOR_ID, "IDX_IR_OPERATOR");

            entity.HasIndex(e => e.PRODUCT_ID, "IDX_IR_PRODUCT");

            entity.Property(e => e.RECORD_ID)
                .HasPrecision(10)
                .HasComment("流水编号");
            entity.Property(e => e.CHANGE_QTY)
                .HasPrecision(10)
                .HasComment("变动数量");
            entity.Property(e => e.OPERATOR_ID)
                .HasPrecision(10)
                .HasComment("操作人编号");
            entity.Property(e => e.PRODUCT_ID)
                .HasPrecision(10)
                .HasComment("商品编号");
            entity.Property(e => e.RECORD_TIME)
                .HasComment("记录时间")
                .HasColumnType("DATE");
            entity.Property(e => e.RECORD_TYPE)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("流水类型(入库/销售/退货/盘点)");
            entity.Property(e => e.REMAIN_QTY)
                .HasPrecision(10)
                .HasComment("变动后库存");
            entity.Property(e => e.REMARK)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasComment("备注");
            entity.Property(e => e.SOURCE_NO)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("来源单号");

            entity.HasOne(d => d.OPERATOR).WithMany(p => p.INVENTORY_RECORDs)
                .HasForeignKey(d => d.OPERATOR_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INV_RECORD_USER");

            entity.HasOne(d => d.PRODUCT).WithMany(p => p.INVENTORY_RECORDs)
                .HasForeignKey(d => d.PRODUCT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INV_RECORD_PRODUCT");
        });

        modelBuilder.Entity<MEMBER>(entity =>
        {
            entity.HasKey(e => e.MEMBER_ID);

            entity.ToTable("MEMBER", tb => tb.HasComment("会员信息表"));

            entity.HasIndex(e => e.PHONE, "UK_MEMBER_PHONE").IsUnique();

            entity.Property(e => e.MEMBER_ID)
                .HasPrecision(10)
                .HasComment("会员唯一标识 (主键)");
            entity.Property(e => e.BIRTHDAY).HasColumnType("DATE");
            entity.Property(e => e.CREATE_TIME)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE");
            entity.Property(e => e.GENDER)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LEVEL_NAME)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'普通会员'")
                .HasComment("会员等级（硬编码文本）");
            entity.Property(e => e.MEMBER_NAME)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PHONE)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("会员手机号 (唯一约束)");
            entity.Property(e => e.POINTS)
                .HasPrecision(10)
                .HasDefaultValueSql("0")
                .HasComment("当前剩余积分");
            entity.Property(e => e.TOTAL_AMOUNT)
                .HasDefaultValueSql("0.00")
                .HasColumnType("NUMBER(12,2)");
        });

        modelBuilder.Entity<POINT_RECORD>(entity =>
        {
            entity.HasKey(e => e.POINT_RECORD_ID).HasName("SYS_C008747");

            entity.ToTable("POINT_RECORD", tb => tb.HasComment("积分流水表：记录会员积分的每一次增减变动历史"));

            entity.HasIndex(e => e.MEMBER_ID, "IDX_PR_MEMBER");

            entity.HasIndex(e => e.SALE_ID, "IDX_PR_SALE");

            entity.Property(e => e.POINT_RECORD_ID).HasPrecision(10);
            entity.Property(e => e.CHANGE_POINTS).HasPrecision(10);
            entity.Property(e => e.CHANGE_TYPE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MEMBER_ID).HasPrecision(10);
            entity.Property(e => e.RECORD_TIME)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE");
            entity.Property(e => e.REMAIN_POINTS).HasPrecision(10);
            entity.Property(e => e.REMARK)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SALE_ID)
                .HasPrecision(10)
                .HasComment("关联销售单，记录是哪笔交易产生的积分变动；若是其他活动导致积分变化时可为空");

            entity.HasOne(d => d.MEMBER).WithMany(p => p.POINT_RECORDs)
                .HasForeignKey(d => d.MEMBER_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PR_MEMBER");

            entity.HasOne(d => d.SALE).WithMany(p => p.POINT_RECORDs)
                .HasForeignKey(d => d.SALE_ID)
                .HasConstraintName("FK_PR_SALE");
        });

        modelBuilder.Entity<PRODUCT>(entity =>
        {
            entity.HasKey(e => e.PRODUCT_ID).HasName("SYS_C008671");

            entity.ToTable("PRODUCT", tb => tb.HasComment("商品基础资料表"));

            entity.HasIndex(e => e.CATEGORY_ID, "IDX_PRODUCT_CATEGORY");

            entity.HasIndex(e => e.SUPPLIER_ID, "IDX_PRODUCT_SUPPLIER");

            entity.HasIndex(e => e.BARCODE, "SYS_C008672").IsUnique();

            entity.Property(e => e.PRODUCT_ID).HasPrecision(10);
            entity.Property(e => e.BARCODE)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CATEGORY_ID).HasPrecision(10);
            entity.Property(e => e.PRODUCT_NAME)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PURCHASE_PRICE)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.SALE_PRICE)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.SPECIFICATION)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'在售'");
            entity.Property(e => e.STOCK_WARNING)
                .HasPrecision(10)
                .HasDefaultValueSql("10");
            entity.Property(e => e.SUPPLIER_ID).HasPrecision(10);
            entity.Property(e => e.UNIT)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.CATEGORY).WithMany(p => p.PRODUCTs)
                .HasForeignKey(d => d.CATEGORY_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PROD_CATEGORY");

            entity.HasOne(d => d.SUPPLIER).WithMany(p => p.PRODUCTs)
                .HasForeignKey(d => d.SUPPLIER_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PROD_SUPPLIER");
        });

        modelBuilder.Entity<PRODUCT_CATEGORY>(entity =>
        {
            entity.HasKey(e => e.CATEGORY_ID).HasName("SYS_C008658");

            entity.ToTable("PRODUCT_CATEGORY", tb => tb.HasComment("商品类别表"));

            entity.HasIndex(e => e.CATEGORY_NAME, "SYS_C008659").IsUnique();

            entity.Property(e => e.CATEGORY_ID).HasPrecision(10);
            entity.Property(e => e.CATEGORY_DESC)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CATEGORY_NAME)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("类别名称，如食品、日用品等");
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'启用'");
        });

        modelBuilder.Entity<PURCHASE_ORDER>(entity =>
        {
            entity.HasKey(e => e.ORDER_ID);

            entity.ToTable("PURCHASE_ORDER", tb => tb.HasComment("采购订单主表"));

            entity.HasIndex(e => e.APPLICANT_ID, "IDX_PO_APPLICANT");

            entity.HasIndex(e => e.APPROVER_ID, "IDX_PO_APPROVER");

            entity.HasIndex(e => e.SUPPLIER_ID, "IDX_PO_SUPPLIER");

            entity.HasIndex(e => e.ORDER_CODE, "UK_PURCHASE_ORDER_CODE").IsUnique();

            entity.Property(e => e.ORDER_ID)
                .HasPrecision(10)
                .HasComment("采购订单唯一标识 (主键)");
            entity.Property(e => e.APPLICANT_ID)
                .HasPrecision(10)
                .HasComment("申请用户ID (关联sys_user表)");
            entity.Property(e => e.APPROVER_ID)
                .HasPrecision(10)
                .HasComment("审批用户ID (关联sys_user表)");
            entity.Property(e => e.ORDER_CODE)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("采购单据编码 (系统生成, 唯一)");
            entity.Property(e => e.PURCHASE_DATE)
                .HasDefaultValueSql("TRUNC(SYSDATE)")
                .HasColumnType("DATE");
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'待审批'")
                .HasComment("单据状态");
            entity.Property(e => e.SUPPLIER_ID)
                .HasPrecision(10)
                .HasComment("供应商ID (关联supplier表)");
            entity.Property(e => e.TOTAL_AMOUNT).HasColumnType("NUMBER(12,2)");

            entity.HasOne(d => d.APPLICANT).WithMany(p => p.PURCHASE_ORDERAPPLICANTs)
                .HasForeignKey(d => d.APPLICANT_ID)
                .HasConstraintName("FK_PO_APPLICANT");

            entity.HasOne(d => d.APPROVER).WithMany(p => p.PURCHASE_ORDERAPPROVERs)
                .HasForeignKey(d => d.APPROVER_ID)
                .HasConstraintName("FK_PO_APPROVER");

            entity.HasOne(d => d.SUPPLIER).WithMany(p => p.PURCHASE_ORDERs)
                .HasForeignKey(d => d.SUPPLIER_ID)
                .HasConstraintName("FK_PO_SUPPLIER");
        });

        modelBuilder.Entity<PURCHASE_ORDER_DETAIL>(entity =>
        {
            entity.HasKey(e => e.PURCHASE_DETAIL_ID).HasName("SYS_C008686");

            entity.ToTable("PURCHASE_ORDER_DETAIL", tb => tb.HasComment("采购订单明细表"));

            entity.HasIndex(e => e.PRODUCT_ID, "IDX_POD_PRODUCT");

            entity.HasIndex(e => e.PURCHASE_ID, "IDX_POD_PURCHASE");

            entity.Property(e => e.PURCHASE_DETAIL_ID)
                .HasPrecision(10)
                .HasComment("明细编号");
            entity.Property(e => e.PRODUCT_ID)
                .HasPrecision(10)
                .HasComment("商品编号");
            entity.Property(e => e.PURCHASE_ID)
                .HasPrecision(10)
                .HasComment("采购单编号");
            entity.Property(e => e.PURCHASE_PRICE)
                .HasComment("采购单价")
                .HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.PURCHASE_QUANTITY)
                .HasPrecision(10)
                .HasComment("采购数量");

            entity.HasOne(d => d.PRODUCT).WithMany(p => p.PURCHASE_ORDER_DETAILs)
                .HasForeignKey(d => d.PRODUCT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PUR_DETAIL_PRODUCT");

            entity.HasOne(d => d.PURCHASE).WithMany(p => p.PURCHASE_ORDER_DETAILs)
                .HasForeignKey(d => d.PURCHASE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PUR_DETAIL_PURCHASE");
        });

        modelBuilder.Entity<RETURN_ORDER>(entity =>
        {
            entity.HasKey(e => e.RETURN_ID).HasName("SYS_C008727");

            entity.ToTable("RETURN_ORDER");

            entity.HasIndex(e => e.MEMBER_ID, "IDX_RETURN_MEMBER");

            entity.HasIndex(e => e.OPERATOR_ID, "IDX_RETURN_OPERATOR");

            entity.HasIndex(e => e.SALE_ID, "IDX_RETURN_SALE");

            entity.HasIndex(e => e.RETURN_NO, "UK_RETURN_ORDER_NO").IsUnique();

            entity.Property(e => e.RETURN_ID)
                .HasPrecision(10)
                .HasComment("退货单编号");
            entity.Property(e => e.MEMBER_ID)
                .HasPrecision(10)
                .HasComment("会员编号");
            entity.Property(e => e.OPERATOR_ID)
                .HasPrecision(10)
                .HasComment("经办人编号");
            entity.Property(e => e.REFUND_AMOUNT)
                .HasComment("退款金额")
                .HasColumnType("NUMBER(12,2)");
            entity.Property(e => e.REMARK)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasComment("备注");
            entity.Property(e => e.RETURN_DATE)
                .HasComment("退货日期")
                .HasColumnType("DATE");
            entity.Property(e => e.RETURN_NO)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("退货单号");
            entity.Property(e => e.SALE_ID)
                .HasPrecision(10)
                .HasComment("对应销售单编号");
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'待处理' ")
                .HasComment("退货状态");

            entity.HasOne(d => d.MEMBER).WithMany(p => p.RETURN_ORDERs)
                .HasForeignKey(d => d.MEMBER_ID)
                .HasConstraintName("FK_RETURN_MEMBER");

            entity.HasOne(d => d.OPERATOR).WithMany(p => p.RETURN_ORDERs)
                .HasForeignKey(d => d.OPERATOR_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RETURN_OPERATOR");

            entity.HasOne(d => d.SALE).WithMany(p => p.RETURN_ORDERs)
                .HasForeignKey(d => d.SALE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RETURN_SALE");
        });

        modelBuilder.Entity<RETURN_ORDER_DETAIL>(entity =>
        {
            entity.HasKey(e => e.RETURN_DETAIL_ID).HasName("SYS_C008738");

            entity.ToTable("RETURN_ORDER_DETAIL", tb => tb.HasComment("退货单明细表：记录每一笔退货业务中包含的具体商品、数量及退款单价"));

            entity.HasIndex(e => e.PRODUCT_ID, "IDX_ROD_PRODUCT");

            entity.HasIndex(e => e.RETURN_ID, "IDX_ROD_RETURN");

            entity.Property(e => e.RETURN_DETAIL_ID).HasPrecision(10);
            entity.Property(e => e.PRODUCT_ID).HasPrecision(10);
            entity.Property(e => e.QUANTITY).HasPrecision(10);
            entity.Property(e => e.REFUND_PRICE)
                .HasComment("退货单价")
                .HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.RETURN_ID).HasPrecision(10);
            entity.Property(e => e.SUBTOTAL)
                .HasComment("该笔商品退款总计金额")
                .HasColumnType("NUMBER(12,2)");

            entity.HasOne(d => d.PRODUCT).WithMany(p => p.RETURN_ORDER_DETAILs)
                .HasForeignKey(d => d.PRODUCT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ROD_PRODUCT");

            entity.HasOne(d => d.RETURN).WithMany(p => p.RETURN_ORDER_DETAILs)
                .HasForeignKey(d => d.RETURN_ID)
                .HasConstraintName("FK_ROD_RETURN");
        });

        modelBuilder.Entity<SALE_ORDER>(entity =>
        {
            entity.HasKey(e => e.SALE_ID).HasName("SYS_C008693");

            entity.ToTable("SALE_ORDER", tb => tb.HasComment("销售订单表"));

            entity.HasIndex(e => e.MEMBER_ID, "IDX_SALE_MEMBER");

            entity.HasIndex(e => e.USER_ID, "IDX_SALE_USER");

            entity.HasIndex(e => e.SALE_NO, "UK_SALE_NO").IsUnique();

            entity.Property(e => e.SALE_ID)
                .HasPrecision(10)
                .HasComment("销售单编号");
            entity.Property(e => e.DISCOUNT_AMOUNT)
                .HasComment("优惠金额")
                .HasColumnType("NUMBER(12,2)");
            entity.Property(e => e.MEMBER_ID)
                .HasPrecision(10)
                .HasComment("会员编号");
            entity.Property(e => e.PAID_AMOUNT)
                .HasComment("实付金额")
                .HasColumnType("NUMBER(12,2)");
            entity.Property(e => e.PAY_TYPE)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("支付方式");
            entity.Property(e => e.SALE_DATE)
                .HasComment("销售日期")
                .HasColumnType("DATE");
            entity.Property(e => e.SALE_NO)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("销售单号");
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'待支付'")
                .HasComment("销售单状态");
            entity.Property(e => e.TOTAL_AMOUNT)
                .HasComment("原始总金额")
                .HasColumnType("NUMBER(12,2)");
            entity.Property(e => e.USER_ID)
                .HasPrecision(10)
                .HasComment("收银员编号");

            entity.HasOne(d => d.MEMBER).WithMany(p => p.SALE_ORDERs)
                .HasForeignKey(d => d.MEMBER_ID)
                .HasConstraintName("FK_SALE_MEMBER");

            entity.HasOne(d => d.USER).WithMany(p => p.SALE_ORDERs)
                .HasForeignKey(d => d.USER_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SALE_USER");
        });

        modelBuilder.Entity<SALE_ORDER_DETAIL>(entity =>
        {
            entity.HasKey(e => e.SALE_DETAIL_ID).HasName("SYS_C008700");

            entity.ToTable("SALE_ORDER_DETAIL", tb => tb.HasComment("销售单明细表"));

            entity.HasIndex(e => e.PRODUCT_ID, "IDX_SOD_PRODUCT");

            entity.HasIndex(e => e.SALE_ID, "IDX_SOD_SALE");

            entity.Property(e => e.SALE_DETAIL_ID)
                .HasPrecision(10)
                .HasComment("明细编号");
            entity.Property(e => e.PRODUCT_ID)
                .HasPrecision(10)
                .HasComment("商品编号");
            entity.Property(e => e.SALE_ID)
                .HasPrecision(10)
                .HasComment("销售单编号");
            entity.Property(e => e.SALE_PRICE)
                .HasComment("销售单价")
                .HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.SALE_QUANTITY)
                .HasPrecision(10)
                .HasComment("销售数量");

            entity.HasOne(d => d.PRODUCT).WithMany(p => p.SALE_ORDER_DETAILs)
                .HasForeignKey(d => d.PRODUCT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SALE_DETAIL_PRODUCT");

            entity.HasOne(d => d.SALE).WithMany(p => p.SALE_ORDER_DETAILs)
                .HasForeignKey(d => d.SALE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SALE_DETAIL_SALE");
        });

        modelBuilder.Entity<SUPPLIER>(entity =>
        {
            entity.HasKey(e => e.SUPPLIER_ID);

            entity.ToTable("SUPPLIER", tb => tb.HasComment("供应商信息表"));

            entity.Property(e => e.SUPPLIER_ID)
                .HasPrecision(10)
                .HasComment("供应商唯一标识 (主键)");
            entity.Property(e => e.ADDRESS)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CONTACT_NAME)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CREDIT_LEVEL)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("供应商信誉等级");
            entity.Property(e => e.EMAIL)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PAYMENT_CYCLE)
                .HasPrecision(5)
                .HasComment("约定结算周期（天数）");
            entity.Property(e => e.PHONE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SUPPLIER_NAME)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SUPPLIER_SETTLEMENT>(entity =>
        {
            entity.HasKey(e => e.SETTLEMENT_ID).HasName("SYS_C008757");

            entity.ToTable("SUPPLIER_SETTLEMENT", tb => tb.HasComment("供应商结算表：管理与供应商的财务结账情况"));

            entity.HasIndex(e => e.PURCHASE_ID, "IDX_SS_PURCHASE");

            entity.HasIndex(e => e.SUPPLIER_ID, "IDX_SS_SUPPLIER");

            entity.Property(e => e.SETTLEMENT_ID).HasPrecision(10);
            entity.Property(e => e.PAID_AMOUNT)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMBER(12,2)");
            entity.Property(e => e.PURCHASE_ID).HasPrecision(10);
            entity.Property(e => e.REMARK)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SETTLEMENT_AMOUNT).HasColumnType("NUMBER(12,2)");
            entity.Property(e => e.SETTLEMENT_DATE)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE");
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SUPPLIER_ID).HasPrecision(10);
            entity.Property(e => e.UNPAID_AMOUNT).HasColumnType("NUMBER(12,2)");

            entity.HasOne(d => d.PURCHASE).WithMany(p => p.SUPPLIER_SETTLEMENTs)
                .HasForeignKey(d => d.PURCHASE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SS_PURCHASE");

            entity.HasOne(d => d.SUPPLIER).WithMany(p => p.SUPPLIER_SETTLEMENTs)
                .HasForeignKey(d => d.SUPPLIER_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SS_SUPPLIER");
        });

        modelBuilder.Entity<SYS_MENU>(entity =>
        {
            entity.HasKey(e => e.MENU_ID).HasName("SYS_C008637");

            entity.ToTable("SYS_MENU", tb => tb.HasComment("菜单功能表"));

            entity.Property(e => e.MENU_ID)
                .HasPrecision(10)
                .HasComment("菜单编号");
            entity.Property(e => e.MENU_NAME)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("菜单名称");
            entity.Property(e => e.MENU_ORDER)
                .HasPrecision(5)
                .HasComment("菜单顺序");
            entity.Property(e => e.MENU_URL)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("菜单路径");
            entity.Property(e => e.PARENT_ID)
                .HasPrecision(10)
                .HasComment("上级菜单编号");
        });

        modelBuilder.Entity<SYS_ROLE>(entity =>
        {
            entity.HasKey(e => e.ROLE_ID).HasName("SYS_C008633");

            entity.ToTable("SYS_ROLE", tb => tb.HasComment("角色表"));

            entity.HasIndex(e => e.ROLE_NAME, "SYS_C008634").IsUnique();

            entity.Property(e => e.ROLE_ID)
                .HasPrecision(10)
                .HasComment("角色编号");
            entity.Property(e => e.ROLE_DESC)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasComment("角色说明");
            entity.Property(e => e.ROLE_NAME)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("角色名称");
        });

        modelBuilder.Entity<SYS_ROLE_MENU>(entity =>
        {
            entity.HasKey(e => e.ROLE_MENU_ID).HasName("SYS_C008648");

            entity.ToTable("SYS_ROLE_MENU", tb => tb.HasComment("权限中间表：定义哪些角色可以访问哪些菜单"));

            entity.HasIndex(e => e.MENU_ID, "IDX_SRM_MENU");

            entity.HasIndex(e => e.ROLE_ID, "IDX_SRM_ROLE");

            entity.HasIndex(e => new { e.ROLE_ID, e.MENU_ID }, "UK_ROLE_MENU").IsUnique();

            entity.Property(e => e.ROLE_MENU_ID).HasPrecision(10);
            entity.Property(e => e.MENU_ID).HasPrecision(10);
            entity.Property(e => e.ROLE_ID).HasPrecision(10);

            entity.HasOne(d => d.MENU).WithMany(p => p.SYS_ROLE_MENUs)
                .HasForeignKey(d => d.MENU_ID)
                .HasConstraintName("FK_RM_MENU");

            entity.HasOne(d => d.ROLE).WithMany(p => p.SYS_ROLE_MENUs)
                .HasForeignKey(d => d.ROLE_ID)
                .HasConstraintName("FK_RM_ROLE");
        });

        modelBuilder.Entity<SYS_USER>(entity =>
        {
            entity.HasKey(e => e.USER_ID).HasName("SYS_C008642");

            entity.ToTable("SYS_USER", tb => tb.HasComment("用户表"));

            entity.HasIndex(e => e.ROLE_ID, "IDX_SYS_USER_ROLE");

            entity.HasIndex(e => e.USERNAME, "SYS_C008643").IsUnique();

            entity.Property(e => e.USER_ID)
                .HasPrecision(10)
                .HasComment("用户编号");
            entity.Property(e => e.CREATE_TIME)
                .HasDefaultValueSql("SYSDATE")
                .HasComment("创建时间")
                .HasColumnType("DATE");
            entity.Property(e => e.GENDER)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("性别");
            entity.Property(e => e.PASSWORD)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("登录密码");
            entity.Property(e => e.PHONE)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("联系电话");
            entity.Property(e => e.REAL_NAME)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("真实姓名");
            entity.Property(e => e.ROLE_ID)
                .HasPrecision(10)
                .HasComment("角色编号");
            entity.Property(e => e.STATUS)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("用户状态");
            entity.Property(e => e.USERNAME)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("登录账号");

            entity.HasOne(d => d.ROLE).WithMany(p => p.SYS_USERs)
                .HasForeignKey(d => d.ROLE_ID)
                .HasConstraintName("FK_USER_ROLE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
