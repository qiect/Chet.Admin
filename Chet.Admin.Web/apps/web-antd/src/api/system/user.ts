import { requestClient } from '#/api/request';

export async function getUserListApi(params: any) {
  const result = await requestClient.get('/users/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function getUserByIdApi(id: number) {
  return requestClient.get(`/users/${id}`);
}

export async function createUserApi(data: any) {
  return requestClient.post('/users', data);
}

export async function updateUserApi(id: number, data: any) {
  return requestClient.put(`/users/${id}`, data);
}

export async function deleteUserApi(id: number) {
  return requestClient.delete(`/users/${id}`);
}

export async function getRoleListAllApi() {
  return requestClient.get('/roles');
}
