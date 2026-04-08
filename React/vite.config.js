import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiTarget = env.VITE_PROXY_API_TARGET || 'https://localhost:44363';

  return {
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@pages': path.resolve(__dirname, './src/pages'),
      '@styles': path.resolve(__dirname, './src/styles'),
      '@services': path.resolve(__dirname, './src/services'),
      '@hooks': path.resolve(__dirname, './src/hooks'),
      '@routes': path.resolve(__dirname, './src/routes'),
      '@helpers': path.resolve(__dirname, './src/helpers'),
    },
  },
  optimizeDeps: {
    include: ['antd', '@ant-design/icons'],
    esbuildOptions: {
      target: 'es2020',
      jsx: 'automatic',
    },
    exclude: ['antd/es/style'],
  },
  esbuild: {
    target: 'es2020',
    logOverride: { 'this-is-undefined-in-esm': 'silent' },
    legalComments: 'none',
  },
  server: {
    port: 3000,
    open: true,
    // Encaminha /api para a API (IIS Express em HTTPS). secure:false aceita certificado de desenvolvimento.
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    commonjsOptions: {
      include: [/antd/, /node_modules/],
      transformMixedEsModules: true,
    },
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'antd-vendor': ['antd', '@ant-design/icons'],
        },
      },
    },
  },
};
});
