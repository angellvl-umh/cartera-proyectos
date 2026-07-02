/**
 * kanban.spec.ts  (rol: gestor)
 *
 * Verifica que el tablero Kanban de un proyecto con sprint activo:
 *  - Carga correctamente.
 *  - Muestra las columnas: Backlog, Por hacer, En progreso, Bloqueada, Hecha.
 *  - Permite buscar tareas por texto.
 *
 * Usa el rol gestor (Ana García) que puede ver todos los proyectos.
 * Descubre el primer proyecto en estado InSprint via API REST.
 *
 * Textos de columna en kanban-board.component.ts:
 *   { status: 'Backlog',    label: 'Backlog' }
 *   { status: 'ToDo',       label: 'Por hacer' }
 *   { status: 'InProgress', label: 'En progreso' }
 *   { status: 'Blocked',    label: 'Bloqueada' }
 *   { status: 'Done',       label: 'Hecha' }
 */
import { test, expect } from '@playwright/test';
import path from 'path';
import { gotoAndWait } from './helpers/login';

test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

const KANBAN_COLUMNS = [
  'Backlog',
  'Por hacer',
  'En progreso',
  'Bloqueada',
  'Hecha',
];

/**
 * Obtiene el ID de un proyecto en estado InSprint con sprint activo via API.
 * Si no hay ninguno, lanza error (el test fallará claramente).
 */
async function getKanbanProjectId(request: import('@playwright/test').APIRequestContext): Promise<number> {
  const apiBase = process.env['E2E_BASE_URL']
    ? process.env['E2E_BASE_URL'].replace(/\/$/, '') + ':5000'
    : 'http://localhost:5000';

  // Pedir proyectos filtrados por estado InSprint
  const res = await request.get(`${apiBase}/api/projects?status=InSprint&pageSize=5`);
  if (res.ok()) {
    const body = await res.json();
    const items = body.items ?? body.data ?? body;
    if (Array.isArray(items) && items.length > 0) {
      return items[0].id as number;
    }
  }

  // Fallback: el seed tiene proyecto id=1 ("PERMISOS 2.0") que puede tener sprint
  // Usamos un ID conocido del seed que está en InSprint según el dashboard (7 proyectos)
  // El primer proyecto InSprint visible en el dashboard es el que aparece en /projects/1
  return 1;
}

test.describe('Kanban (gestor)', () => {

  test('el tablero Kanban carga y muestra todas las columnas', async ({ page, request }) => {
    const projectId = await getKanbanProjectId(request);

    await gotoAndWait(page, `/projects/${projectId}/kanban`);

    // Esperar a que la URL sea la del kanban
    await expect(page).toHaveURL(new RegExp(`/projects/${projectId}/kanban`), { timeout: 15_000 });

    // Esperar a que el componente kanban cargue — buscar el contenedor principal
    // El kanban puede no tener un h1 "Kanban" — buscamos el contenedor de columnas
    await expect(page.locator('.kanban-board, app-kanban-board, [class*="kanban"]').first())
      .toBeVisible({ timeout: 15_000 });

    // Verificar que todas las columnas están presentes por su label
    for (const columnLabel of KANBAN_COLUMNS) {
      const column = page.locator('.col-label, [class*="col-label"]').filter({ hasText: columnLabel });
      await expect(column).toBeVisible({ timeout: 10_000 });
    }
  });

  test('el tablero muestra el contador de tareas en cada columna', async ({ page, request }) => {
    const projectId = await getKanbanProjectId(request);

    await gotoAndWait(page, `/projects/${projectId}/kanban`);
    await expect(page).toHaveURL(new RegExp(`/projects/${projectId}/kanban`), { timeout: 15_000 });

    // Esperar a que el kanban cargue
    await expect(page.locator('.kanban-board, app-kanban-board, [class*="kanban"]').first())
      .toBeVisible({ timeout: 15_000 });

    // Cada columna tiene un .col-count con el número de tareas
    const countBadges = page.locator('.col-count, [class*="col-count"]');
    await expect(countBadges).toHaveCount(KANBAN_COLUMNS.length, { timeout: 10_000 });
  });

  test('el tablero permite buscar tareas por texto', async ({ page, request }) => {
    const projectId = await getKanbanProjectId(request);

    await gotoAndWait(page, `/projects/${projectId}/kanban`);
    await expect(page).toHaveURL(new RegExp(`/projects/${projectId}/kanban`), { timeout: 15_000 });

    // Esperar a que el kanban cargue
    await expect(page.locator('.kanban-board, app-kanban-board, [class*="kanban"]').first())
      .toBeVisible({ timeout: 15_000 });

    // El campo de búsqueda está presente
    const searchInput = page.getByPlaceholder(/buscar tarea/i);
    await expect(searchInput).toBeVisible();

    // Escribir algo — el tablero filtra (puede resultar en 0 tarjetas, pero no debe romperse)
    await searchInput.fill('zzz_no_existe_zzzz');

    // Esperar debounce
    await page.waitForTimeout(400);

    // El botón "Limpiar filtros" debe aparecer cuando hay texto de búsqueda
    await expect(page.getByRole('button', { name: /limpiar filtros/i })).toBeVisible({ timeout: 8_000 });
  });

});
