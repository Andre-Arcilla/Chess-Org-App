import { defineConfig } from 'vite';
import { resolve } from 'path';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        portal: resolve(__dirname, 'portal.html'),
        'dashboard-ADMIN': resolve(__dirname, 'dashboard-ADMIN.html'),
        'dashboard-MEMBER': resolve(__dirname, 'dashboard-MEMBER.html'),
        'dashboard-COACH': resolve(__dirname, 'dashboard-COACH.html')
      },
    },
  },
});
