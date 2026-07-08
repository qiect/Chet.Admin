import { requestClient } from '#/api/request';

export async function getOnlineUsersApi() {
  return requestClient.get('/onlineusers');
}

export async function forceOfflineApi(userId: number) {
  return requestClient.delete(`/onlineusers/${userId}`);
}
