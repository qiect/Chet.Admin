import { requestClient } from '#/api/request';

/**
 * 获取在线用户列表
 * @returns 在线用户列表
 */
export async function getOnlineUsersApi() {
  return requestClient.get('/onlineusers');
}

/**
 * 强制指定用户下线
 * @param userId 用户ID
 * @returns 强制下线操作结果
 */
export async function forceOfflineApi(userId: number) {
  return requestClient.delete(`/onlineusers/${userId}`);
}
