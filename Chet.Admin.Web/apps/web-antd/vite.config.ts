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
        },
      },
    },
  };
});
