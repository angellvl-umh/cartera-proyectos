/**
 * auth.setup.ts
 *
 * Ejecutado UNA VEZ antes del proyecto "chromium" (dependencia de proyecto).
 * Hace login en Keycloak vía UI para cada rol y guarda el storageState en disco.
 * Los tests posteriores cargan esos ficheros con `test.use({ storageState })`.
 */
import { test as setup } from '@playwright/test';
import path from 'path';
import { loginAs } from './helpers/login';

const AUTH_DIR = path.join(__dirname, '.auth');

setup('auth: gestor', async ({ page }) => {
  await loginAs(page, 'gestor');
  await page.context().storageState({ path: path.join(AUTH_DIR, 'gestor.json') });
});

setup('auth: jefe', async ({ page }) => {
  await loginAs(page, 'jefe');
  await page.context().storageState({ path: path.join(AUTH_DIR, 'jefe.json') });
});

setup('auth: dev', async ({ page }) => {
  await loginAs(page, 'dev');
  await page.context().storageState({ path: path.join(AUTH_DIR, 'dev.json') });
});
