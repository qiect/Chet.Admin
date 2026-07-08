using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Role
{
    /// <summary>
    /// 角色权限关联配置
    /// </summary>
    public class RolePermissionConfig : IEntityTypeConfiguration<RolePermissionEntity>
    {
        /// <summary>
        /// 配置角色权限关联实体映射
        /// </summary>
        /// <param name="builder">实体类型构建器</param>
        public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
        {
            builder.ToTable("RolePermissions", t => t.HasComment("角色权限关联表"));
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
