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
 * 将后端菜单数据转换为Vben Admin路由格式
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
      title: item.name,
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
 * 获取用户所有菜单
 */
export async function getAllMenusApi() {
  const backendMenus =
    await requestClient.get<BackendMenuItem[]>('/menus/tree');
  return transformMenuData(backendMenus);
}
