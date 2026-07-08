using Chet.Admin.Domain;
using Chet.Admin.Domain.User;
using Chet.Admin.Domain.Role;
using Chet.Admin.Domain.Menu;
using Chet.Admin.Domain.Department;
using Chet.Admin.Domain.Dictionary;
using Chet.Admin.Domain.Audit;
using Chet.Admin.Domain.Notification;
using Chet.Admin.Domain.File;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data
{
    /// <summary>
    /// EF Core 数据库上下文类，用于管理实体和数据库交互
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文配置选项</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 表示数据库中的 Users 表
        /// </summary>
        public DbSet<UserEntity> Users { get; set; }

        /// <summary>
        /// 角色表
        /// </summary>
        public DbSet<RoleEntity> Roles { get; set; }

        /// <summary>
        /// 菜单表
        /// </summary>
        public DbSet<MenuEntity> Menus { get; set; }

        /// <summary>
        /// 部门表
        /// </summary>
        public DbSet<DepartmentEntity> Departments { get; set; }

        /// <summary>
        /// 字典表
        /// </summary>
        public DbSet<DictionaryEntity> Dictionaries { get; set; }

        /// <summary>
        /// 用户角色关联表
        /// </summary>
        public DbSet<UserRoleEntity> UserRoles { get; set; }

        /// <summary>
        /// 角色菜单关联表
        /// </summary>
        public DbSet<RoleMenuEntity> RoleMenus { get; set; }

        /// <summary>
        /// 角色自定义数据权限部门关联表
        /// </summary>
        public DbSet<RoleDataScopeDeptEntity> RoleDataScopeDepts { get; set; }

        /// <summary>
        /// 操作日志表
        /// </summary>
        public DbSet<AuditLogEntity> AuditLogs { get; set; }

        /// <summary>
        /// 通知公告表
        /// </summary>
        public DbSet<NotificationEntity> Notifications { get; set; }

        /// <summary>
        /// 通知接收者表
        /// </summary>
        public DbSet<NotificationRecipientEntity> NotificationRecipients { get; set; }

        /// <summary>
        /// 文件管理表
        /// </summary>
        public DbSet<FileEntity> Files { get; set; }

        /// <summary>
        /// 配置实体映射和关系
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 从当前程序集自动应用所有实现了 IEntityTypeConfiguration<T> 接口的配置类
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }

        /// <summary>
        /// 重写基类方法，用于自动设置实体的创建和更新时间
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>影响的行数</returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 自动设置创建和更新时间
            var entities = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entities)
            {
                var entity = (BaseEntity)entityEntry.Entity;
                entity.UpdatedAt = DateTime.Now; // 设置更新时间为当前 北京 时间

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.Now; // 新建实体时，设置创建时间为当前 北京 时间
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
