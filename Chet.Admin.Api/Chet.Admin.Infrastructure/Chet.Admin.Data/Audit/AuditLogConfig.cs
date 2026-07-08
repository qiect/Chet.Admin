using Chet.Admin.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Audit;

/// <summary>
/// 操作日志配置
/// </summary>
public class AuditLogConfig : IEntityTypeConfiguration<AuditLogEntity>
{
    /// <summary>
    /// 配置操作日志实体映射
    /// </summary>
    /// <param name="builder">实体类型构建器</param>
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("AuditLogs");
        builder.ToTable(e => e.HasComment("操作日志表"));

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasComment("操作用户ID");

        builder.Property(e => e.UserName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("操作用户名");

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("操作类型");

        builder.Property(e => e.Module)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("操作模块");

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("操作描述");

        builder.Property(e => e.TargetId)
            .HasMaxLength(100)
            .HasComment("操作目标ID");

        builder.Property(e => e.HttpMethod)
            .IsRequired()
            .HasMaxLength(10)
            .HasComment("HTTP方法");

        builder.Property(e => e.RequestPath)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("请求路径");

        builder.Property(e => e.RequestData)
            .HasMaxLength(4000)
            .HasComment("请求数据");

        builder.Property(e => e.ClientIp)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("客户端IP");

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500)
            .HasComment("用户代理");

        builder.Property(e => e.OperatedAt)
            .IsRequired()
            .HasComment("操作时间");

        builder.HasIndex(e => e.OperatedAt).HasDatabaseName("IX_AuditLogs_OperatedAt");
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_AuditLogs_UserId");
    }
}
