using Chet.Admin.Domain.Permission;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Permission
{
    public class PermissionConfig : IEntityTypeConfiguration<PermissionEntity>
    {
        public void Configure(EntityTypeBuilder<PermissionEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Permissions", t => t.HasComment("权限表"));

            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasComment("权限编码");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasComment("权限名称");
            builder.Property(e => e.Type).IsRequired().HasMaxLength(20).HasComment("权限类型");
            builder.Property(e => e.Description).HasMaxLength(500).HasComment("权限描述");
            builder.Property(e => e.MenuId).HasComment("关联菜单ID");

            builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_Permissions_Code");
        }
    }
}
