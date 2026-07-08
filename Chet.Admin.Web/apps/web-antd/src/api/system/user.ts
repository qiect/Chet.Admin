import { requestClient } from '#/api/request';

/**
 * 分页查询用户列表
 * @param params 查询参数
 * @returns 用户分页列表,包含数据项和总记录数
 */
export async function getUserListApi(params: any) {
  const result = await requestClient.get('/users/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 根据用户ID获取用户信息
 * @param id 用户ID
 * @returns 用户信息
 */
export async function getUserByIdApi(id: number) {
  return requestClient.get(`/users/${id}`);
}

/**
 * 创建用户
 * @param data 用户信息
 * @returns 创建结果
 */
export async function createUserApi(data: any) {
  return requestClient.post('/users', data);
}

/**
 * 更新用户信息
 * @param id 用户ID
 * @param data 用户信息
 * @returns 更新结果
 */
export async function updateUserApi(id: number, data: any) {
  return requestClient.put(`/users/${id}`, data);
}

/**
 * 删除用户
 * @param id 用户ID
 * @returns 删除结果
 */
export async function deleteUserApi(id: number) {
  return requestClient.delete(`/users/${id}`);
}

/**
 * 获取全部角色列表(用户模块使用)
 * @returns 角色列表
 */
export async function getRoleListAllApi() {
  return requestClient.get('/roles');
}
