import { test as setup, expect, request } from '@playwright/test';
import path from 'path';

const authFile = path.join(__dirname, '../.auth/gestor.json');

setup('authenticate as gestor', async ({ page }) => {
  await page.goto('/');

  // Wait for Keycloak login form to appear
  await page.getByRole('textbox', { name: /username/i }).waitFor({ timeout: 15_000 });
  await page.getByRole('textbox', { name: /username/i }).fill('gestor');
  await page.getByRole('textbox', { name: /password/i }).fill('gestor123');
  await page.getByRole('button', { name: /sign in/i }).click();

  // Wait for redirect back to app (triggers /api/me auto-provisioning)
  await page.waitForURL('**/dashboard', { timeout: 15_000 });

  // Ensure the user is provisioned and promoted to Gestor role
  const apiContext = await request.newContext({ baseURL: 'http://localhost' });

  // Copy cookies/storage from browser to API context
  const cookies = await page.context().cookies();
  await apiContext.storageState(); // warm up

  // Use the page's fetch to call the API (inherits auth cookies)
  const meResponse = await page.evaluate(async () => {
    const res = await fetch('/api/me');
    return res.ok ? res.json() : null;
  });

  if (meResponse && meResponse.Role !== 'Gestor') {
    await page.evaluate(async (personId: number) => {
      await fetch(`/api/persons/${personId}/role`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role: 'Gestor' }),
      });
    }, meResponse.Id as number);

    // Reload to pick up new role in the app
    await page.reload();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });
  }

  // Save auth state
  await page.context().storageState({ path: authFile });
});
