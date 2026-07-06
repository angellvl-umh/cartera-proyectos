# Tests E2E — Cartera de Proyectos TIC

Tests de extremo a extremo con [Playwright](https://playwright.dev/) para el frontend Angular 21.

## Dos stacks: uso vs E2E

Los tests crean proyectos y tareas reales, así que **no deben ejecutarse contra el stack de uso** (volumen `pgdata`, donde vive tu base de demo/pruebas manuales). Para eso existe `docker-compose.e2e.yml`: un override que monta la BD sobre un volumen tmpfs **efímero** (`pgdata_e2e`). Cada arranque parte de una BD vacía: el backend la migra al arrancar y `pnpm stack:e2e:up` aplica `infra/seed.sql` al final (personas —incluidos los usuarios de Keycloak pre-registrados—, equipos y proyectos). Al parar el stack, los datos E2E se descartan.

| Stack | Comando (desde `src/frontend`) | Volumen BD |
|-------|-------------------------------|-----------|
| E2E (efímero) | `pnpm stack:e2e:up` / `pnpm stack:e2e:down` | tmpfs, se descarta al parar |
| Uso (persistente) | `pnpm stack:up` | `pgdata`, nunca lo tocan los tests |

⚠️ No uses `docker compose down -v`: borraría también `pgdata` con tus datos de uso.

## Requisitos previos

1. **Stack E2E levantado y sembrado** (espera a los healthchecks de `db`, `keycloak`, `backend` y `frontend`, y aplica `infra/seed.sql`):
   ```bash
   pnpm stack:e2e:up
   ```
   Para re-aplicar solo el seed (idempotente): `pnpm stack:e2e:seed`. Ver `infra/SEED.md`.

3. **Node.js / pnpm** instalados (misma versión que el proyecto).

4. **Navegador Chromium** de Playwright instalado:
   ```bash
   pnpm exec playwright install chromium
   ```

## Comandos

```bash
# Ejecutar todos los tests E2E (headless)
pnpm e2e

# Abrir la interfaz visual de Playwright
pnpm e2e:ui

# Ciclo completo: levantar stack E2E efímero, testear y tirar el stack
pnpm stack:e2e:up && pnpm e2e && pnpm stack:e2e:down

# Volver al stack de uso (recrea `db` apuntando al volumen persistente pgdata)
pnpm stack:up

# Listar los tests sin ejecutarlos
pnpm exec playwright test --list

# Ver el informe HTML del último run
pnpm exec playwright show-report playwright-report
```

## Variables de entorno

| Variable        | Descripción                              | Valor por defecto         |
|-----------------|------------------------------------------|---------------------------|
| `E2E_BASE_URL`  | URL base de la aplicación frontend       | `http://localhost:4200`   |

Ejemplo para apuntar a un entorno diferente:
```bash
E2E_BASE_URL=http://staging.cartera.local pnpm e2e
```

## Usuarios de prueba (Keycloak realm `cartera`)

| Rol           | Usuario  | Contraseña  |
|---------------|----------|-------------|
| Gestor        | gestor   | gestor123   |
| Jefe de equipo| jefe     | jefe123     |
| Desarrollador | dev      | dev123      |

## Estructura

```
e2e/
├── .auth/              # storageState generado por auth.setup.ts (gitignored)
├── helpers/
│   └── login.ts        # Helper loginAs(page, role)
├── auth.setup.ts       # Setup: hace login una vez por rol y guarda .auth/*.json
├── auth.spec.ts        # Tests de autenticación
├── projects.spec.ts    # Tests de proyectos (gestor)
├── workitems.spec.ts   # Tests de work items (gestor)
├── kanban.spec.ts      # Tests del tablero Kanban (dev)
└── roles.spec.ts       # Tests de control de roles (dev)
```

## Notas

- Los tests que crean datos usan timestamps en los nombres para tolerar ejecuciones repetidas.
- El test de Kanban (`kanban.spec.ts`) depende de datos de semilla — ajusta `SEEDED_PROJECT_ID` si el ID del proyecto cambia.
- Los artefactos de fallo (capturas de pantalla, trazas) se guardan en `playwright-results/`.
