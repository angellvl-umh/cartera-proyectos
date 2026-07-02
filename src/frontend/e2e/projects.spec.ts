/**
 * projects.spec.ts  (rol: gestor)
 *
 * Flujos de proyectos:
 *  1. La lista de proyectos carga con filas.
 *  2. Crear un proyecto y verlo en el listado.
 *  3. Abrir el detalle del proyecto recién creado.
 *  4. Cambiar estado Stopped → "Planificando con cliente" y verificar el badge.
 */
import { test, expect, Page } from '@playwright/test';
import path from 'path';
import { gotoAndWait } from './helpers/login';

test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

// Timestamp único para no colisionar en ejecuciones repetidas
const TS = Date.now();
const PROJECT_TITLE = `Proyecto E2E ${TS}`;

// Guarda la URL del proyecto creado en beforeAll para usarla en tests 3 y 4
let projectUrl: string | null = null;

/**
 * Helper: navega a /projects y espera que cargue.
 */
async function gotoProjects(page: Page): Promise<void> {
  await gotoAndWait(page, '/projects');
  await expect(page).toHaveURL(/\/projects/, { timeout: 15_000 });
  await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible({ timeout: 15_000 });
}

/**
 * Helper: busca el proyecto en el campo de búsqueda.
 */
async function searchProject(page: Page, title: string): Promise<void> {
  const searchInput = page.getByRole('textbox', { name: /buscar por título/i });
  await expect(searchInput).toBeVisible({ timeout: 5_000 });
  await searchInput.clear();
  await searchInput.fill(title);
  await page.waitForTimeout(600); // debounce
}

/**
 * Helper: click en el botón Ver/ojo de la fila que coincide con el título.
 * Los botones en la columna Acciones son iconos image-button.
 */
async function clickViewButton(page: Page, title: string): Promise<void> {
  const row = page.locator('tbody tr').filter({ hasText: title });
  // El primer botón de la fila (columna Acciones) es "Ver detalle" (eye icon)
  await row.locator('button').first().click();
}

/**
 * Helper: crea un proyecto vía UI y retorna su URL.
 */
async function createProjectViaUI(page: Page): Promise<string> {
  await gotoProjects(page);

  await page.getByRole('button', { name: /nuevo proyecto/i }).click();
  await expect(page.getByText('Nuevo proyecto').last()).toBeVisible({ timeout: 8_000 });

  const modal = page.locator('nz-modal-container').last();
  await expect(modal).toBeVisible({ timeout: 5_000 });

  // Rellenar título — primer input de texto del modal
  const titleInput = modal.locator('input[type="text"]').first();
  await expect(titleInput).toBeVisible({ timeout: 5_000 });
  await titleInput.fill(PROJECT_TITLE);

  // Complejidad — primer nz-select del modal
  await modal.locator('nz-select').first().click();
  await page.locator('.cdk-overlay-container nz-option-item').first().click();

  await page.getByRole('button', { name: /guardar/i }).click();
  await expect(modal).toBeHidden({ timeout: 10_000 });

  // Buscar y navegar al proyecto
  await searchProject(page, PROJECT_TITLE);
  await expect(page.getByText(PROJECT_TITLE)).toBeVisible({ timeout: 10_000 });
  await clickViewButton(page, PROJECT_TITLE);

  await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });
  return page.url();
}

test.describe('Proyectos (gestor)', () => {

  // Crear el proyecto una vez para los tests que lo necesitan
  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext({
      storageState: path.join(__dirname, '.auth/gestor.json'),
    });
    const page = await context.newPage();
    try {
      projectUrl = await createProjectViaUI(page);
    } finally {
      await context.close();
    }
  });

  test('la lista de proyectos carga con filas de tabla', async ({ page }) => {
    await gotoProjects(page);

    // La tabla debe estar visible con filas de proyectos
    const table = page.locator('nz-table').first();
    await expect(table).toBeVisible();
    await expect(table.locator('thead tr')).toBeVisible();
    await expect(table.locator('tbody tr').first()).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible();
  });

  test('crear un proyecto y verlo en el listado', async ({ page }) => {
    // Este test crea su propio proyecto con un título único extra
    const localTs = Date.now();
    const localTitle = `Proyecto E2E Create ${localTs}`;

    await gotoProjects(page);

    await page.getByRole('button', { name: /nuevo proyecto/i }).click();
    await expect(page.getByText('Nuevo proyecto').last()).toBeVisible({ timeout: 8_000 });

    const modal = page.locator('nz-modal-container').last();
    await expect(modal).toBeVisible({ timeout: 5_000 });

    const titleInput = modal.locator('input[type="text"]').first();
    await expect(titleInput).toBeVisible({ timeout: 5_000 });
    await titleInput.fill(localTitle);

    await modal.locator('nz-select').first().click();
    await page.locator('.cdk-overlay-container nz-option-item').first().click();

    await page.getByRole('button', { name: /guardar/i }).click();
    await expect(modal).toBeHidden({ timeout: 10_000 });

    // Buscar el proyecto en el listado
    await searchProject(page, localTitle);
    await expect(page.getByText(localTitle)).toBeVisible({ timeout: 10_000 });
  });

  test('abrir el detalle del proyecto recién creado', async ({ page }) => {
    // Navegar directamente a la URL del proyecto creado en beforeAll
    if (projectUrl) {
      await gotoAndWait(page, projectUrl.replace(new URL(projectUrl).origin, ''));
      await expect(page).toHaveURL(new RegExp(projectUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')), { timeout: 10_000 });
    } else {
      // Fallback: buscar por título
      await gotoProjects(page);
      await searchProject(page, PROJECT_TITLE);
      await expect(page.getByText(PROJECT_TITLE)).toBeVisible({ timeout: 10_000 });
      await clickViewButton(page, PROJECT_TITLE);
      await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });
    }

    // El título del proyecto aparece en el encabezado
    await expect(page.getByRole('heading', { name: PROJECT_TITLE })).toBeVisible();
  });

  test('cambiar estado Stopped → Planificando con cliente y verificar badge', async ({ page }) => {
    // Navegar al proyecto creado en beforeAll
    if (projectUrl) {
      const pathOnly = projectUrl.replace(new URL(projectUrl).origin, '');
      await gotoAndWait(page, pathOnly);
      await expect(page).toHaveURL(new RegExp(projectUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')), { timeout: 10_000 });
    } else {
      await gotoProjects(page);
      await searchProject(page, PROJECT_TITLE);
      await clickViewButton(page, PROJECT_TITLE);
      await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });
    }

    // Verificar que el badge actual es visible (estado "Parado")
    const statusBadge = page.locator('app-project-status-badge').first();
    await expect(statusBadge).toBeVisible();

    // El select de transición de estado está visible en la página de detalle del proyecto
    // El nz-select aparece como un textbox + arrow "down" junto al badge de estado
    // Hacemos click en el contenedor del select (que contiene el arrow down)
    // El select está en la cabecera del proyecto junto al badge de estado "Parado"

    // Buscamos el nz-select que está cerca del badge de estado
    // El árbol muestra: "text: Parado", "textbox", "text: Parado", "img 'down'"
    // El nz-select es el contenedor de ese textbox + img down
    const statusSection = page.locator('main').locator('nz-select').first();
    await statusSection.click();

    // Esperar que el dropdown esté abierto y elegir "Planificando con cliente"
    const option = page.locator('.cdk-overlay-container nz-option-item')
      .filter({ hasText: /planificando con cliente/i });
    await expect(option).toBeVisible({ timeout: 8_000 });
    await option.click();

    // Esperar mensaje de éxito
    await expect(
      page.locator('.ant-message-notice').first()
    ).toBeVisible({ timeout: 10_000 });

    // El badge debe haber cambiado a "Planificando..."
    await expect(statusBadge).toContainText(/planificando/i, { timeout: 10_000 });
  });

});
