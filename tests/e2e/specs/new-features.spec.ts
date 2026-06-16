import { test, expect, Page } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '../.auth/gestor.json') });

async function bootstrap(page: Page, target: string) {
  await page.goto('/');
  await page.waitForURL('**/dashboard', { timeout: 20_000 });
  await page.goto(target);
}

test.describe('Admin catalogs', () => {
  test('promoters page loads', async ({ page }) => {
    await bootstrap(page, '/admin/promoters');
    await expect(page.locator('h2').filter({ hasText: 'Promotores' })).toBeVisible({ timeout: 15_000 });
    await page.screenshot({ path: 'screenshots/admin-promoters.png' });
  });

  test('organic units page loads', async ({ page }) => {
    await bootstrap(page, '/admin/organic-units');
    await expect(page.locator('h2').filter({ hasText: 'Unidades Orgánicas' })).toBeVisible({ timeout: 15_000 });
    await page.screenshot({ path: 'screenshots/admin-organic-units.png' });
  });

  test('tags page loads', async ({ page }) => {
    await bootstrap(page, '/admin/tags');
    await expect(page.locator('h2').filter({ hasText: 'Etiquetas' })).toBeVisible({ timeout: 15_000 });
    await page.screenshot({ path: 'screenshots/admin-tags.png' });
  });

  test('create and delete a promoter', async ({ page }) => {
    await bootstrap(page, '/admin/promoters');
    await expect(page.locator('h2').filter({ hasText: 'Promotores' })).toBeVisible({ timeout: 15_000 });

    // Use unique name to avoid conflicts from failed previous runs
    const promoterName = `Promotor E2E ${Date.now()}`;

    // Open modal
    await page.locator('button').filter({ hasText: 'Nuevo promotor' }).click();
    await expect(page.locator('.ant-modal-content')).toBeVisible({ timeout: 10_000 });

    // Fill using click + pressSequentially to properly trigger Angular ControlValueAccessor
    const input = page.locator('.ant-modal-content input').first();
    await input.click();
    await input.pressSequentially(promoterName, { delay: 50 });

    // Wait for the Crear button to be enabled (form valid)
    const createBtn = page.locator('.ant-modal-footer button').filter({ hasText: 'Crear' });
    await expect(createBtn).not.toBeDisabled({ timeout: 5_000 });
    await createBtn.click();

    // Confirm success (not just any message)
    await expect(page.locator('.ant-message-success')).toBeVisible({ timeout: 10_000 });
    await page.screenshot({ path: 'screenshots/admin-promoters-created.png' });

    // Row should appear after list reloads
    const row = page.locator('.ant-table-row').filter({ hasText: promoterName });
    await expect(row).toBeVisible({ timeout: 15_000 });

    // Delete it
    await row.locator('button').filter({ hasText: 'Eliminar' }).click();
    await page.locator('.ant-popover-buttons button').filter({ hasText: 'Aceptar' }).click();
    await expect(page.locator('.ant-message-success')).toBeVisible({ timeout: 8_000 });
  });
});

test.describe('Portfolio new statuses', () => {
  test('portfolio shows 8 status stat-boxes', async ({ page }) => {
    await bootstrap(page, '/portfolio');
    await expect(page.locator('.stat-box').first()).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('.stat-box')).toHaveCount(8);
    await page.screenshot({ path: 'screenshots/portfolio.png' });
  });
});

test.describe('Project detail extended fields', () => {
  test('project detail info tab loads', async ({ page }) => {
    await bootstrap(page, '/projects');
    // Click the eye button (title="Ver detalle") to navigate to project detail
    await expect(page.locator('button[title="Ver detalle"]').first()).toBeVisible({ timeout: 15_000 });
    await page.locator('button[title="Ver detalle"]').first().click();
    await page.waitForURL('**/projects/**', { timeout: 15_000 });
    await expect(page.locator('h2, h1').first()).toBeVisible({ timeout: 10_000 });
    await page.screenshot({ path: 'screenshots/project-detail-info.png' });
  });

  test('project detail has notes tab', async ({ page }) => {
    await bootstrap(page, '/projects');
    await expect(page.locator('button[title="Ver detalle"]').first()).toBeVisible({ timeout: 15_000 });
    await page.locator('button[title="Ver detalle"]').first().click();
    await page.waitForURL('**/projects/**', { timeout: 15_000 });
    // Look for Notas tab
    const notesTab = page.locator('.ant-tabs-tab').filter({ hasText: 'Notas' });
    await expect(notesTab).toBeVisible({ timeout: 15_000 });
    await notesTab.click();
    await page.screenshot({ path: 'screenshots/project-detail-notes.png' });
  });
});

test.describe('Project form extended fields', () => {
  test('new project form has all three sections', async ({ page }) => {
    await bootstrap(page, '/projects');
    await expect(page.locator('h2').first()).toBeVisible({ timeout: 15_000 });
    await page.locator('button').filter({ hasText: 'Nuevo proyecto' }).click();
    await expect(page.locator('.ant-modal-content')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('.ant-divider').filter({ hasText: 'Datos básicos' })).toBeVisible();
    await expect(page.locator('.ant-divider').filter({ hasText: 'Clasificación y gobernanza' })).toBeVisible();
    await expect(page.locator('.ant-divider').filter({ hasText: 'Ciclo de vida' })).toBeVisible();
    await page.screenshot({ path: 'screenshots/project-form.png' });
  });
});
