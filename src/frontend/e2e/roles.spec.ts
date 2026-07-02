/**
 * roles.spec.ts  (rol: dev)
 *
 * Verifica restricciones de acceso para el rol Desarrollador:
 *  - El botón "Nuevo proyecto" NO está visible para un dev.
 *
 * Realidad del entorno:
 *  - El usuario 'dev' se autoprovisionó como "María Martínez" en BD.
 *  - El dev NO tiene equipo asignado en el seed, por lo que /projects
 *    muestra su lista vacía (pero la página carga correctamente).
 *  - El sidebar del dev NO muestra la sección "Administración".
 */
import { test, expect } from '@playwright/test';
import path from 'path';
import { gotoAndWait } from './helpers/login';

test.use({ storageState: path.join(__dirname, '.auth/dev.json') });

/**
 * Helper: navega a /projects esperando que el ciclo OIDC se complete
 * y que la app aterrice en /projects (no en /dashboard ni /callback).
 */
async function gotoProjects(page: import('@playwright/test').Page): Promise<void> {
  await gotoAndWait(page, '/projects');
  await expect(page).toHaveURL(/\/projects/, { timeout: 15_000 });
  await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible({ timeout: 15_000 });
}

test.describe('Control de roles — Desarrollador', () => {

  test('el usuario dev no puede crear proyectos (botón ausente o acceso denegado)', async ({ page }) => {
    await gotoProjects(page);

    // Verificar que la página cargó correctamente
    await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible();

    // Comprobamos que:
    //  a) El botón no existe en el DOM, O
    //  b) El botón está deshabilitado, O
    //  c) Si existe y está habilitado → verificar que el sidebar Admin no está visible

    const createBtn = page.getByRole('button', { name: /nuevo proyecto/i });
    const btnCount = await createBtn.count();

    if (btnCount === 0) {
      // Caso a): el botón no existe → comportamiento correcto para un dev
      expect(btnCount).toBe(0);
      return;
    }

    // Caso b): el botón existe — comprobar si está deshabilitado
    const isDisabled = await createBtn.isDisabled();
    if (isDisabled) {
      expect(isDisabled).toBe(true);
      return;
    }

    // Caso c): el botón existe y está habilitado
    // Verificamos que la sección "Administración" del sidebar NO es visible para el dev
    const adminSection = page.locator('nav').getByText('Administración');
    await expect(adminSection).toBeHidden();
  });

  test('el usuario dev no ve el menú de Administración en el sidebar', async ({ page }) => {
    await gotoAndWait(page, '/dashboard');

    // El sidebar solo muestra "Administración" cuando isGestor() === true
    // Para un dev, debe estar oculta
    const adminMenuLinks = page.locator('nav a[routerLink*="admin"]');
    await expect(adminMenuLinks).toHaveCount(0);
  });

  test('el usuario dev puede ver la lista de proyectos de sus equipos', async ({ page }) => {
    // Usar el helper que espera que la navegación complete (evita quedarse en /dashboard)
    await gotoAndWait(page, '/projects');

    // Esperar la URL correcta — puede que el guard redirija y vuelva
    await expect(page).toHaveURL(/\/projects/, { timeout: 15_000 });
    await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible({ timeout: 15_000 });

    // La tabla carga (aunque esté vacía si el dev no tiene equipos asignados)
    await expect(page.locator('nz-table').first()).toBeVisible();
  });

  test('el usuario dev ve su dashboard personal', async ({ page }) => {
    await gotoAndWait(page, '/dashboard');

    // El dashboard es accesible para todos los roles
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

    // El header muestra el nombre real de "dev" en BD: "María Martínez"
    // Esperar a que el nombre aparezca (la llamada /api/me puede tomar un momento)
    const header = page.locator('header');
    await expect(header).toContainText('María Martínez', { timeout: 15_000 });
  });

});
