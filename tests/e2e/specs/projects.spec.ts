import { test, expect } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '../.auth/gestor.json') });

test.describe('Projects', () => {
  test('project list has items from seed', async ({ page }) => {
    await page.goto('/');
    await page.waitForURL('**/dashboard', { timeout: 15_000 });
    await page.locator('nz-sider, .ant-layout-sider').getByText('Proyectos', { exact: true }).click();
    await page.waitForURL('**/projects');
    const rows = page.locator('.ant-table-row');
    // Wait until at least 6 real data rows are rendered (polling)
    await expect(rows.nth(5)).toBeVisible({ timeout: 20_000 });
    expect(await rows.count()).toBeGreaterThan(5);
  });

  test('project detail page loads', async ({ page }) => {
    await page.goto('/');
    await page.waitForURL('**/dashboard', { timeout: 15_000 });
    await page.locator('nz-sider, .ant-layout-sider').getByText('Proyectos', { exact: true }).click();
    await page.waitForURL('**/projects');
    await page.locator('.ant-table-row').first().click();
    await expect(page.locator('h2, h1').first()).toBeVisible({ timeout: 10_000 });
  });
});
