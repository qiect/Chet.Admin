using Chet.Admin.Domain.Menu;
using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Role
{
    /// <summary>
    /// 角色菜单关联配置
    /// </summary>
    public class RoleMenuConfig : IEntityTypeConfiguration<RoleMenuEntity>
    {
        /// <summary>
        /// 配置角色菜单关联实体映射
        /// </summary>
        /// <param name="builder">实体类型构建器</param>
        public void Configure(EntityTypeBuilder<RoleMenuEntity> builder)
        {
            builder.ToTable("RoleMenus", t => t.HasComment("角色菜单关联表"));
            builder.HasKey(rm => new { rm.RoleId, rm.MenuId });

            builder.HasOne(rm => rm.Role)
                .WithMany(r => r.RoleMenus)
                .HasForeignKey(rm => rm.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rm => rm.Menu)
                .WithMany(m => m.RoleMenus)
                .HasForeignKey(rm => rm.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
