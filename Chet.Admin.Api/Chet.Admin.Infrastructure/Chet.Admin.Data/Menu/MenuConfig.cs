using Chet.Admin.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Menu
{
    /// <summary>
    /// 菜单配置
    /// </summary>
    public class MenuConfig : IEntityTypeConfiguration<MenuEntity>
    {
        /// <summary>
        /// 配置菜单实体映射
        /// </summary>
        /// <param name="builder">实体类型构建器</param>
        public void Configure(EntityTypeBuilder<MenuEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Menus", t => t.HasComment("菜单表"));

            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasComment("菜单名称");
            builder.Property(e => e.Path).HasMaxLength(255).HasComment("菜单路径");
            builder.Property(e => e.Component).HasMaxLength(255).HasComment("组件路径");
            builder.Property(e => e.Redirect).HasMaxLength(255).HasComment("重定向路径");
            builder.Property(e => e.Icon).HasMaxLength(100).HasComment("图标");
            builder.Property(e => e.ParentId).HasComment("父菜单ID");
            builder.Property(e => e.Type).IsRequired().HasMaxLength(20).HasComment("菜单类型");
            builder.Property(e => e.Sort).HasComment("排序号");
            builder.Property(e => e.IsEnabled).HasComment("是否启用");
            builder.Property(e => e.IsExternal).HasComment("是否外链");
            builder.Property(e => e.IsCache).HasComment("是否缓存");
            builder.Property(e => e.IsVisible).HasComment("是否显示");
            builder.Property(e => e.Permission).HasMaxLength(100).HasComment("权限标识");

            builder.HasIndex(e => e.ParentId).HasDatabaseName("IX_Menus_ParentId");
        }
    }
}
