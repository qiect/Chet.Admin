import { requestClient } from '#/api/request';

export async function getRoleListApi(params: any) {
  const result = await requestClient.get('/roles/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function getRoleAllApi() {
  return requestClient.get('/roles');
}

export async function createRoleApi(data: any) {
  return requestClient.post('/roles', data);
}

export async function updateRoleApi(id: number, data: any) {
  return requestClient.put(`/roles/${id}`, data);
}

export async function deleteRoleApi(id: number) {
  return requestClient.delete(`/roles/${id}`);
}

export async function getRolePermissionsApi(id: number) {
  return requestClient.get(`/roles/${id}/permissions`);
}

export async function assignRolePermissionsApi(id: number, permissionIds: number[]) {
  return requestClient.post(`/roles/${id}/permissions`, permissionIds);
}

export async function getRoleMenusApi(id: number) {
  return requestClient.get(`/roles/${id}/menus`);
}

export async function assignRoleMenusApi(id: number, menuIds: number[]) {
  return requestClient.post(`/roles/${id}/menus`, menuIds);
}

export async function getPermissionAllApi() {
  return requestClient.get('/permissions');
}

export async function getMenuTreeApi() {
  return requestClient.get('/menus/tree');
}

export async function updateDataScopeApi(id: number, data: { dataScope: string; customDeptIds?: number[] }) {
  return requestClient.put(`/api/v1/roles/${id}/data-scope`, data);
}
