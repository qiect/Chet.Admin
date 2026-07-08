using Chet.Admin.Domain.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.User
{
    /// <summary>
    /// 用户配置
    /// </summary>
    public class UserConfig : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.HasKey(e => e.Id); // 设置主键
            builder.ToTable("Users");
            builder.ToTable(e => e.HasComment("用户信息表"));

            builder.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("Name")
                    .HasComment("用户名");

            builder.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("Email")
                .HasComment("用户邮箱，用于登录和通知");

            builder.Property(e => e.PasswordHash)
                .IsRequired()
                .HasColumnName("PasswordHash")
                .HasComment("密码哈希值，使用 BCrypt 算法生成");

            builder.Property(e => e.RefreshToken)
                .HasMaxLength(500)
                .HasColumnName("RefreshToken")
                .HasComment("刷新令牌，用于获取新的访问令牌");

            builder.Property(e => e.RefreshTokenExpiryTime)
                .HasColumnName("RefreshTokenExpiryTime")
                .HasComment("刷新令牌过期时间");

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasComment("创建时间");

            builder.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnName("UpdatedAt")
                .HasComment("更新时间");

            builder.Property(e => e.Avatar)
                .HasMaxLength(500)
                .HasColumnName("Avatar")
                .HasComment("用户头像URL");

            builder.Property(e => e.DepartmentId).HasComment("部门ID");
            builder.HasIndex(e => e.DepartmentId).HasDatabaseName("IX_Users_DepartmentId");

            builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_Users_Email");

            builder.Property(e => e.LoginFailCount)
                .HasDefaultValue(0)
                .HasColumnName("LoginFailCount")
                .HasComment("连续登录失败次数");

            builder.Property(e => e.LockedUntil)
                .HasColumnName("LockedUntil")
                .HasComment("锁定截止时间");

            builder.Property(e => e.PasswordChangedAt)
                .HasColumnName("PasswordChangedAt")
                .HasComment("密码最后修改时间");

            builder.Property(e => e.MustChangePassword)
                .HasDefaultValue(false)
                .HasColumnName("MustChangePassword")
                .HasComment("是否需要强制修改密码");
        }
    }
}
