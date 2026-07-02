/**
 * kanban.spec.ts  (rol: dev)
 *
 * Verifica que el tablero Kanban de un proyecto con datos de semilla:
 *  - Carga correctamente.
 *  - Muestra las columnas: Backlog, Por hacer, En progreso, Bloqueada, Hecha.
 *
 * Textos de columna extraídos de COLUMNS_DEF en kanban-board.component.ts:
 *   { status: 'Backlog',    label: 'Backlog' }
 *   { status: 'ToDo',       label: 'Por hacer' }
 *   { status: 'InProgress', label: 'En progreso' }
 *   { status: 'Blocked',    label: 'Bloqueada' }
 *   { status: 'Done',       label: 'Hecha' }
 *
 * Requiere datos de semilla (infra/seed.sql) para tener al menos un proyecto
 * con un sprint activo. Ajusta SEEDED_PROJECT_ID si es necesario.
 */
import { test, expect } from '@playwright/test';
import path from 'path';

test.use({ storageState: path.join(__dirname, '.auth/dev.json') });

/**
 * ID del proyecto con datos de semilla que tiene un tablero Kanban disponible.
 * El dev debe ser miembro del equipo asignado a este proyecto.
 * Ajusta según infra/seed.sql — por defecto asumimos id=1.
 */
const SEEDED_PROJECT_ID = 1;

const KANBAN_COLUMNS = [
  'Backlog',
  'Por hacer',
  'En progreso',
  'Bloqueada',
  'Hecha',
];

test.describe('Kanban (dev)', () => {

  test('el tablero Kanban carga y muestra todas las columnas', async ({ page }) => {
    // Navegar directamente al kanban del proyecto de semilla
    await page.goto(`/projects/${SEEDED_PROJECT_ID}/kanban`);

    // Esperar a que el componente cargue (título "Kanban del proyecto" o "Tablero Kanban")
    await expect(
      page.getByRole('heading', { name: /kanban/i })
    ).toBeVisible({ timeout: 15_000 });

    // Verificar que todas las columnas están presentes por su label (col-label)
    for (const columnLabel of KANBAN_COLUMNS) {
      const column = page.locator('.col-label').filter({ hasText: columnLabel });
      await expect(column).toBeVisible({ timeout: 10_000 });
    }
  });

  test('el tablero muestra el contador de tareas en cada columna', async ({ page }) => {
    await page.goto(`/projects/${SEEDED_PROJECT_ID}/kanban`);

    await expect(
      page.getByRole('heading', { name: /kanban/i })
    ).toBeVisible({ timeout: 15_000 });

    // Cada columna tiene un .col-count con el número de tareas
    const countBadges = page.locator('.col-count');
    await expect(countBadges).toHaveCount(KANBAN_COLUMNS.length, { timeout: 10_000 });
  });

  test('el tablero permite buscar tareas por texto', async ({ page }) => {
    await page.goto(`/projects/${SEEDED_PROJECT_ID}/kanban`);

    await expect(
      page.getByRole('heading', { name: /kanban/i })
    ).toBeVisible({ timeout: 15_000 });

    // El campo de búsqueda está presente
    const searchInput = page.getByPlaceholder(/buscar tarea/i);
    await expect(searchInput).toBeVisible();

    // Escribir algo — el tablero filtra (puede resultar en 0 tarjetas, pero no debe romperse)
    await searchInput.fill('zzz_no_existe_zzzz');

    // El botón "Limpiar filtros" debe aparecer
    await expect(page.getByRole('button', { name: /limpiar filtros/i })).toBeVisible();
  });

});
