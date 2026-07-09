// <copyright file="Program.cs" company="Chet.Admin">
// Copyright (c) Chet.Admin. All rights reserved.
// </copyright>

using Chet.Admin.Api.Configurations;
using Chet.Admin.Api.Filters;
using Chet.Admin.Api.Middleware;
using Chet.Admin.Configuration;
using Chet.Admin.Mapping.User;
using Chet.Admin.Shared.Api;
using Microsoft.Extensions.FileProviders;
using Serilog;

/*
 * ASP.NET Core Web API 应用程序入口点
 *
 * 本文件是应用程序的启动配置中心，负责：
 * - 初始化日志系统（Serilog）
 * - 配置依赖注入服务
 * - 构建中间件管道
 * - 启动HTTP服务器监听
 *
 * 项目架构概览：
 * 本项目采用分层架构（Layered Architecture）设计模式：
 *   - Api（表示层）：控制器、中间件、配置、DTO
 *   - Application（应用层）：业务逻辑、服务、映射、DTO定义
 *   - Infrastructure（基础设施层）：数据访问、缓存、日志、外部服务集成
 *   - Core（核心层）：领域实体、接口、共享工具、契约
 *
 * 技术栈：
 *   - .NET 10 + C# 12
 *   - Entity Framework Core 8/9 (SQLite)
 *   - Redis缓存（可选，支持NoOp降级）
 *   - JWT身份认证
 *   - AutoMapper对象映射
 *   - Serilog结构化日志
 *   - Swagger/OpenAPI文档
 *   - Docker容器化部署
 */

// ============================================
// 第一阶段：初始化和前置检查
// ============================================

Log.Information("Starting application...");

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSerilog();


var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
builder.Services.AddSingleton(appSettings!);

// ============================================
// 第二阶段：服务注册（依赖注入配置）
// ============================================

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // SQLite 存储的 DateTime 读回时 Kind=Unspecified，System.Text.Json 默认序列化时不带 Z 后缀
    // 这里统一指定 Kind=Utc，使输出带 Z，前端 new Date() 可正确按 UTC 解析并转本地时区显示
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
});

builder.Services.ConfigureApiVersioning();

builder.Services.ConfigureSwagger();

builder.Services.ConfigureDatabase(builder.Configuration);

builder.Services.ConfigureRedis(appSettings);

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();

builder.Services.ConfigureFluentValidation();

builder.Services.ConfigureJwt(appSettings);

builder.Services.ConfigureCors(builder.Configuration);

// 内存缓存（验证码等）
builder.Services.AddMemoryCache();

// ============================================
// 第三阶段：构建应用实例
// ============================================

var app = builder.Build();

// ============================================
// 第四阶段：数据库初始化
// ============================================

await app.InitializeDatabaseAsync();

// ============================================
// 第五阶段：中间件管道配置
// ============================================

app.ConfigureExceptionHandling();

// 添加日志上下文中间件，为所有请求添加上下文信息
app.UseLogContext();

app.UseCors("DefaultPolicy");

app.UseRateLimiting();

// 开发环境禁用HTTPS重定向，避免Vite代理请求被重定向到HTTPS端口
// app.UseHttpsRedirection();

app.ConfigureSwaggerUI();

app.ConfigureAuthMiddleware(appSettings);

// 添加操作日志中间件，记录API写操作
app.UseMiddleware<AuditLogMiddleware>();

// 添加在线用户追踪中间件，刷新用户活跃时间
app.UseMiddleware<OnlineUserTrackingMiddleware>();

var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

// ============================================
// 第六阶段：启动应用
// ============================================

Log.Information("Application started successfully. Listening on {Urls}", app.Urls);
app.Run();
