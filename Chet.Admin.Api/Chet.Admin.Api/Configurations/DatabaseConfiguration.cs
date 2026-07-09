using Chet.Admin.Data;
using Chet.Admin.Domain.Audit;
using Chet.Admin.Domain.Role;
using Chet.Admin.Domain.Menu;
using Chet.Admin.Domain.Department;
using Chet.Admin.Domain.Dictionary;
using Chet.Admin.Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// 数据库配置扩展类
/// <para>
/// 提供数据库上下文的注册、初始化和迁移管理功能。
/// 支持Entity Framework Core的代码优先（Code-First）迁移策略，
/// 在应用启动时自动应用待处理的数据库迁移。
/// </para>
/// </summary>
/// <remarks>
/// <para>主要功能：</para>
/// <list type="number">
///   <item><description>注册DbContext到依赖注入容器</description></item>
///   <item><description>配置SQLite数据库连接</description></item>
///   <item><description>自动执行数据库迁移</description></item>
///   <item><description>种子数据初始化</description></item>
/// </list>
/// 
/// <para>迁移策略说明：</para>
/// <para>
/// 本项目使用Entity Framework Core的迁移功能来管理数据库Schema变更。
/// 与EnsureCreated()不同，MigrateAsync()支持增量式Schema更新，
/// 不会在检测到Schema变更时丢失现有数据。
/// </para>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 1. 在Program.cs中注册服务
/// builder.Services.ConfigureDatabase(builder.Configuration);
/// 
/// // 2. 在应用构建后初始化数据库
/// var app = builder.Build();
/// await app.InitializeDatabaseAsync();
/// </code>
/// </remarks>
public static class DatabaseConfiguration
{
    /// <summary>
    /// 配置并注册数据库上下文服务
    /// </summary>
    /// <param name="services">依赖注入服务集合</param>
    /// <param name="configuration">应用程序配置，用于读取连接字符串</param>
    /// <remarks>
    /// <para>配置详情：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>配置项</term>
    ///     <description>说明</description>
    ///   </listheader>
    ///   <item>
    ///     <term>数据库类型</term>
    ///     <description>SQLite（轻量级、文件型数据库，适合开发和测试环境）</description>
    ///   </item>
    ///   <item>
    ///     <term>连接字符串</term>
    ///     <description>从appsettings.json的ConnectionStrings:DefaultConnection读取</description>
    ///   </item>
    ///   <item>
    ///     <term>迁移程序集</term>
    ///     <description>指定包含迁移文件的程序集（AppDbContext所在程序集）</description>
    ///   </item>
    ///   <item>
    ///     <term>命令超时</term>
    ///     <description>30秒（防止长时间运行的查询阻塞应用）</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>生命周期：</para>
    /// <para>注册为Scoped作用域，每个HTTP请求创建一个实例。</para>
    /// 
    /// <para>示例配置（appsettings.json）：</para>
    /// <code>
    /// {
    ///   "ConnectionStrings": {
    ///     "DefaultConnection": "Data Source=Chet.Admin.db"
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlOptions.CommandTimeout(30);
                }));
    }

    /// <summary>
    /// 初始化数据库并应用挂起的迁移
    /// <para>
    /// 在应用启动时调用此方法，确保数据库Schema与当前模型一致。
    /// 如果存在未应用的迁移，将自动执行迁移操作。
    /// 迁移完成后，会检查是否需要初始化种子数据。
    /// </para>
    /// </summary>
    /// <param name="app">WebApplication实例，用于获取服务提供者</param>
    /// <returns>表示异步初始化操作的任务</returns>
    /// <exception cref="Exception">
    /// 当迁移执行失败时抛出异常。异常会被记录到日志中，
    /// 并重新抛出以阻止应用继续启动。
    /// </exception>
    /// <remarks>
    /// <para>执行流程：</para>
    /// <list type="number">
    ///   <item><description>创建新的服务作用域</description></item>
    ///   <item><description>从容器中获取DbContext和Logger实例</description></item>
    ///   <item><description>检查是否存在待处理的迁移</description></item>
    ///   <item><description>如果有迁移：执行MigrateAsync()</description></item>
    ///   <item><description>如果没有迁移：跳过，记录日志</description></item>
    ///   <item><description>调用SeedDataAsync()初始化种子数据</description></item>
    /// </list>
    /// 
    /// <para>何时会抛出异常？</para>
    /// <list type="bullet">
    ///   <item><description>数据库连接失败（如连接字符串错误、权限不足）</description></item>
    ///   <item><description>SQL语法错误（迁移脚本有问题）</description></item>
    ///   <item><description>并发冲突（多个实例同时尝试迁移）</description></item>
    /// </list>
    /// </remarks>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            logger.LogInformation("Initializing database...");

            if (dbContext.Database.GetPendingMigrations().Any())
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            else if (!dbContext.Database.CanConnect())
            {
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database created successfully");
            }
            else
            {
                logger.LogInformation("Database is up to date, no migrations needed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations");
            throw;
        }

        await SeedDataAsync(dbContext, logger);
    }

    /// <summary>
    /// 初始化种子数据
    /// <para>
    /// 检查数据库是否为空（无用户记录），如果是则插入初始数据。
    /// 种子数据通常包括：
    /// - 默认管理员账户
    /// - 系统配置项
    /// - 基础参考数据
    /// </para>
    /// </summary>
    /// <param name="dbContext">数据库上下文实例</param>
    /// <param name="logger">日志记录器实例</param>
    /// <returns>表示异步操作的任务</returns>
    /// <remarks>
    /// <para>幂等性保证：</para>
    /// <para>
    /// 此方法通过检查Users表是否为空来判断是否需要播种，
    /// 确保多次调用不会产生重复数据。
    /// 这种模式称为"幂等播种"（Idempotent Seeding）。
    /// </para>
    /// 
    /// <para>扩展建议：</para>
    /// <para>
    /// 可以根据业务需求在此方法中添加更多种子数据：
    /// <list type="bullet">
    ///   <item><description>创建默认角色和权限</description></item>
    ///   <item><description>插入系统配置参数</description></item>
    ///   <item><description>添加基础字典数据（如国家、地区列表）</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static async Task SeedDataAsync(AppDbContext dbContext, ILogger logger)
    {
        if (!await dbContext.Users.AnyAsync())
        {
            logger.LogInformation("Seeding initial data...");

            // 种子部门
            var rootDept = new DepartmentEntity
            {
                Name = "总公司",
                Code = "ROOT",
                ParentId = 0,
                Sort = 0,
                IsEnabled = true
            };
            await dbContext.Departments.AddAsync(rootDept);
            await dbContext.SaveChangesAsync(); // 先保存获取ID

            var techDept = new DepartmentEntity
            {
                Name = "技术部",
                Code = "TECH",
                ParentId = rootDept.Id,
                Sort = 1,
                IsEnabled = true
            };
            await dbContext.Departments.AddAsync(techDept);
            await dbContext.SaveChangesAsync();

            // 种子角色
            var adminRole = new RoleEntity
            {
                Code = "admin",
                Name = "超级管理员",
                Description = "拥有系统所有权限",
                Sort = 0,
                IsEnabled = true
            };
            var userRole = new RoleEntity
            {
                Code = "user",
                Name = "普通用户",
                Description = "拥有基本操作权限",
                Sort = 1,
                IsEnabled = true
            };
            await dbContext.Roles.AddRangeAsync(adminRole, userRole);
            await dbContext.SaveChangesAsync();

            // 种子菜单 - 一级目录
            var dashboardMenu = new MenuEntity
            {
                Name = "仪表盘",
                Path = "/dashboard",
                Component = "/dashboard/index",
                Icon = "lucide:layout-dashboard",
                ParentId = 0,
                Type = "Menu",
                Sort = 0,
                IsEnabled = true,
                IsVisible = true,
                Permission = "dashboard:view"
            };
            await dbContext.Menus.AddAsync(dashboardMenu);

            var systemMenu = new MenuEntity
            {
                Name = "系统管理",
                Path = "/system",
                Component = "BasicLayout",
                Icon = "lucide:settings",
                ParentId = 0,
                Type = "Directory",
                Sort = 98,
                IsEnabled = true,
                IsVisible = true
            };
            await dbContext.Menus.AddAsync(systemMenu);

            // 系统运维目录：组织权限以外的系统辅助/运维功能
            var opsMenu = new MenuEntity
            {
                Name = "系统运维",
                Path = "/system-ops",
                Component = "BasicLayout",
                Icon = "lucide:wrench",
                ParentId = 0,
                Type = "Directory",
                Sort = 99,
                IsEnabled = true,
                IsVisible = true
            };
            await dbContext.Menus.AddAsync(opsMenu);
            await dbContext.SaveChangesAsync();

            // 系统管理下的菜单（组织与权限）
            var userMenu = new MenuEntity
            {
                Name = "用户管理",
                Path = "/system/user",
                Component = "/system/user/index",
                Icon = "lucide:users",
                ParentId = systemMenu.Id,
                Type = "Menu",
                Sort = 1,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:user:list"
            };
            var roleMenu = new MenuEntity
            {
                Name = "角色管理",
                Path = "/system/role",
                Component = "/system/role/index",
                Icon = "lucide:shield",
                ParentId = systemMenu.Id,
                Type = "Menu",
                Sort = 2,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:role:list"
            };
            var menuMenu = new MenuEntity
            {
                Name = "菜单管理",
                Path = "/system/menu",
                Component = "/system/menu/index",
                Icon = "lucide:menu",
                ParentId = systemMenu.Id,
                Type = "Menu",
                Sort = 3,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:menu:list"
            };
            var deptMenu = new MenuEntity
            {
                Name = "部门管理",
                Path = "/system/department",
                Component = "/system/department/index",
                Icon = "lucide:building",
                ParentId = systemMenu.Id,
                Type = "Menu",
                Sort = 4,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:dept:list"
            };

            // 系统运维下的菜单（辅助与运维功能）
            var dictMenu = new MenuEntity
            {
                Name = "字典管理",
                Path = "/system/dictionary",
                Component = "/system/dictionary/index",
                Icon = "lucide:book-open",
                ParentId = opsMenu.Id,
                Type = "Menu",
                Sort = 1,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:dict:list"
            };
            var auditMenu = new MenuEntity
            {
                Name = "操作日志",
                Path = "/system/audit-log",
                Component = "/system/audit-log/index",
                Icon = "lucide:file-text",
                ParentId = opsMenu.Id,
                Type = "Menu",
                Sort = 2,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:audit:list"
            };
            var notificationMenu = new MenuEntity
            {
                Name = "通知管理",
                Path = "/system/notification",
                Component = "/system/notification/index",
                Icon = "lucide:bell",
                ParentId = opsMenu.Id,
                Type = "Menu",
                Sort = 3,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:notification:list"
            };
            var fileMenu = new MenuEntity
            {
                Name = "文件管理",
                Path = "/system/file",
                Component = "/system/file/index",
                Icon = "lucide:folder",
                ParentId = opsMenu.Id,
                Type = "Menu",
                Sort = 4,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:file:list"
            };
            var onlineMenu = new MenuEntity
            {
                Name = "在线用户",
                Path = "/system/online-user",
                Component = "/system/online-user/index",
                Icon = "lucide:wifi",
                ParentId = opsMenu.Id,
                Type = "Menu",
                Sort = 5,
                IsEnabled = true,
                IsVisible = true,
                Permission = "system:online:list"
            };
            await dbContext.Menus.AddRangeAsync(userMenu, roleMenu, menuMenu, deptMenu, dictMenu, auditMenu, notificationMenu, fileMenu, onlineMenu);
            await dbContext.SaveChangesAsync();

            // 按钮级菜单
            var userCreateBtn = new MenuEntity { Name = "新增用户", Path = "", Component = "", ParentId = userMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:user:create" };
            var userUpdateBtn = new MenuEntity { Name = "编辑用户", Path = "", Component = "", ParentId = userMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:user:update" };
            var userDeleteBtn = new MenuEntity { Name = "删除用户", Path = "", Component = "", ParentId = userMenu.Id, Type = "Button", Sort = 3, IsEnabled = true, IsVisible = false, Permission = "system:user:delete" };
            var roleCreateBtn = new MenuEntity { Name = "新增角色", Path = "", Component = "", ParentId = roleMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:role:create" };
            var roleUpdateBtn = new MenuEntity { Name = "编辑角色", Path = "", Component = "", ParentId = roleMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:role:update" };
            var roleDeleteBtn = new MenuEntity { Name = "删除角色", Path = "", Component = "", ParentId = roleMenu.Id, Type = "Button", Sort = 3, IsEnabled = true, IsVisible = false, Permission = "system:role:delete" };
            var menuCreateBtn = new MenuEntity { Name = "新增菜单", Path = "", Component = "", ParentId = menuMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:menu:create" };
            var menuUpdateBtn = new MenuEntity { Name = "编辑菜单", Path = "", Component = "", ParentId = menuMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:menu:update" };
            var menuDeleteBtn = new MenuEntity { Name = "删除菜单", Path = "", Component = "", ParentId = menuMenu.Id, Type = "Button", Sort = 3, IsEnabled = true, IsVisible = false, Permission = "system:menu:delete" };
            var deptCreateBtn = new MenuEntity { Name = "新增部门", Path = "", Component = "", ParentId = deptMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:dept:create" };
            var deptUpdateBtn = new MenuEntity { Name = "编辑部门", Path = "", Component = "", ParentId = deptMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:dept:update" };
            var deptDeleteBtn = new MenuEntity { Name = "删除部门", Path = "", Component = "", ParentId = deptMenu.Id, Type = "Button", Sort = 3, IsEnabled = true, IsVisible = false, Permission = "system:dept:delete" };
            var dictCreateBtn = new MenuEntity { Name = "新增字典", Path = "", Component = "", ParentId = dictMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:dict:create" };
            var dictUpdateBtn = new MenuEntity { Name = "编辑字典", Path = "", Component = "", ParentId = dictMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:dict:update" };
            var dictDeleteBtn = new MenuEntity { Name = "删除字典", Path = "", Component = "", ParentId = dictMenu.Id, Type = "Button", Sort = 3, IsEnabled = true, IsVisible = false, Permission = "system:dict:delete" };
            var auditListBtn = new MenuEntity { Name = "查看日志", Path = "", Component = "", ParentId = auditMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:audit:list" };
            var auditClearBtn = new MenuEntity { Name = "清除日志", Path = "", Component = "", ParentId = auditMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:audit:clear" };
            var notifCreateBtn = new MenuEntity { Name = "新增通知", Path = "", Component = "", ParentId = notificationMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:notification:create" };
            var notifDeleteBtn = new MenuEntity { Name = "删除通知", Path = "", Component = "", ParentId = notificationMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:notification:delete" };
            var onlineListBtn = new MenuEntity { Name = "查看在线用户", Path = "", Component = "", ParentId = onlineMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:online:list" };
            var onlineForceOfflineBtn = new MenuEntity { Name = "强制下线", Path = "", Component = "", ParentId = onlineMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:online:force-offline" };
            var fileUploadBtn = new MenuEntity { Name = "上传文件", Path = "", Component = "", ParentId = fileMenu.Id, Type = "Button", Sort = 1, IsEnabled = true, IsVisible = false, Permission = "system:file:upload" };
            var fileDeleteBtn = new MenuEntity { Name = "删除文件", Path = "", Component = "", ParentId = fileMenu.Id, Type = "Button", Sort = 2, IsEnabled = true, IsVisible = false, Permission = "system:file:delete" };
            await dbContext.Menus.AddRangeAsync(
                userCreateBtn, userUpdateBtn, userDeleteBtn,
                roleCreateBtn, roleUpdateBtn, roleDeleteBtn,
                menuCreateBtn, menuUpdateBtn, menuDeleteBtn,
                deptCreateBtn, deptUpdateBtn, deptDeleteBtn,
                dictCreateBtn, dictUpdateBtn, dictDeleteBtn,
                auditListBtn, auditClearBtn,
                notifCreateBtn, notifDeleteBtn,
                onlineListBtn, onlineForceOfflineBtn,
                fileUploadBtn, fileDeleteBtn
            );
            await dbContext.SaveChangesAsync();

            // 管理员角色分配所有菜单
            var allMenus = await dbContext.Menus.ToListAsync();

            foreach (var menu in allMenus)
            {
                await dbContext.RoleMenus.AddAsync(new RoleMenuEntity { RoleId = adminRole.Id, MenuId = menu.Id });
            }
            await dbContext.SaveChangesAsync();

            // 普通用户角色分配查看菜单
            var viewMenus = allMenus.Where(m => m.Type == "Menu" || m.Type == "Directory").ToList();
            foreach (var menu in viewMenus)
            {
                await dbContext.RoleMenus.AddAsync(new RoleMenuEntity { RoleId = userRole.Id, MenuId = menu.Id });
            }
            await dbContext.SaveChangesAsync();

            // 种子字典 - 父级类型
            var menuTypeDict = new DictionaryEntity { DictType = "menu_type", Name = "菜单类型", Value = "", Label = "菜单类型", ParentId = 0, Sort = 1 };
            var statusDict = new DictionaryEntity { DictType = "status", Name = "状态", Value = "", Label = "状态", ParentId = 0, Sort = 3 };
            var userStatusDict = new DictionaryEntity { DictType = "user_status", Name = "用户状态", Value = "", Label = "用户状态", ParentId = 0, Sort = 4 };
            var genderDict = new DictionaryEntity { DictType = "gender", Name = "性别", Value = "", Label = "性别", ParentId = 0, Sort = 5 };
            var yesNoDict = new DictionaryEntity { DictType = "yes_no", Name = "是否", Value = "", Label = "是否", ParentId = 0, Sort = 6 };
            await dbContext.Dictionaries.AddRangeAsync(menuTypeDict, statusDict, userStatusDict, genderDict, yesNoDict);
            await dbContext.SaveChangesAsync();

            // 种子字典 - 子级项
            var dictItems = new List<DictionaryEntity>
            {
                // menu_type 子项
                new() { DictType = "menu_type", Name = "菜单类型-目录", Value = "Directory", Label = "目录", ParentId = menuTypeDict.Id, Sort = 1 },
                new() { DictType = "menu_type", Name = "菜单类型-菜单", Value = "Menu", Label = "菜单", ParentId = menuTypeDict.Id, Sort = 2 },
                new() { DictType = "menu_type", Name = "菜单类型-按钮", Value = "Button", Label = "按钮", ParentId = menuTypeDict.Id, Sort = 3 },
                // status 子项
                new() { DictType = "status", Name = "状态-启用", Value = "1", Label = "启用", ParentId = statusDict.Id, Sort = 1 },
                new() { DictType = "status", Name = "状态-禁用", Value = "0", Label = "禁用", ParentId = statusDict.Id, Sort = 2 },
                // user_status 子项
                new() { DictType = "user_status", Name = "启用", Value = "1", Label = "启用", ParentId = userStatusDict.Id, IsEnabled = true, Sort = 1 },
                new() { DictType = "user_status", Name = "禁用", Value = "0", Label = "禁用", ParentId = userStatusDict.Id, IsEnabled = true, Sort = 2 },
                // gender 子项
                new() { DictType = "gender", Name = "男", Value = "1", Label = "男", ParentId = genderDict.Id, IsEnabled = true, Sort = 1 },
                new() { DictType = "gender", Name = "女", Value = "2", Label = "女", ParentId = genderDict.Id, IsEnabled = true, Sort = 2 },
                new() { DictType = "gender", Name = "未知", Value = "0", Label = "未知", ParentId = genderDict.Id, IsEnabled = true, Sort = 3 },
                // yes_no 子项
                new() { DictType = "yes_no", Name = "是", Value = "1", Label = "是", ParentId = yesNoDict.Id, IsEnabled = true, Sort = 1 },
                new() { DictType = "yes_no", Name = "否", Value = "0", Label = "否", ParentId = yesNoDict.Id, IsEnabled = true, Sort = 2 },
            };
            await dbContext.Dictionaries.AddRangeAsync(dictItems);
            await dbContext.SaveChangesAsync();

            // 创建管理员用户（隶属总公司）
            var adminUser = new UserEntity
            {
                Name = "超级管理员",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                DepartmentId = rootDept.Id
            };
            await dbContext.Users.AddAsync(adminUser);
            await dbContext.SaveChangesAsync();

            // 管理员用户分配管理员角色
            await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = adminUser.Id, RoleId = adminRole.Id });
            await dbContext.SaveChangesAsync();

            // 创建普通用户（只有查看权限，无增删改按钮权限）
            var normalUser = new UserEntity
            {
                Name = "普通用户",
                Email = "user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                DepartmentId = techDept.Id
            };
            await dbContext.Users.AddAsync(normalUser);
            await dbContext.SaveChangesAsync();

            // 普通用户分配普通用户角色（userRole 已只分配 Menu/Directory，不含按钮）
            await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = normalUser.Id, RoleId = userRole.Id });
            await dbContext.SaveChangesAsync();

            // 种子近 7 天登录审计日志，让仪表盘登录趋势图有数据展示
            var rand = new Random(20260709);
            var seedAuditLogs = new List<AuditLogEntity>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-i);
                // 每天随机 2-8 次登录，模拟真实使用情况
                var count = rand.Next(2, 9);
                for (int j = 0; j < count; j++)
                {
                    seedAuditLogs.Add(new AuditLogEntity
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.Name,
                        Action = "Login",
                        Module = "Auth",
                        Description = "用户登录",
                        HttpMethod = "POST",
                        RequestPath = "/api/v1/auth/login",
                        StatusCode = 200,
                        ClientIp = "127.0.0.1",
                        OperatedAt = day.AddHours(rand.Next(8, 22)).AddMinutes(rand.Next(0, 60)),
                        Duration = rand.Next(50, 300),
                    });
                }
            }
            await dbContext.AuditLogs.AddRangeAsync(seedAuditLogs);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Initial data seeded successfully");
        }

        // 恢复菜单结构：如果存在"系统设置"，将其子菜单移回"系统管理"，然后删除"系统设置"
        var existingSettingMenu = await dbContext.Menus.FirstOrDefaultAsync(m => m.Name == "系统设置");
        if (existingSettingMenu != null)
        {
            var existingSystemMenu = await dbContext.Menus.FirstOrDefaultAsync(m => m.Name == "系统管理");
            if (existingSystemMenu != null)
            {
                // 恢复系统管理图标
                existingSystemMenu.Icon = "lucide:settings";

                // 将系统设置下的菜单移回系统管理
                var settingMenus = await dbContext.Menus
                    .Where(m => m.ParentId == existingSettingMenu.Id)
                    .ToListAsync();

                var pathRestoreMapping = new Dictionary<string, string>
                {
                    { "菜单管理", "/system/menu" },
                    { "部门管理", "/system/department" },
                    { "字典管理", "/system/dictionary" },
                    { "通知管理", "/system/notification" },
                    { "文件管理", "/system/file" }
                };

                foreach (var menu in settingMenus)
                {
                    menu.ParentId = existingSystemMenu.Id;
                    if (pathRestoreMapping.TryGetValue(menu.Name, out var restorePath))
                    {
                        menu.Path = restorePath;
                    }
                }

                // 调整排序
                var sortMapping = new Dictionary<string, int>
                {
                    { "用户管理", 1 }, { "角色管理", 2 }, { "菜单管理", 3 },
                    { "部门管理", 4 }, { "字典管理", 6 },
                    { "操作日志", 7 }, { "通知管理", 8 }, { "文件管理", 9 },
                    { "在线用户", 10 }
                };
                var allSystemMenus = await dbContext.Menus
                    .Where(m => m.ParentId == existingSystemMenu.Id)
                    .ToListAsync();
                foreach (var menu in allSystemMenus)
                {
                    if (sortMapping.TryGetValue(menu.Name, out var sort))
                    {
                        menu.Sort = sort;
                    }
                }

                await dbContext.SaveChangesAsync();

                // 删除系统设置菜单的角色关联
                var settingRoleMenus = await dbContext.RoleMenus
                    .Where(rm => rm.MenuId == existingSettingMenu.Id)
                    .ToListAsync();
                dbContext.RoleMenus.RemoveRange(settingRoleMenus);

                // 删除系统设置菜单
                dbContext.Menus.Remove(existingSettingMenu);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Menu structure restored: merged 系统设置 back into 系统管理");
            }
        }
    }
}
