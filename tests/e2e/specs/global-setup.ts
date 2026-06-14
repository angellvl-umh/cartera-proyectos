import { test as setup, expect } from '@playwright/test';
import path from 'path';

const authFile = path.join(__dirname, '../.auth/gestor.json');

setup('authenticate as gestor', async ({ page }) => {
  await page.goto('/');

  // Wait for Keycloak login form to appear
  await page.getByRole('textbox', { name: /username/i }).waitFor({ timeout: 15_000 });
  await page.getByRole('textbox', { name: /username/i }).fill('gestor');
  await page.getByRole('textbox', { name: /password/i }).fill('gestor123');
  await page.getByRole('button', { name: /sign in/i }).click();

  // Wait for redirect back to app
  await page.waitForURL('**/dashboard', { timeout: 15_000 });

  // Save auth state
  await page.context().storageState({ path: authFile });
});
