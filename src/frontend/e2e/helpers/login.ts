import { Page } from '@playwright/test';

export type Role = 'gestor' | 'jefe' | 'dev';

const CREDENTIALS: Record<Role, { username: string; password: string }> = {
  gestor: { username: 'gestor', password: 'gestor123' },
  jefe:   { username: 'jefe',   password: 'jefe123'   },
  dev:    { username: 'dev',    password: 'dev123'     },
};

/**
 * Navega a la app y, si Keycloak redirige al formulario de login,
 * introduce las credenciales del rol indicado y espera a volver a la app.
 *
 * Compatible con el patrón storageState de Playwright:
 *   await loginAs(page, 'gestor');
 *   await page.context().storageState({ path: 'e2e/.auth/gestor.json' });
 */
export async function loginAs(page: Page, role: Role): Promise<void> {
  const { username, password } = CREDENTIALS[role];

  // 1. Navegar a la raíz — el guard AutoLoginPartialRoutesGuard redirigirá a Keycloak
  await page.goto('/');

  // 2. Esperar el formulario de login de Keycloak
  await page.waitForSelector('#kc-form-login', { timeout: 15_000 });

  // 3. Rellenar credenciales
  await page.locator('#username').fill(username);
  await page.locator('#password').fill(password);
  await page.locator('#kc-login').click();

  // 4. Esperar a que la app cargue (sidebar con "Cartera TIC" o breadcrumb)
  await page.waitForURL(/localhost:4200/, { timeout: 20_000 });

  // 5. Esperar a que el layout de la app sea visible (sidebar nav)
  await page.waitForSelector('app-root', { timeout: 15_000 });

  // Asegurarse de que el nombre del usuario aparece en el header
  // (indica que /api/me respondió y OIDC está completamente inicializado)
  await page.waitForSelector('header', { timeout: 10_000 });
}
