import { requestClient } from '#/api/request';

export async function getAllDeptsApi() {
  return requestClient.get('/departments');
}

export async function getDeptTreeApi() {
  return requestClient.get('/departments/tree');
}

export async function getDeptListApi(params: any) {
  const result = await requestClient.get('/departments/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function createDeptApi(data: any) {
  return requestClient.post('/departments', data);
}

export async function updateDeptApi(id: number, data: any) {
  return requestClient.put(`/departments/${id}`, data);
}

export async function deleteDeptApi(id: number) {
  return requestClient.delete(`/departments/${id}`);
}
