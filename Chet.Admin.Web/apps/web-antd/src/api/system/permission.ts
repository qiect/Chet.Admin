import { requestClient } from '#/api/request';

/**
 * 分页查询权限列表
 * @param params 查询参数
 * @returns 权限分页列表,包含数据项和总记录数
 */
export async function getPermissionListApi(params: any) {
  const result = await requestClient.get('/permissions/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 获取全部权限列表
 * @returns 权限列表
 */
export async function getPermissionAllApi() {
  return requestClient.get('/permissions');
}

/**
 * 创建权限
 * @param data 权限信息
 * @returns 创建结果
 */
export async function createPermissionApi(data: any) {
  return requestClient.post('/permissions', data);
}

/**
 * 更新权限信息
 * @param id 权限ID
 * @param data 权限信息
 * @returns 更新结果
 */
export async function updatePermissionApi(id: number, data: any) {
  return requestClient.put(`/permissions/${id}`, data);
}

/**
 * 删除权限
 * @param id 权限ID
 * @returns 删除结果
 */
export async function deletePermissionApi(id: number) {
  return requestClient.delete(`/permissions/${id}`);
}
