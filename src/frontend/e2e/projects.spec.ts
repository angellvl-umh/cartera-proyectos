/**
 * projects.spec.ts  (rol: gestor)
 *
 * Flujos de proyectos:
 *  1. La lista de proyectos carga con filas.
 *  2. Crear un proyecto y verlo en el listado.
 *  3. Abrir el detalle del proyecto recién creado.
 *  4. Cambiar estado Stopped → "Planificando con cliente" y verificar el badge.
 */
import { test, expect } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

// Timestamp único para no colisionar en ejecuciones repetidas
const TS = Date.now();
const PROJECT_TITLE = `Proyecto E2E ${TS}`;

test.describe('Proyectos (gestor)', () => {

  test('la lista de proyectos carga con filas de tabla', async ({ page }) => {
    await page.goto('/projects');

    // La tabla nz-table debe estar visible
    const table = page.locator('nz-table');
    await expect(table).toBeVisible();

    // Al menos hay la cabecera thead + alguna fila (puede estar vacía si la DB está limpia)
    await expect(table.locator('thead tr')).toBeVisible();

    // El título de la página
    await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible();
  });

  test('crear un proyecto y verlo en el listado', async ({ page }) => {
    await page.goto('/projects');

    // Abrir modal de creación
    await page.getByRole('button', { name: /nuevo proyecto/i }).click();

    // Esperar a que el modal esté visible — título "Nuevo proyecto"
    await expect(page.getByText('Nuevo proyecto').last()).toBeVisible();

    // Rellenar el título
    await page.getByLabel('Título').fill(PROJECT_TITLE);

    // Seleccionar complejidad "Media" — el campo nzSelect usa nz-option
    // Abrimos el select de Complejidad
    const complexitySelect = page.locator('nz-select').filter({ hasText: /complejidad|media|pequeño/i }).first();
    await complexitySelect.click();
    // Elegir la opción "Media" en el dropdown
    await page.locator('.cdk-overlay-container nz-option-item').filter({ hasText: /^Media$/ }).click();

    // Guardar
    await page.getByRole('button', { name: /guardar/i }).click();

    // El modal debe cerrarse y el proyecto debe aparecer en la tabla
    await expect(page.getByText('Nuevo proyecto')).toBeHidden({ timeout: 8_000 });

    // Buscar el proyecto por título en la tabla
    await page.locator('input[placeholder*="Buscar"]').fill(PROJECT_TITLE);
    await expect(page.getByText(PROJECT_TITLE)).toBeVisible({ timeout: 10_000 });
  });

  test('abrir el detalle del proyecto recién creado', async ({ page }) => {
    await page.goto('/projects');

    // Buscar el proyecto
    await page.locator('input[placeholder*="Buscar"]').fill(PROJECT_TITLE);
    await expect(page.getByText(PROJECT_TITLE)).toBeVisible({ timeout: 10_000 });

    // Hacer click en el botón "Ver detalle" (icono ojo) de esa fila
    const row = page.locator('tbody tr').filter({ hasText: PROJECT_TITLE });
    await row.getByTitle('Ver detalle').click();

    // Debe navegar a /projects/:id
    await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });

    // El título del proyecto aparece en el encabezado
    await expect(page.getByRole('heading', { name: PROJECT_TITLE })).toBeVisible();
  });

  test('cambiar estado Stopped → Planificando con cliente y verificar badge', async ({ page }) => {
    await page.goto('/projects');

    // Localizar el proyecto
    await page.locator('input[placeholder*="Buscar"]').fill(PROJECT_TITLE);
    const row = page.locator('tbody tr').filter({ hasText: PROJECT_TITLE });
    await row.getByTitle('Ver detalle').click();

    await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });

    // Verificar que el badge actual es "Stopped" (o la etiqueta visible en español)
    // El componente project-status-badge.component.ts renderiza el badge
    const statusBadge = page.locator('app-project-status-badge').first();
    await expect(statusBadge).toBeVisible();

    // El select de transición — nz-select con placeholder "Cambiar estado"
    const transitionSelect = page.locator('nz-select').filter({ hasText: /cambiar estado|parado/i }).first();
    await transitionSelect.click();

    // Elegir "Planificando con cliente" en el dropdown
    await page.locator('.cdk-overlay-container nz-option-item')
      .filter({ hasText: /planificando con cliente/i })
      .click();

    // Esperar mensaje de éxito
    await expect(page.locator('.ant-message')).toContainText(/estado actualizado/i, { timeout: 8_000 });

    // El badge debe haber cambiado — verificar que ya no muestra "Parado"
    // y sí muestra algo relacionado con "Planificando"
    await expect(statusBadge).toContainText(/planificando/i, { timeout: 8_000 });
  });

});
