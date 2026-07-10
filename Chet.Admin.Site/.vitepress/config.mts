import { defineConfig } from 'vitepress'

// Chet.Admin 官方文档站点配置
export default defineConfig({
  lang: 'zh-CN',
  title: 'Chet.Admin',
  description: '基于 .NET 10 + Vue 3 的企业级 RBAC 权限管理系统',

  // GitHub Pages 部署需要设置 base 为仓库名
  base: '/Chet.Admin/',

  lastUpdated: true,
  cleanUrls: true,

  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/Chet.Admin/logo.svg' }],
    ['meta', { name: 'theme-color', content: '#0066FF' }],
  ],

  themeConfig: {
    logo: '/logo.svg',

    siteTitle: 'Chet.Admin',

    nav: [
      { text: '指南', link: '/guide/introduction', activeMatch: '/guide/' },
      { text: '功能模块', link: '/modules/overview', activeMatch: '/modules/' },
      { text: 'API 文档', link: '/api/response-format', activeMatch: '/api/' },
      {
        text: '文章',
        link: '/articles/overview',
        activeMatch: '/articles/',
      },
      { text: '赞助', link: '/sponsor', activeMatch: '/sponsor' },
    ],

    sidebar: {
      '/guide/': [
        {
          text: '开始',
          collapsed: false,
          items: [
            { text: '项目简介', link: '/guide/introduction' },
            { text: '系统架构', link: '/guide/architecture' },
            { text: '快速开始', link: '/guide/quick-start' },
          ],
        },
        {
          text: '开发指南',
          collapsed: false,
          items: [
            { text: '后端开发指南', link: '/guide/backend' },
            { text: '前端开发指南', link: '/guide/frontend' },
            { text: '数据库设计', link: '/guide/database' },
          ],
        },
        {
          text: '其他',
          collapsed: false,
          items: [{ text: '部署指南', link: '/guide/deployment' }],
        },
      ],
      '/modules/': [
        {
          text: '功能模块',
          collapsed: false,
          items: [
            { text: '模块总览', link: '/modules/overview' },
            { text: '认证登录', link: '/modules/auth' },
            { text: '用户管理', link: '/modules/user' },
            { text: '角色管理', link: '/modules/role' },
            { text: '菜单管理', link: '/modules/menu' },
            { text: '部门管理', link: '/modules/department' },
            { text: '权限管理', link: '/modules/permission' },
            { text: '字典管理', link: '/modules/dictionary' },
            { text: '操作日志', link: '/modules/audit-log' },
            { text: '通知公告', link: '/modules/notification' },
            { text: '文件上传', link: '/modules/file-upload' },
            { text: '在线用户', link: '/modules/online-user' },
          ],
        },
      ],
      '/api/': [
        {
          text: 'API 文档',
          collapsed: false,
          items: [
            { text: '统一响应格式', link: '/api/response-format' },
            { text: '认证机制', link: '/api/authentication' },
            { text: '接口清单', link: '/api/endpoints' },
          ],
        },
      ],
      '/articles/': [
        {
          text: '系列文章',
          collapsed: false,
          items: [
            { text: '文章总览', link: '/articles/overview' },
            { text: '开篇总览', link: '/articles/01-overview' },
            { text: '快速上手', link: '/articles/02-quick-start' },
            { text: '项目结构全解析', link: '/articles/03-project-structure' },
            { text: '后端分层架构', link: '/articles/04-backend-architecture' },
            { text: '安全基石 JWT 认证', link: '/articles/05-jwt-auth' },
            { text: '权限模型 RBAC 三层防护', link: '/articles/06-rbac' },
            { text: '模块详解：认证登录', link: '/articles/07-auth-module' },
            { text: '模块详解：用户管理', link: '/articles/08-user-module' },
            { text: '模块详解：角色与权限', link: '/articles/09-role-module' },
            { text: '模块详解：菜单管理', link: '/articles/10-menu-module' },
            { text: '模块详解：部门管理', link: '/articles/11-department-module' },
            { text: '模块详解：字典管理', link: '/articles/12-dictionary-module' },
            { text: '模块详解：仪表盘', link: '/articles/13-dashboard-module' },
            { text: '模块详解：操作日志', link: '/articles/14-audit-log-module' },
            { text: '模块详解：通知公告', link: '/articles/15-notification-module' },
            { text: '模块详解：文件上传', link: '/articles/16-file-upload-module' },
            { text: '模块详解：在线用户', link: '/articles/17-online-user-module' },
            { text: '时间时区统一方案', link: '/articles/18-timezone' },
            { text: '项目重命名指南', link: '/articles/19-rename' },
            { text: '部署上线', link: '/articles/20-deployment' },
          ],
        },
      ],
    },

    socialLinks: [{ icon: 'github', link: 'https://github.com/qiect/Chet.Admin' }],

    footer: {
      message: '基于 MIT 协议发布',
      copyright: 'Copyright © 2026 Chet.Admin',
    },

    search: {
      provider: 'local',
      options: {
        translations: {
          button: {
            buttonText: '搜索文档',
            buttonAriaLabel: '搜索文档',
          },
          modal: {
            noResultsText: '无法找到相关结果',
            resetButtonTitle: '清除查询条件',
            footer: {
              selectText: '选择',
              navigateText: '切换',
            },
          },
        },
      },
    },

    outline: {
      label: '本页目录',
      level: [2, 3],
    },

    docFooter: {
      prev: '上一页',
      next: '下一页',
    },

    lastUpdated: {
      text: '最后更新于',
    },

    returnToTopLabel: '回到顶部',
    sidebarMenuLabel: '菜单',
    darkModeSwitchLabel: '主题',
    lightModeSwitchTitle: '切换到浅色模式',
    darkModeSwitchTitle: '切换到深色模式',
  },
})
