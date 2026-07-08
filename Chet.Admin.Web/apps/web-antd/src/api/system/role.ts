import { requestClient } from '#/api/request';

/**
 * 分页查询角色列表
 * @param params 查询参数
 * @returns 角色分页列表,包含数据项和总记录数
 */
export async function getRoleListApi(params: any) {
  const result = await requestClient.get('/roles/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 获取全部角色列表
 * @returns 角色列表
 */
export async function getRoleAllApi() {
  return requestClient.get('/roles');
}

/**
 * 创建角色
 * @param data 角色信息
 * @returns 创建结果
 */
export async function createRoleApi(data: any) {
  return requestClient.post('/roles', data);
}

/**
 * 更新角色信息
 * @param id 角色ID
 * @param data 角色信息
 * @returns 更新结果
 */
export async function updateRoleApi(id: number, data: any) {
  return requestClient.put(`/roles/${id}`, data);
}

/**
 * 删除角色
 * @param id 角色ID
 * @returns 删除结果
 */
export async function deleteRoleApi(id: number) {
  return requestClient.delete(`/roles/${id}`);
}

/**
 * 获取指定角色的权限列表
 * @param id 角色ID
 * @returns 角色权限列表
 */
export async function getRolePermissionsApi(id: number) {
  return requestClient.get(`/roles/${id}/permissions`);
}

/**
 * 为指定角色分配权限
 * @param id 角色ID
 * @param permissionIds 权限ID数组
 * @returns 分配结果
 */
export async function assignRolePermissionsApi(id: number, permissionIds: number[]) {
  return requestClient.post(`/roles/${id}/permissions`, permissionIds);
}

/**
 * 获取指定角色的菜单列表
 * @param id 角色ID
 * @returns 角色菜单列表
 */
export async function getRoleMenusApi(id: number) {
  return requestClient.get(`/roles/${id}/menus`);
}

/**
 * 为指定角色分配菜单
 * @param id 角色ID
 * @param menuIds 菜单ID数组
 * @returns 分配结果
 */
export async function assignRoleMenusApi(id: number, menuIds: number[]) {
  return requestClient.post(`/roles/${id}/menus`, menuIds);
}

/**
 * 获取全部权限列表(角色模块使用)
 * @returns 权限列表
 */
export async function getPermissionAllApi() {
  return requestClient.get('/permissions');
}

/**
 * 获取菜单树形结构(角色模块使用)
 * @returns 菜单树形数据
 */
export async function getMenuTreeApi() {
  return requestClient.get('/menus/tree');
}

/**
 * 更新角色数据权限范围
 * @param id 角色ID
 * @param data 数据权限配置,包含权限范围及自定义部门ID列表
 * @returns 更新结果
 */
export async function updateDataScopeApi(id: number, data: { dataScope: string; customDeptIds?: number[] }) {
  return requestClient.put(`/api/v1/roles/${id}/data-scope`, data);
}
