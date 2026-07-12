import type { RouteRecordStringComponent } from '@vben/types';

import { requestClient } from '#/api/request';

/** 后端菜单树数据结构 */
interface BackendMenuItem {
  id: number;
  name: string;
  path: string;
  component?: string | null;
  redirect?: string | null;
  icon?: string | null;
  parentId: number;
  type: string;
  sort: number;
  isEnabled: boolean;
  isExternal: boolean;
  isCache: boolean;
  isVisible: boolean;
  permission?: string | null;
  children?: BackendMenuItem[];
}

/**
 * 将菜单路径转换为i18n key(扁平key,避免嵌套冲突)
 * /system/user → menu.system-user
 * /dashboard → menu.dashboard
 */
export function pathToI18nKey(path: string): string {
  const key = path.split('/').filter(Boolean).join('-');
  return `menu.${key}`;
}

/**
 * 将后端菜单数据转换为Vben Admin路由格式
 * 菜单标题使用path生成的i18n key,支持多语言切换
 */
function transformMenuData(
  menus: BackendMenuItem[],
): RouteRecordStringComponent[] {
  return menus
    .filter((item) => item.isEnabled && item.type !== 'Button')
    .map((item) => transformMenuItem(item));
}

function transformMenuItem(
  item: BackendMenuItem,
): RouteRecordStringComponent {
  const route: RouteRecordStringComponent = {
    name: item.path
      .split('/')
      .filter(Boolean)
      .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
      .join(''),
    path: item.path,
    component: item.component || undefined,
    meta: {
      title: item.path ? pathToI18nKey(item.path) : item.name,
      icon: item.icon || undefined,
      order: item.sort,
      hideInMenu: !item.isVisible,
      keepAlive: item.isCache,
      ...(item.isExternal ? { link: item.path } : {}),
    },
  };

  if (item.redirect) {
    route.redirect = item.redirect;
  }

  if (item.children && item.children.length > 0) {
    const filteredChildren = item.children.filter(
      (child) => child.isEnabled && child.type !== 'Button',
    );
    if (filteredChildren.length > 0) {
      route.children = transformMenuData(item.children);
    }
  }

  return route;
}

/**
 * 获取用户所有菜单（不过滤，包含全部菜单）
 */
export async function getAllMenusApi() {
  const backendMenus =
    await requestClient.get<BackendMenuItem[]>('/menus/tree');
  return transformMenuData(backendMenus);
}

/**
 * 获取当前登录用户的菜单（按角色权限过滤）
 */
export async function getMyMenusApi() {
  const backendMenus =
    await requestClient.get<BackendMenuItem[]>('/menus/my-tree');
  return transformMenuData(backendMenus);
}
