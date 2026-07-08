import { requestClient } from '#/api/request';

/**
 * 获取全部部门列表
 * @returns 部门列表
 */
export async function getAllDeptsApi() {
  return requestClient.get('/departments');
}

/**
 * 获取部门树形结构
 * @returns 部门树形数据
 */
export async function getDeptTreeApi() {
  return requestClient.get('/departments/tree');
}

/**
 * 分页查询部门列表
 * @param params 查询参数
 * @returns 部门分页列表,包含数据项和总记录数
 */
export async function getDeptListApi(params: any) {
  const result = await requestClient.get('/departments/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 创建部门
 * @param data 部门信息
 * @returns 创建结果
 */
export async function createDeptApi(data: any) {
  return requestClient.post('/departments', data);
}

/**
 * 更新部门信息
 * @param id 部门ID
 * @param data 部门信息
 * @returns 更新结果
 */
export async function updateDeptApi(id: number, data: any) {
  return requestClient.put(`/departments/${id}`, data);
}

/**
 * 删除部门
 * @param id 部门ID
 * @returns 删除结果
 */
export async function deleteDeptApi(id: number) {
  return requestClient.delete(`/departments/${id}`);
}
