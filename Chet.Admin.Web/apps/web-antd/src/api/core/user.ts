import type { UserInfo } from '@vben/types';

import { requestClient } from '#/api/request';

export interface BackendUserInfo {
  id: number;
  name: string;
  email: string;
  avatar?: string | null;
  departmentId?: number | null;
  roles: string[];
  permissions: string[];
}

/**
 * 获取用户信息
 */
export async function getUserInfoApi() {
  const backendUserInfo =
    await requestClient.get<BackendUserInfo>('/auth/user-info');
  // 将后端用户信息转换为前端UserInfo格式
  const userInfo: UserInfo = {
    userId: String(backendUserInfo.id),
    username: backendUserInfo.email,
    realName: backendUserInfo.name,
    avatar: backendUserInfo.avatar || '',
    roles: backendUserInfo.roles,
  };
  return {
    userInfo,
    permissions: backendUserInfo.permissions,
  };
}
