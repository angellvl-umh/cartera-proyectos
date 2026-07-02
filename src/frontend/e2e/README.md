# Tests E2E — Cartera de Proyectos TIC

Tests de extremo a extremo con [Playwright](https://playwright.dev/) para el frontend Angular 21.

## Requisitos previos

1. **Stack Docker levantado** con todos los servicios:
   ```bash
   docker compose up -d
   ```
   Servicios necesarios: `db` (PostgreSQL), `keycloak`, `backend` (.NET), `frontend` (Angular).

2. **Datos de semilla** cargados:
   ```bash
   # Aplica infra/seed.sql contra la base de datos
   docker compose exec db psql -U postgres -d cartera -f /seed.sql
   ```
   Ver `infra/SEED.md` para más detalles.

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
