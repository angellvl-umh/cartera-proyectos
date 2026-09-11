import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'happy-dom',
    globals: true,
    // Solo tests unitarios de la app. Los `e2e/**` son specs de Playwright
    // (usan `test.use()`, API incompatible con Vitest) y se ejecutan aparte
    // con `pnpm e2e`.
    include: ['src/**/*.spec.ts'],
  },
});
