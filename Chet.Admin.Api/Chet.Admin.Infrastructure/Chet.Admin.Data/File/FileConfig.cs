using Chet.Admin.Domain.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chet.Admin.Data.File;

public class FileConfig : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("Files", t => t.HasComment("文件管理表"));

        builder.Property(e => e.FileName).IsRequired().HasMaxLength(255).HasComment("原始文件名");
        builder.Property(e => e.StoredName).IsRequired().HasMaxLength(255).HasComment("存储文件名");
        builder.Property(e => e.FilePath).IsRequired().HasMaxLength(500).HasComment("文件路径");
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100).HasComment("内容类型");
        builder.Property(e => e.FileSize).HasComment("文件大小(字节)");
        builder.Property(e => e.Description).HasMaxLength(500).HasComment("描述");
        builder.Property(e => e.UploaderId).HasComment("上传者ID");
        builder.Property(e => e.CreatedAt).IsRequired().HasComment("创建时间");

        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Files_CreatedAt");
    }
}
