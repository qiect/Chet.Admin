/**
 * 该文件可自行根据业务逻辑进行调整
 */
import type { RequestClientOptions } from '@vben/request';

import { useAppConfig } from '@vben/hooks';
import { preferences } from '@vben/preferences';
import {
  authenticateResponseInterceptor,
  defaultResponseInterceptor,
  errorMessageResponseInterceptor,
  RequestClient,
} from '@vben/request';
import { useAccessStore } from '@vben/stores';

import { message } from 'ant-design-vue';

import { useAuthStore } from '#/store';

import { refreshTokenApi } from './core';

const { apiURL } = useAppConfig(import.meta.env, import.meta.env.PROD);

function createRequestClient(baseURL: string, options?: RequestClientOptions) {
  const client = new RequestClient({
    ...options,
    baseURL,
  });

  /**
   * 重新认证逻辑
   */
  async function doReAuthenticate() {
    console.warn('Access token or refresh token is invalid or expired. ');
    const accessStore = useAccessStore();
    const authStore = useAuthStore();
    accessStore.setAccessToken(null);
    accessStore.setRefreshToken(null);
    if (
      preferences.app.loginExpiredMode === 'modal' &&
      accessStore.isAccessChecked
    ) {
      accessStore.setLoginExpired(true);
    } else {
      await authStore.logout();
    }
  }

  /**
   * 刷新token逻辑
   */
  async function doRefreshToken() {
    const accessStore = useAccessStore();
    // 没有 token 时直接抛出，避免发送无效请求触发 500 错误
    if (!accessStore.accessToken || !accessStore.refreshToken) {
      throw new Error('No token to refresh');
    }
    const resp = await refreshTokenApi({
      accessToken: accessStore.accessToken,
      refreshToken: accessStore.refreshToken,
    });
    const newAccessToken = resp.accessToken;
    const newRefreshToken = resp.refreshToken;
    accessStore.setAccessToken(newAccessToken);
    accessStore.setRefreshToken(newRefreshToken);
    return newAccessToken;
  }

  function formatToken(token: null | string) {
    return token ? `Bearer ${token}` : null;
  }

  // 请求头处理
  client.addRequestInterceptor({
    fulfilled: async (config) => {
      const accessStore = useAccessStore();

      config.headers.Authorization = formatToken(accessStore.accessToken);
      config.headers['Accept-Language'] = preferences.app.locale;
      return config;
    },
  });

  // 处理返回的响应数据格式
  // 后端返回格式: { success: true, data: ..., message: ..., statusCode: 200 }
  // 使用 success 字段判断接口是否成功
  client.addResponseInterceptor(
    defaultResponseInterceptor({
      codeField: 'success',
      dataField: 'data',
      successCode: true,
    }),
  );

  // token过期的处理
  client.addResponseInterceptor(
    authenticateResponseInterceptor({
      client,
      doReAuthenticate,
      doRefreshToken,
      enableRefreshToken: preferences.app.enableRefreshToken,
      formatToken,
    }),
  );

  // 通用的错误处理,如果没有进入上面的错误处理逻辑，就会进入这里
  client.addResponseInterceptor(
    errorMessageResponseInterceptor((msg: string, error) => {
      const responseData = error?.response?.data ?? {};
      const statusCode = responseData?.statusCode ?? error?.response?.status ?? 0;
      const errorMessage = responseData?.message ?? '';

      // 401错误：不显示错误提示，由authenticateResponseInterceptor处理跳转
      if (statusCode === 401) {
        return;
      }

      // 429错误：请求过于频繁（限流）
      if (statusCode === 429) {
        message.error(errorMessage || '请求过于频繁，请稍后再试');
        return;
      }

      // 403错误
      if (statusCode === 403) {
        message.error('没有操作权限');
        return;
      }

      // 404错误
      if (statusCode === 404) {
        message.error('请求的资源不存在');
        return;
      }

      // 500错误
      if (statusCode === 500) {
        message.error('服务器内部错误，请稍后重试');
        return;
      }

      // 其他错误：显示后端返回的消息，或默认提示
      message.error(errorMessage || msg || '请求失败');
    }),
  );

  return client;
}

export const requestClient = createRequestClient(apiURL, {
  responseReturn: 'data',
});

export const baseRequestClient = new RequestClient({ baseURL: apiURL });
