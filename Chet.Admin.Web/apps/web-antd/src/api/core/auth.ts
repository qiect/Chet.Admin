import { baseRequestClient, requestClient } from '#/api/request';

export namespace AuthApi {
  /** 登录接口参数 */
  export interface LoginParams {
    email?: string;
    password?: string;
    captchaId?: string;
    captchaCode?: string;
  }

  /** 登录接口返回值 */
  export interface LoginResult {
    accessToken: string;
    refreshToken: string;
    requireCaptcha?: boolean;
    lockedUntil?: string;
  }

  /** 验证码接口返回值 */
  export interface CaptchaResult {
    id: string;
    svg: string;
  }

  export interface RefreshTokenParams {
    accessToken: string;
    refreshToken: string;
  }

  export interface RefreshTokenResult {
    accessToken: string;
    refreshToken: string;
  }
}

/**
 * 登录
 */
export async function loginApi(data: AuthApi.LoginParams) {
  return requestClient.post<AuthApi.LoginResult>('/auth/login', data);
}

/**
 * 获取验证码
 */
export async function getCaptchaApi() {
  return requestClient.get<AuthApi.CaptchaResult>('/auth/captcha');
}

/**
 * 刷新accessToken
 */
export async function refreshTokenApi(data: AuthApi.RefreshTokenParams) {
  return baseRequestClient.post<AuthApi.RefreshTokenResult>(
    '/auth/refresh-token',
    data,
  );
}

/**
 * 退出登录
 */
export async function logoutApi() {
  return baseRequestClient.post('/auth/logout');
}

/**
 * 获取用户权限码
 */
export async function getAccessCodesApi() {
  return requestClient.get<string[]>('/auth/codes');
}

/**
 * 获取个人中心信息
 */
export async function getProfileApi() {
  return requestClient.get('/auth/profile');
}

/**
 * 更新个人中心信息
 */
export async function updateProfileApi(data: { name?: string; avatar?: string }) {
  return requestClient.put('/auth/profile', data);
}

/**
 * 修改密码
 */
export async function changePasswordApi(data: { oldPassword: string; newPassword: string }) {
  return requestClient.put('/auth/change-password', data);
}
