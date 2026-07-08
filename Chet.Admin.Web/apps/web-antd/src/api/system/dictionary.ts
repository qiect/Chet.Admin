import { requestClient } from '#/api/request';

export async function getDictListApi(params: any) {
  const result = await requestClient.get('/dictionaries/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function getDictByTypeApi(dictType: string) {
  return requestClient.get(`/dictionaries/type/${dictType}`);
}

export async function createDictApi(data: any) {
  return requestClient.post('/dictionaries', data);
}

export async function updateDictApi(id: number, data: any) {
  return requestClient.put(`/dictionaries/${id}`, data);
}

export async function deleteDictApi(id: number) {
  return requestClient.delete(`/dictionaries/${id}`);
}

export async function getDictItemsByCodeApi(code: string) {
  return requestClient.get(`/dictionaries/code/${code}`);
}
