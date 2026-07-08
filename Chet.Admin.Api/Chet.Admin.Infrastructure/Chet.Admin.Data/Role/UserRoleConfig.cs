using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Role
{
    /// <summary>
    /// 用户角色关联配置
    /// </summary>
    public class UserRoleConfig : IEntityTypeConfiguration<UserRoleEntity>
    {
        /// <summary>
        /// 配置用户角色关联实体映射
        /// </summary>
        /// <param name="builder">实体类型构建器</param>
        public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
        {
            builder.ToTable("UserRoles", t => t.HasComment("用户角色关联表"));
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
