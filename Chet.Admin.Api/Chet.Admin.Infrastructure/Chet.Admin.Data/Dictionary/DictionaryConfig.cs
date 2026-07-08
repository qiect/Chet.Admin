using Chet.Admin.Domain.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.Dictionary
{
    public class DictionaryConfig : IEntityTypeConfiguration<DictionaryEntity>
    {
        public void Configure(EntityTypeBuilder<DictionaryEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Dictionaries", t => t.HasComment("字典表"));

            builder.Property(e => e.DictType).IsRequired().HasMaxLength(50).HasComment("字典类型编码");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasComment("字典名称");
            builder.Property(e => e.Value).IsRequired().HasMaxLength(100).HasComment("字典值");
            builder.Property(e => e.Label).IsRequired().HasMaxLength(100).HasComment("字典标签");
            builder.Property(e => e.Sort).HasComment("排序号");
            builder.Property(e => e.IsEnabled).HasComment("是否启用");
            builder.Property(e => e.Remark).HasMaxLength(500).HasComment("备注");
            builder.Property(e => e.ParentId).HasComment("父级ID");

            builder.HasIndex(e => e.DictType).HasDatabaseName("IX_Dictionaries_DictType");
        }
    }
}
