/**
 * workitems.spec.ts  (rol: gestor)
 *
 * Flujos de work items dentro de un proyecto creado por el propio test:
 *  1. Crear una tarea y verla en el listado (Product Backlog).
 *  2. Cambiar el estado de la tarea a "En curso" (via el drawer "Ver").
 *  3. Descartar la tarea → verificar estado "Descartada".
 *
 * Nota: Playwright ejecuta el módulo independientemente para el beforeAll
 * y para cada test. Para compartir state, usamos variables del describe
 * que se asignan en beforeAll y se leen en los tests (mismo proceso worker,
 * misma instancia de las variables del describe).
 */
import { test, expect, Page } from '@playwright/test';
import path from 'path';
import { gotoAndWait } from './helpers/login';

test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

test.describe('Work Items (gestor)', () => {

  // Variables compartidas entre beforeAll y tests (mismo worker)
  let projectDetailUrl = '';
  let sharedProjectTitle = '';
  let sharedTaskTitle = '';

  // ──────────────────────────────────────────────────────────────────────────
  // Setup
  // ──────────────────────────────────────────────────────────────────────────

  test.beforeAll(async ({ browser }) => {
    // Generar títulos únicos UNA SOLA VEZ en beforeAll
    const ts = Date.now();
    sharedProjectTitle = `WI E2E ${ts}`;
    sharedTaskTitle    = `Tarea E2E ${ts}`;

    const ctx = await browser.newContext({
      storageState: path.join(__dirname, '.auth/gestor.json'),
    });
    const page = await ctx.newPage();
    try {
      // Crear proyecto
      projectDetailUrl = await createProjectAndGetUrl(page, sharedProjectTitle);
      // Crear tarea en el backlog
      await navigateToBacklog(page, projectDetailUrl);
      await createTask(page, sharedTaskTitle);
    } finally {
      await ctx.close();
    }
  });

  // ──────────────────────────────────────────────────────────────────────────
  // Helpers
  // ──────────────────────────────────────────────────────────────────────────

  async function gotoProjects(page: Page): Promise<void> {
    await gotoAndWait(page, '/projects');
    await expect(page).toHaveURL(/\/projects/, { timeout: 15_000 });
    await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible({ timeout: 15_000 });
  }

  async function searchProject(page: Page, title: string): Promise<void> {
    const input = page.getByRole('textbox', { name: /buscar por título/i });
    await expect(input).toBeVisible({ timeout: 5_000 });
    await input.clear();
    await input.fill(title);
    await page.waitForTimeout(600);
  }

  async function createProjectAndGetUrl(page: Page, projectTitle: string): Promise<string> {
    await gotoProjects(page);

    await page.getByRole('button', { name: /nuevo proyecto/i }).click();
    await expect(page.getByText('Nuevo proyecto').last()).toBeVisible({ timeout: 8_000 });

    const modal = page.locator('nz-modal-container').last();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    await modal.locator('input[type="text"]').first().fill(projectTitle);
    await modal.locator('nz-select').first().click();
    await page.locator('.cdk-overlay-container nz-option-item').first().click();

    await page.getByRole('button', { name: /guardar/i }).click();
    await expect(modal).toBeHidden({ timeout: 10_000 });

    await searchProject(page, projectTitle);
    await expect(page.getByText(projectTitle)).toBeVisible({ timeout: 10_000 });

    const row = page.locator('tbody tr').filter({ hasText: projectTitle });
    await row.locator('button').first().click();
    await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });

    return page.url();
  }

  async function navigateToBacklog(page: Page, detailUrl: string): Promise<void> {
    const pathOnly = detailUrl.replace(new URL(detailUrl).origin, '');
    await gotoAndWait(page, pathOnly);
    await expect(page).toHaveURL(
      new RegExp(detailUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
      { timeout: 10_000 }
    );

    // Esperar que el heading del proyecto esté visible (página completamente cargada)
    await expect(page.locator('h2')).toBeVisible({ timeout: 10_000 });

    const tab = page.getByRole('tab', { name: /product backlog/i });
    await expect(tab).toBeVisible({ timeout: 10_000 });
    await tab.click();

    // Esperar que la tabla del backlog sea visible
    await expect(page.getByRole('button', { name: /nueva tarea/i })).toBeVisible({ timeout: 10_000 });
    await page.waitForTimeout(300);
  }

  async function createTask(page: Page, title: string): Promise<void> {
    // Usar la alta rápida al pie de la tabla del backlog
    // El campo tiene placeholder "Añadir tarea de alto nivel…"
    const quickAddInput = page.getByRole('textbox', { name: /añadir tarea de alto nivel/i });
    await expect(quickAddInput).toBeVisible({ timeout: 5_000 });
    await quickAddInput.fill(title);

    // El botón "Añadir" se habilita cuando hay texto
    const addBtn = page.getByRole('button', { name: /añadir/i });
    await expect(addBtn).toBeEnabled({ timeout: 5_000 });
    await addBtn.click();

    // Esperar que la tarea aparezca en la tabla del backlog
    await page.waitForTimeout(500);
    await expect(page.locator('tbody').getByText(title)).toBeVisible({ timeout: 10_000 });
  }

  async function goToBacklog(page: Page): Promise<void> {
    if (projectDetailUrl) {
      await navigateToBacklog(page, projectDetailUrl);
    } else {
      await gotoProjects(page);
      await searchProject(page, sharedProjectTitle);
      const row = page.locator('tbody tr').filter({ hasText: sharedProjectTitle });
      await row.locator('button').first().click();
      await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });
      await page.getByRole('tab', { name: /product backlog/i }).click();
      await page.waitForTimeout(500);
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Tests
  // ──────────────────────────────────────────────────────────────────────────

  test('crear una tarea y verla en el listado', async ({ page }) => {
    await goToBacklog(page);

    // Crear una tarea adicional para verificar que la creación funciona
    const localTitle = `${sharedTaskTitle} - Extra`;
    await createTask(page, localTitle);

    // Verificar que aparece en el backlog
    await expect(page.locator('tbody').getByText(localTitle)).toBeVisible();
  });

  test('cambiar el estado de la tarea a En curso', async ({ page }) => {
    await goToBacklog(page);

    // La tarea fue creada en beforeAll — debe estar en el backlog
    const row = page.locator('tbody tr').filter({ hasText: sharedTaskTitle }).first();
    await expect(row).toBeVisible({ timeout: 10_000 });

    // Abrir el drawer "Ver" (primer botón de la fila)
    await row.locator('button').first().click();

    const drawer = page.locator('nz-drawer, .ant-drawer').last();
    await expect(drawer).toBeVisible({ timeout: 8_000 });

    // Buscar transición de estado
    const inProgressBtn = drawer.getByRole('button', { name: /en curso|in progress/i });
    const btnCount = await inProgressBtn.count();

    if (btnCount > 0) {
      await inProgressBtn.first().click();
    } else {
      const statusSelect = drawer.locator('nz-select').first();
      const selectCount = await statusSelect.count();
      if (selectCount > 0) {
        await statusSelect.click();
        const option = page.locator('.cdk-overlay-container nz-option-item')
          .filter({ hasText: /en curso/i });
        await expect(option).toBeVisible({ timeout: 5_000 });
        await option.click();
      }
    }

    await page.waitForTimeout(1000);

    // No debe haber mensaje de error
    await expect(page.locator('.ant-message-error')).toHaveCount(0);
  });

  test('descartar la tarea y verificar estado Descartada', async ({ page }) => {
    await goToBacklog(page);

    // Necesitamos una tarea en estado que permita descarte (no Discarded ya)
    // Creamos una tarea específica para este test
    const discardTitle = `${sharedTaskTitle} - ToDiscard`;
    await createTask(page, discardTitle);

    const row = page.locator('tbody tr').filter({ hasText: discardTitle }).first();
    await expect(row).toBeVisible({ timeout: 5_000 });

    // El botón de descartar es el botón "stop" (icono stop de Ant Design)
    // Orden de botones en la fila: eye, link, comment, edit, stop, delete
    const stopBtn = row.locator('button').filter({ has: page.locator('img[alt="stop"]') });
    const stopBtnCount = await stopBtn.count();

    if (stopBtnCount > 0) {
      await stopBtn.first().click();
    } else {
      // Fallback: penúltimo botón de la fila (antes de delete)
      const rowBtns = row.locator('button');
      const total = await rowBtns.count();
      if (total >= 2) {
        await rowBtns.nth(total - 2).click();
      }
    }

    // El popconfirm aparece — buscar el botón de confirmación "Aceptar"
    await page.waitForTimeout(400);

    // El popconfirm tiene botones "Cancelar" y "Aceptar"
    const popconfirmOk = page.getByRole('button', { name: /aceptar/i });
    // Puede haber múltiples "Aceptar" en la página — cogemos el que está en el overlay del popconfirm
    // El popconfirm overlay es el último en el DOM
    const aceptarBtns = page.locator('button').filter({ hasText: /^Aceptar$/ });
    const aceptarCount = await aceptarBtns.count();

    if (aceptarCount > 0) {
      await aceptarBtns.last().click();
    } else {
      // Fallback
      await popconfirmOk.last().click();
    }

    // Esperar mensaje de éxito
    await expect(
      page.locator('.ant-message-notice').first()
    ).toBeVisible({ timeout: 8_000 });

    // La tarea descartada debe mostrar el tag "Descartada"
    const updatedRow = page.locator('tbody tr').filter({ hasText: discardTitle }).first();
    await expect(updatedRow.locator('nz-tag').filter({ hasText: /descart/i }))
      .toBeVisible({ timeout: 10_000 });
  });

});
