import { defineConfig } from '@vben/vite-config';

export default defineConfig(async () => {
  return {
    application: {},
    vite: {
      server: {
        proxy: {
          '/api': {
            changeOrigin: true,
            // 后端API地址
            target: 'http://localhost:5000',
            ws: true,
          },
          // 静态资源（上传的文件）代理
          '/uploads': {
            changeOrigin: true,
            target: 'http://localhost:5000',
          },
        },
      },
    },
  };
});
