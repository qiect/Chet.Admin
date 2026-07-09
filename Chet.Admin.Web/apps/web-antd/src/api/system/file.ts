import { requestClient } from '#/api/request';

/**
 * 获取文件列表
 * @param params 查询参数
 * @returns 文件列表
 */
export async function getFileListApi(params?: any) {
  return requestClient.get('/files', { params });
}

/**
 * 上传文件
 * @param file 待上传的文件对象
 * @returns 上传结果,包含文件访问地址等信息
 */
export async function uploadFileApi(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  return requestClient.post('/files/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
}

/**
 * 根据文件ID获取文件信息
 * @param id 文件ID
 * @returns 文件信息
 */
export async function getFileApi(id: number) {
  return requestClient.get(`/files/${id}`);
}

/**
 * 下载文件（携带 Token，返回 Blob）
 * 后端返回的是原始文件流（非 ApiResponse JSON），需要用 responseReturn: 'body' 跳过 success 校验
 * @param id 文件ID
 */
export async function downloadFileApi(id: number) {
  return requestClient.get<Blob>(`/files/${id}/download`, {
    responseType: 'blob',
    responseReturn: 'body',
  });
}

/**
 * 删除文件
 * @param id 文件ID
 * @returns 删除结果
 */
export async function deleteFileApi(id: number) {
  return requestClient.delete(`/files/${id}`);
}
