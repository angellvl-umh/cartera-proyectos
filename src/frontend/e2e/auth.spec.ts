/**
 * auth.spec.ts
 *
 * Flujos de autenticación:
 *  1. Login como gestor → dashboard con nombre del usuario visible.
 *  2. Acceso sin sesión → redirección a Keycloak.
 */
import { test, expect } from '@playwright/test';
import path from 'path';

// ── Test 1: usuario autenticado ve el dashboard ───────────────────────────────

test.describe('Autenticación — gestor', () => {
  test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

  test('login como gestor muestra el dashboard con el nombre del usuario', async ({ page }) => {
    // Arrange & Act
    await page.goto('/dashboard');

    // Assert — el layout debe mostrar el nombre "gestor" en el header
    // El app.component.ts muestra me()!.name en el header
    const header = page.locator('header');
    await expect(header).toBeVisible();

    // El titulo de la página de dashboard debe ser visible
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

    // El usuario aparece en el header (nombre del perfil)
    // El componente muestra me()!.name — el usuario de Keycloak se llama "gestor"
    await expect(header).toContainText('gestor');
  });

  test('usuario autenticado puede navegar a proyectos desde el sidebar', async ({ page }) => {
    await page.goto('/dashboard');

    // Click en el enlace "Proyectos" del sidebar
    await page.getByRole('link', { name: 'Proyectos' }).click();
    await expect(page).toHaveURL(/\/projects/);

    // La cabecera h1 de la lista debe aparecer
    await expect(page.getByRole('heading', { name: 'Cartera de Proyectos' })).toBeVisible();
  });
});

// ── Test 2: sin sesión → Keycloak ────────────────────────────────────────────

test.describe('Autenticación — sin sesión', () => {
  // Sin storageState: contexto limpio
  test('un usuario sin sesión es redirigido a Keycloak', async ({ page }) => {
    // Navegar directamente a una ruta protegida
    await page.goto('/dashboard');

    // El guard AutoLoginPartialRoutesGuard redirige a Keycloak
    // Esperamos que la URL contenga el dominio de Keycloak
    await expect(page).toHaveURL(/keycloak|kc-form-login|openid|auth\/realms/, { timeout: 15_000 });

    // Alternativamente, el formulario de login de Keycloak debe estar presente
    await expect(page.locator('#kc-form-login')).toBeVisible({ timeout: 15_000 });
  });
});
