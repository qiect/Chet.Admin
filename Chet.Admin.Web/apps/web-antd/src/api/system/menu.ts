import { requestClient } from '#/api/request';

/**
 * 获取全部菜单列表
 * @returns 菜单列表
 */
export async function getAllMenusApi() {
  return requestClient.get('/menus');
}

/**
 * 获取菜单树形结构
 * @returns 菜单树形数据
 */
export async function getMenuTreeApi() {
  return requestClient.get('/menus/tree');
}

/**
 * 创建菜单
 * @param data 菜单信息
 * @returns 创建结果
 */
export async function createMenuApi(data: any) {
  return requestClient.post('/menus', data);
}

/**
 * 更新菜单信息
 * @param id 菜单ID
 * @param data 菜单信息
 * @returns 更新结果
 */
export async function updateMenuApi(id: number, data: any) {
  return requestClient.put(`/menus/${id}`, data);
}

/**
 * 删除菜单
 * @param id 菜单ID
 * @returns 删除结果
 */
export async function deleteMenuApi(id: number) {
  return requestClient.delete(`/menus/${id}`);
}
