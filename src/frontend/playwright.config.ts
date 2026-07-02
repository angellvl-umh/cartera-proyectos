import { defineConfig, devices } from '@playwright/test';
import path from 'path';

/**
 * Playwright E2E configuration.
 * Stack: Angular 21 + Keycloak 26 OIDC.
 *
 * Run against the full Docker stack:
 *   pnpm e2e
 *
 * Set E2E_BASE_URL to target a different environment:
 *   E2E_BASE_URL=http://localhost:4200 pnpm e2e
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: { timeout: 10_000 },

  /* Run tests in each file sequentially (safer for E2E with shared state) */
  fullyParallel: false,
  workers: 1,

  /* Fail fast in CI */
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,

  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],

  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },

  projects: [
    /* ── Auth setup (runs once before everything else) ── */
    {
      name: 'auth-setup',
      testMatch: /auth\.setup\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },

    /* ── Tests that use stored auth state ── */
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // storageState is set per-test via test.use() or fixture
      },
      dependencies: ['auth-setup'],
    },
  ],

  /* Playwright output (screenshots, videos, traces) */
  outputDir: 'playwright-results/',
});
