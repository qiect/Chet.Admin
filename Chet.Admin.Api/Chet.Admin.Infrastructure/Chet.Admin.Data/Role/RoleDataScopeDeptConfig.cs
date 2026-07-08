using Chet.Admin.Domain.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Role;

public class RoleDataScopeDeptConfig : IEntityTypeConfiguration<RoleDataScopeDeptEntity>
{
    public void Configure(EntityTypeBuilder<RoleDataScopeDeptEntity> builder)
    {
        builder.ToTable("RoleDataScopeDepts", t => t.HasComment("角色自定义数据权限部门关联表"));
        builder.HasKey(rd => rd.Id);

        builder.Property(rd => rd.RoleId).HasComment("角色ID");
        builder.Property(rd => rd.DepartmentId).HasComment("部门ID");

        builder.HasIndex(rd => new { rd.RoleId, rd.DepartmentId }).IsUnique().HasDatabaseName("IX_RoleDataScopeDepts_Role_Dept");

        builder.HasOne(rd => rd.Role)
            .WithMany(r => r.RoleDataScopeDepts)
            .HasForeignKey(rd => rd.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rd => rd.Department)
            .WithMany()
            .HasForeignKey(rd => rd.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
