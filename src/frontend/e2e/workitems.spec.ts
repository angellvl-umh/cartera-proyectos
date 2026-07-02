/**
 * workitems.spec.ts  (rol: gestor)
 *
 * Flujos de work items dentro de un proyecto creado por el propio test:
 *  1. Crear una tarea y verla en el listado.
 *  2. Cambiar el estado de la tarea (Backlog → En curso).
 *  3. Descartar la tarea (Descartar + confirmación) → estado "Descartada".
 *
 * El test crea su propio proyecto para ser autosuficiente.
 */
import { test, expect } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

const TS = Date.now();
const PROJECT_TITLE = `WI E2E ${TS}`;
const TASK_TITLE    = `Tarea E2E ${TS}`;

test.describe('Work Items (gestor)', () => {

  /**
   * Helper: crea un proyecto y devuelve su URL de detalle.
   */
  async function createProject(page: import('@playwright/test').Page): Promise<void> {
    await page.goto('/projects');
    await page.getByRole('button', { name: /nuevo proyecto/i }).click();
    await expect(page.getByText('Nuevo proyecto').last()).toBeVisible();
    await page.getByLabel('Título').fill(PROJECT_TITLE);

    // Complejidad: seleccionar la primera opción disponible (Muy pequeño)
    const complexitySelect = page.locator('nz-select').filter({ hasText: /complejidad/i }).first();
    await complexitySelect.click();
    await page.locator('.cdk-overlay-container nz-option-item').first().click();

    await page.getByRole('button', { name: /guardar/i }).click();
    await expect(page.getByText('Nuevo proyecto')).toBeHidden({ timeout: 8_000 });
  }

  /**
   * Helper: navega al detalle del proyecto y abre la pestaña Product Backlog.
   */
  async function goToBacklog(page: import('@playwright/test').Page): Promise<void> {
    await page.goto('/projects');
    await page.locator('input[placeholder*="Buscar"]').fill(PROJECT_TITLE);
    const row = page.locator('tbody tr').filter({ hasText: PROJECT_TITLE });
    await row.getByTitle('Ver detalle').click();
    await expect(page).toHaveURL(/\/projects\/\d+$/, { timeout: 10_000 });

    // Ir a la pestaña "Product Backlog"
    await page.getByRole('tab', { name: /product backlog/i }).click();
  }

  test('crear una tarea y verla en el listado', async ({ page }) => {
    await createProject(page);
    await goToBacklog(page);

    // Abrir modal "Nueva tarea"
    await page.getByRole('button', { name: /nueva tarea/i }).click();
    await expect(page.getByText(/nueva tarea/i).last()).toBeVisible();

    // Rellenar título
    await page.getByLabel('Título').fill(TASK_TITLE);

    // Guardar
    await page.getByRole('button', { name: /guardar/i }).click();
    await expect(page.getByText(/nueva tarea/i).last()).toBeHidden({ timeout: 8_000 });

    // La tarea debe aparecer en la tabla del backlog
    await expect(page.getByText(TASK_TITLE)).toBeVisible({ timeout: 10_000 });
  });

  test('cambiar el estado de la tarea a En curso', async ({ page }) => {
    await goToBacklog(page);

    // La tarea ya existe (creada por el test anterior — misma sesión si se ejecutan en orden)
    // pero los tests deben ser idempotentes; si no existe, la creamos
    const taskExists = await page.getByText(TASK_TITLE).isVisible().catch(() => false);
    if (!taskExists) {
      await page.getByRole('button', { name: /nueva tarea/i }).click();
      await page.getByLabel('Título').fill(TASK_TITLE);
      await page.getByRole('button', { name: /guardar/i }).click();
      await expect(page.getByText(TASK_TITLE)).toBeVisible({ timeout: 10_000 });
    }

    // Abrir el modal de edición de la tarea
    const row = page.locator('tbody tr').filter({ hasText: TASK_TITLE });
    await row.getByTitle('Editar').click();

    await expect(page.getByText(/editar tarea/i).last()).toBeVisible();

    // Cambiar el estado a "En curso" — el select de estado está en el modal de edición
    // Solo aparece al editar (el template lo muestra condicionalmente con @if (editingWorkItem()))
    const statusSelect = page.locator('nz-modal').filter({ hasText: /editar tarea/i })
      .locator('nz-select').filter({ hasText: /backlog|por hacer|en curso|bloqueada|hecho/i });
    await statusSelect.click();
    await page.locator('.cdk-overlay-container nz-option-item').filter({ hasText: /en curso/i }).click();

    await page.getByRole('button', { name: /guardar/i }).click();
    await expect(page.getByText(/editar tarea/i).last()).toBeHidden({ timeout: 8_000 });

    // El estado de la fila debe haber cambiado
    const updatedRow = page.locator('tbody tr').filter({ hasText: TASK_TITLE });
    await expect(updatedRow.locator('nz-tag').filter({ hasText: /en curso/i })).toBeVisible({ timeout: 10_000 });
  });

  test('descartar la tarea y verificar estado Descartada', async ({ page }) => {
    await goToBacklog(page);

    // Localizar la tarea
    const row = page.locator('tbody tr').filter({ hasText: TASK_TITLE });

    // Click en el botón "Descartar" (icono stop, nz-popconfirm)
    // El botón tiene title implícito; lo localizamos por el icono nzType="stop"
    await row.locator('[nz-icon][nztype="stop"], button:has([nz-icon])').last().click();

    // Confirmar el popconfirm — el texto es "¿Descartar esta tarea? Es irreversible."
    // El botón de confirmación en el overlay de Ant Design
    await page.locator('.ant-popover-buttons button').filter({ hasText: /sí|confirmar|ok/i }).click();

    // Esperar mensaje de éxito
    await expect(page.locator('.ant-message')).toContainText(/descart/i, { timeout: 8_000 });

    // El tag de estado debe mostrar "Descartada"
    const updatedRow = page.locator('tbody tr').filter({ hasText: TASK_TITLE });
    await expect(updatedRow.locator('nz-tag').filter({ hasText: /descart/i })).toBeVisible({ timeout: 10_000 });
  });

});
