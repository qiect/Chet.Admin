import { requestClient } from '#/api/request';

/**
 * 分页查询通知列表
 * @param params 查询参数
 * @returns 通知分页列表,包含数据项和总记录数
 */
export async function getNotificationListApi(params: any) {
  const result = await requestClient.get('/notifications/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 获取当前用户的通知列表
 * @param params 查询参数
 * @returns 当前用户通知分页列表,包含数据项和总记录数
 */
export async function getMyNotificationsApi(params?: any) {
  const result = await requestClient.get('/notifications/my', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 获取当前用户的未读通知数量
 * @returns 未读通知数量
 */
export async function getUnreadCountApi() {
  return requestClient.get('/notifications/unread-count');
}

/**
 * 创建通知
 * @param data 通知信息
 * @returns 创建结果
 */
export async function createNotificationApi(data: any) {
  return requestClient.post('/notifications', data);
}

/**
 * 将指定通知标记为已读
 * @param id 通知ID
 * @returns 标记结果
 */
export async function markAsReadApi(id: number) {
  return requestClient.put(`/notifications/${id}/read`);
}

/**
 * 将当前用户的所有通知标记为已读
 * @returns 标记结果
 */
export async function markAllAsReadApi() {
  return requestClient.put('/notifications/read-all');
}

/**
 * 删除通知
 * @param id 通知ID
 * @returns 删除结果
 */
export async function deleteNotificationApi(id: number) {
  return requestClient.delete(`/notifications/${id}`);
}
