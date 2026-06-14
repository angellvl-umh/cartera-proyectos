import { test, expect } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '../.auth/gestor.json') });

test.describe('Teams CRUD', () => {
  const teamName = `E2E Team ${Date.now()}`;

  test('create a new team', async ({ page }) => {
    await page.goto('/');
    await page.waitForURL('**/dashboard', { timeout: 15_000 });
    await page.locator('nz-sider, .ant-layout-sider').getByText('Equipos', { exact: true }).click();
    await page.waitForURL('**/teams');

    await page.getByRole('button', { name: /nuevo equipo/i }).click();

    // Form uses nz-form-label + formControlName, use placeholder
    await page.getByPlaceholder('Nombre del equipo').fill(teamName);
    await page.locator('textarea[formcontrolname="description"]').fill('Team created by E2E test');
    await page.getByRole('button', { name: /crear equipo/i }).click();

    await expect(page.getByText(teamName)).toBeVisible({ timeout: 10_000 });
  });
});
