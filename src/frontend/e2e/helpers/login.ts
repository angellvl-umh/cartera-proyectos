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
  const appOrigin = new URL(process.env['E2E_BASE_URL'] ?? 'http://localhost:4200').origin;

  // 1. Navegar a la raíz — el guard AutoLoginPartialRoutesGuard redirigirá a Keycloak
  await page.goto('/');

  // 2. Esperar el formulario de login de Keycloak
  await page.waitForSelector('#kc-form-login', { timeout: 15_000 });

  // 3. Rellenar credenciales
  await page.locator('#username').fill(username);
  await page.locator('#password').fill(password);
  await page.locator('#kc-login').click();

  // 4. Esperar a que la app cargue: volver al origen del baseURL (no Keycloak)
  //    y que no sea la URL de callback OIDC
  await page.waitForURL(
    (url) => url.origin === appOrigin && !url.pathname.startsWith('/callback'),
    { timeout: 20_000 }
  );

  // 5. Esperar a que el layout de la app sea visible (app-root cargado)
  await page.waitForSelector('app-root', { timeout: 15_000 });

  // 6. Esperar a que el header sea visible
  await page.waitForSelector('header', { timeout: 10_000 });

  // 7. Esperar a que el heading de la página cargue
  await page.waitForSelector('h1', { timeout: 10_000 });
}

/**
 * Navega a una ruta protegida asegurando que el ciclo OIDC esté completado.
 *
 * La app Angular con OIDC siempre procesa el callback en la primera carga
 * y puede redirigir al dashboard. Esta función:
 *  1. Navega a la ruta directamente.
 *  2. Si acaba en /callback o /dashboard (y la ruta destino no es /dashboard),
 *     hace una segunda navegación una vez que la sesión OIDC está inicializada.
 */
export async function gotoAndWait(page: Page, path: string): Promise<void> {
  const appOrigin = new URL(process.env['E2E_BASE_URL'] ?? 'http://localhost:4200').origin;

  // Verificar si ya estamos en la app (sesión ya inicializada)
  const currentUrl = page.url();
  const alreadyInApp = currentUrl.startsWith(appOrigin) &&
    !currentUrl.includes('/callback') &&
    currentUrl !== appOrigin + '/' &&
    currentUrl !== '';

  if (!alreadyInApp) {
    // Primera carga: ir al dashboard para inicializar el cliente OIDC
    await page.goto('/dashboard');
    await page.waitForURL(
      (url) => url.origin === appOrigin && !url.pathname.startsWith('/callback'),
      { timeout: 20_000 }
    );
    await page.waitForSelector('h1', { timeout: 15_000 });
  }

  // Navegar a la ruta destino si no estamos ya en ella
  const targetPath = path.startsWith('/') ? path : '/' + path;
  const urlAlreadyMatches = page.url().includes(targetPath) && !page.url().includes('/callback');

  if (!urlAlreadyMatches) {
    await page.goto(path);
    await page.waitForURL(
      (url) => url.origin === appOrigin && !url.pathname.startsWith('/callback'),
      { timeout: 15_000 }
    );
    await page.waitForSelector('header', { timeout: 10_000 });
  }
}
