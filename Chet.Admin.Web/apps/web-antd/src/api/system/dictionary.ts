import { requestClient } from '#/api/request';

/**
 * 分页查询字典列表
 * @param params 查询参数
 * @returns 字典分页列表,包含数据项和总记录数
 */
export async function getDictListApi(params: any) {
  const result = await requestClient.get('/dictionaries/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 根据字典类型获取字典信息
 * @param dictType 字典类型
 * @returns 字典信息
 */
export async function getDictByTypeApi(dictType: string) {
  return requestClient.get(`/dictionaries/type/${dictType}`);
}

/**
 * 创建字典
 * @param data 字典信息
 * @returns 创建结果
 */
export async function createDictApi(data: any) {
  return requestClient.post('/dictionaries', data);
}

/**
 * 更新字典信息
 * @param id 字典ID
 * @param data 字典信息
 * @returns 更新结果
 */
export async function updateDictApi(id: number, data: any) {
  return requestClient.put(`/dictionaries/${id}`, data);
}

/**
 * 删除字典
 * @param id 字典ID
 * @returns 删除结果
 */
export async function deleteDictApi(id: number) {
  return requestClient.delete(`/dictionaries/${id}`);
}

/**
 * 根据字典编码获取字典项列表
 * @param code 字典编码
 * @returns 字典项列表
 */
export async function getDictItemsByCodeApi(code: string) {
  return requestClient.get(`/dictionaries/code/${code}`);
}
