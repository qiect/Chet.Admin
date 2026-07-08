import { requestClient } from '#/api/request';

export async function getAllMenusApi() {
  return requestClient.get('/menus');
}

export async function getMenuTreeApi() {
  return requestClient.get('/menus/tree');
}

export async function getMenuListApi(params: any) {
  const result = await requestClient.get('/menus/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function createMenuApi(data: any) {
  return requestClient.post('/menus', data);
}

export async function updateMenuApi(id: number, data: any) {
  return requestClient.put(`/menus/${id}`, data);
}

export async function deleteMenuApi(id: number) {
  return requestClient.delete(`/menus/${id}`);
}
