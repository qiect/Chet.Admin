import { requestClient } from '#/api/request';

export async function getNotificationListApi(params: any) {
  const result = await requestClient.get('/notifications/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function getMyNotificationsApi(params?: any) {
  const result = await requestClient.get('/notifications/my', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function getUnreadCountApi() {
  return requestClient.get('/notifications/unread-count');
}

export async function createNotificationApi(data: any) {
  return requestClient.post('/notifications', data);
}

export async function markAsReadApi(id: number) {
  return requestClient.put(`/notifications/${id}/read`);
}

export async function markAllAsReadApi() {
  return requestClient.put('/notifications/read-all');
}

export async function deleteNotificationApi(id: number) {
  return requestClient.delete(`/notifications/${id}`);
}
