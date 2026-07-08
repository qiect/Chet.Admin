using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Role
{
    /// <summary>
    /// 角色配置
    /// </summary>
    public class RoleConfig : IEntityTypeConfiguration<RoleEntity>
    {
        /// <summary>
        /// 配置角色实体映射
        /// </summary>
        /// <param name="builder">实体类型构建器</param>
        public void Configure(EntityTypeBuilder<RoleEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Roles", t => t.HasComment("角色表"));

            builder.Property(e => e.Code).IsRequired().HasMaxLength(50).HasComment("角色编码");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasComment("角色名称");
            builder.Property(e => e.Description).HasMaxLength(500).HasComment("角色描述");
            builder.Property(e => e.Sort).HasComment("排序号");
            builder.Property(e => e.IsEnabled).HasComment("是否启用");
            builder.Property(e => e.DataScope).IsRequired().HasMaxLength(20).HasDefaultValue("All").HasComment("数据权限范围：All/Dept/DeptAndChild/Self/Custom");

            builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_Roles_Code");
        }
    }
}
