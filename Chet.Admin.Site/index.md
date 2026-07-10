---
layout: home

hero:
  name: Chet.Admin
  text: 前后端分离的管理系统框架
  tagline: .NET 10 + Vben Admin，开箱即用、简单轻量
  image:
    src: /logo.svg
    alt: Chet.Admin
  actions:
    - theme: brand
      text: 快速开始
      link: /guide/quick-start
    - theme: alt
      text: 项目简介
      link: /guide/introduction
    - theme: alt
      text: GitHub
      link: https://github.com/qiect/Chet.Admin

features:
  - icon: 🏗️
    title: 企业级架构
    details: 后端 Clean Architecture + DDD 分层，前端 Vben Admin Monorepo（pnpm + Turbo），职责清晰、易于维护。
    link: /guide/architecture
    linkText: 查看详情
  - icon: 🔐
    title: 完善权限体系
    details: 菜单级路由权限 + 按钮级操作权限 + 行级数据权限（5 种范围），覆盖企业级 RBAC 全场景。
    link: /modules/permission
    linkText: 查看详情
  - icon: 🐳
    title: 开箱即用
    details: 内置种子数据启动即可登录，原生支持 Docker / Docker Compose 一键部署后端 + Redis。
    link: /guide/quick-start
    linkText: 查看详情
  - icon: 🔒
    title: 安全可靠
    details: JWT 双令牌认证、BCrypt 密码哈希、限流防护、验证码、登录锁定、密码过期策略。
    link: /modules/auth
    linkText: 查看详情
  - icon: 📊
    title: 可观测与审计
    details: Serilog 结构化日志、操作审计日志、在线用户追踪、通知公告，系统运行状态一目了然。
    link: /modules/audit-log
    linkText: 查看详情
  - icon: 🎨
    title: 现代化 UI
    details: 基于 Ant Design Vue，支持浅色 / 深色主题切换，内置 13 个核心功能模块。
    link: /modules/overview
    linkText: 查看详情
---
