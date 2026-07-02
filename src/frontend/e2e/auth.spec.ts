/**
 * auth.spec.ts
 *
 * Flujos de autenticación:
 *  1. Login como gestor → dashboard con nombre del usuario visible.
 *  2. Acceso sin sesión → redirección a Keycloak.
 */
import { test, expect } from '@playwright/test';
import path from 'path';
import { gotoAndWait } from './helpers/login';

// ── Test 1: usuario autenticado ve el dashboard ───────────────────────────────

test.describe('Autenticación — gestor', () => {
  test.use({ storageState: path.join(__dirname, '.auth/gestor.json') });

  test('login como gestor muestra el dashboard con el nombre del usuario', async ({ page }) => {
    // Arrange & Act — usamos gotoAndWait para que el ciclo OIDC se complete
    await gotoAndWait(page, '/dashboard');

    // Assert — el layout debe mostrar el nombre en el header
    const header = page.locator('header');
    await expect(header).toBeVisible();

    // El titulo de la página de dashboard debe ser visible
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

    // El usuario gestor se muestra en el sistema como "Ana García" (autoprovisionado desde Keycloak)
    // El componente muestra me()!.name que es el nombre real de la persona en BD
    await expect(header).toContainText('Ana García');
  });

  test('usuario autenticado puede navegar a proyectos desde el sidebar', async ({ page }) => {
    await gotoAndWait(page, '/dashboard');

    // Click en el enlace "Proyectos" del sidebar
    // El link tiene href="/projects" y está en el nav
    await page.locator('nav a[href="/projects"]').click();
    await expect(page).toHaveURL(/\/projects/, { timeout: 15_000 });

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
