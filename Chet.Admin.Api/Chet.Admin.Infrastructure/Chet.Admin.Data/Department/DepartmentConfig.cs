using Chet.Admin.Domain.Department;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Department
{
    /// <summary>
    /// 部门配置
    /// </summary>
    public class DepartmentConfig : IEntityTypeConfiguration<DepartmentEntity>
    {
        /// <summary>
        /// 配置部门实体映射
        /// </summary>
        /// <param name="builder">实体类型构建器</param>
        public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Departments", t => t.HasComment("部门表"));

            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasComment("部门名称");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50).HasComment("部门编码");
            builder.Property(e => e.Leader).HasMaxLength(50).HasComment("负责人");
            builder.Property(e => e.Phone).HasMaxLength(20).HasComment("联系电话");
            builder.Property(e => e.Email).HasMaxLength(100).HasComment("邮箱");
            builder.Property(e => e.ParentId).HasComment("父部门ID");
            builder.Property(e => e.Sort).HasComment("排序号");
            builder.Property(e => e.IsEnabled).HasComment("是否启用");

            builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_Departments_Code");
        }
    }
}
