import { requestClient } from '#/api/request';

export async function getFileListApi(params?: any) {
  return requestClient.get('/files', { params });
}

export async function uploadFileApi(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  return requestClient.post('/files/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
}

export async function getFileApi(id: number) {
  return requestClient.get(`/files/${id}`);
}

export function getFileDownloadUrl(id: number) {
  return `/files/${id}/download`;
}

export async function deleteFileApi(id: number) {
  return requestClient.delete(`/files/${id}`);
}
