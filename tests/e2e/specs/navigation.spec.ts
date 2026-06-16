import { test, expect, Page } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '../.auth/gestor.json') });

async function waitForApp(page: Page) {
  await page.waitForURL('**/dashboard', { timeout: 15_000 });
}

/** Click sidebar menu item by exact text */
async function navTo(page: Page, label: string) {
  await page.locator('nz-sider, .ant-layout-sider').getByText(label, { exact: true }).click();
}

test.describe('Navigation', () => {
  test('dashboard loads with stats', async ({ page }) => {
    await page.goto('/');
    await waitForApp(page);
    // Wait for stat cards to appear (data loaded from API, spinner gone)
    await expect(page.locator('.stat-card').first()).toBeVisible({ timeout: 30_000 });
  });

  test('teams list loads via sidebar', async ({ page }) => {
    await page.goto('/');
    await waitForApp(page);
    await navTo(page, 'Equipos');
    await page.waitForURL('**/teams');
    await expect(page.locator('.ant-table-row').first()).toBeVisible({ timeout: 10_000 });
  });

  test('projects list loads via sidebar', async ({ page }) => {
    await page.goto('/');
    await waitForApp(page);
    await navTo(page, 'Proyectos');
    await page.waitForURL('**/projects');
    await expect(page.locator('.ant-table-row').first()).toBeVisible({ timeout: 10_000 });
  });

  test('portfolio loads via sidebar', async ({ page }) => {
    await page.goto('/');
    await waitForApp(page);
    await navTo(page, 'Cartera');
    await page.waitForURL('**/portfolio');
    await expect(page.locator('h2').first()).toBeVisible({ timeout: 10_000 });
  });

  test('capacity loads via sidebar', async ({ page }) => {
    await page.goto('/');
    await waitForApp(page);
    await navTo(page, 'Capacidad');
    await page.waitForURL('**/capacity');
    await expect(page.locator('h2').first()).toBeVisible({ timeout: 10_000 });
  });

  test('my-tasks loads via sidebar', async ({ page }) => {
    await page.goto('/');
    await waitForApp(page);
    await navTo(page, 'Mis tareas');
    await page.waitForURL('**/my-tasks');
    await expect(page.locator('h2').first()).toBeVisible({ timeout: 10_000 });
  });
});
