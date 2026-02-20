import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  base: '/app/',
  build: {
    outDir: '../wwwroot/app',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5050',
      '/export': 'http://localhost:5050',
      '/backup': 'http://localhost:5050',
      '/print-label': 'http://localhost:5050',
      '/print-pallet-contents': 'http://localhost:5050',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setupTests.ts',
    globals: true,
    css: true,
    pool: 'forks',
    maxWorkers: 1,
    fileParallelism: false,
    testTimeout: 10000,
    hookTimeout: 10000,
    clearMocks: true,
    restoreMocks: true,
  },
});
