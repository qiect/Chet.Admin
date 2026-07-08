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
 * 获取文件下载地址
 * @param id 文件ID
 * @returns 文件下载URL
 */
export function getFileDownloadUrl(id: number) {
  return `/files/${id}/download`;
}

/**
 * 删除文件
 * @param id 文件ID
 * @returns 删除结果
 */
export async function deleteFileApi(id: number) {
  return requestClient.delete(`/files/${id}`);
}
