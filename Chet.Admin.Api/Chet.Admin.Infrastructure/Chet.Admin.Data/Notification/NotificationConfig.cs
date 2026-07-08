using Chet.Admin.Domain.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Notification;

/// <summary>
/// 通知配置
/// </summary>
public class NotificationConfig : IEntityTypeConfiguration<NotificationEntity>
{
    /// <summary>
    /// 配置通知实体映射
    /// </summary>
    /// <param name="builder">实体类型构建器</param>
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("Notifications", t => t.HasComment("通知公告表"));

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200).HasComment("标题");
        builder.Property(e => e.Content).IsRequired().HasMaxLength(4000).HasComment("内容");
        builder.Property(e => e.Type).IsRequired().HasMaxLength(20).HasComment("类型");
        builder.Property(e => e.Priority).IsRequired().HasMaxLength(20).HasComment("优先级");
        builder.Property(e => e.SenderId).HasComment("发送者ID");
        builder.Property(e => e.IsGlobal).HasComment("是否全局通知");
        builder.Property(e => e.CreatedAt).IsRequired().HasComment("创建时间");

        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Notifications_CreatedAt");
    }
}

/// <summary>
/// 通知接收者配置
/// </summary>
public class NotificationRecipientConfig : IEntityTypeConfiguration<NotificationRecipientEntity>
{
    /// <summary>
    /// 配置通知接收者实体映射
    /// </summary>
    /// <param name="builder">实体类型构建器</param>
    public void Configure(EntityTypeBuilder<NotificationRecipientEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("NotificationRecipients", t => t.HasComment("通知接收者表"));

        builder.Property(e => e.NotificationId).IsRequired().HasComment("通知ID");
        builder.Property(e => e.UserId).IsRequired().HasComment("用户ID");
        builder.Property(e => e.IsRead).HasComment("是否已读");
        builder.Property(e => e.ReadAt).HasComment("已读时间");

        builder.HasIndex(e => new { e.NotificationId, e.UserId }).IsUnique().HasDatabaseName("IX_NotificationRecipients_NotificationId_UserId");
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_NotificationRecipients_UserId");
    }
}
