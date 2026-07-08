import { requestClient } from '#/api/request';

export async function getPermissionListApi(params: any) {
  const result = await requestClient.get('/permissions/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function getPermissionAllApi() {
  return requestClient.get('/permissions');
}

export async function createPermissionApi(data: any) {
  return requestClient.post('/permissions', data);
}

export async function updatePermissionApi(id: number, data: any) {
  return requestClient.put(`/permissions/${id}`, data);
}

export async function deletePermissionApi(id: number) {
  return requestClient.delete(`/permissions/${id}`);
}
