# Spec: Login sin auto-provisión — solo personas pre-registradas

## Contexto y objetivo

Hoy `/api/me` (en `src/CarteraProyectos.Api/Endpoints/UserEndpoints.cs`) **auto-crea** una `Person` en el primer login a partir de los claims del JWT. Queremos eliminar ese comportamiento:

1. Las personas las crea un **Gestor** desde la app (flujo ya existente: `POST /api/persons`, pre-registro por email, `SubjectId = null`).
2. Hacer login **NO** crea personas.
3. Si alguien autenticado en Keycloak/SSO **no existe** como `Person` → respuesta **403** con mensaje claro, y el frontend muestra una pantalla de "sin acceso".
4. Si la persona existe pero está **desactivada** (`IsActive = false`) → mismo trato (403 + pantalla).
5. **Única excepción (bootstrap):** los emails de `Admin:InitialGestorEmails` (config ya existente) sí se auto-crean como `Gestor` en su primer login, para no dejar un entorno recién levantado sin ningún gestor. Es el único camino de creación por login.

Se mantiene la **vinculación por email**: si existe una `Person` pre-registrada con ese email y `SubjectId` null (o distinto porque se recreó el realm), en el primer login se le asigna el `sub` del token. Esto NO es creación, es vinculación, y se conserva tal cual.

---

## Backend

### 1. Nuevo caso de uso `Core/Features/Users/ResolveCurrentUser.cs`

La lógica sale del endpoint (convención: nunca lógica de negocio en endpoints). Un archivo con Command + Handler + Result:

```csharp
namespace CarteraProyectos.Core.Features.Users;

public enum ResolveUserStatus { Ok, NotRegistered, Inactive }

// bootstrapGestorEmails: lista de Admin:InitialGestorEmails, la pasa el endpoint
// (Core no debe depender de IConfiguration)
public record ResolveCurrentUserCommand(
    string SubjectId,
    string Name,
    string Email,
    string[] BootstrapGestorEmails) : IRequest<ResolveCurrentUserResult>;

public record ResolveCurrentUserResult(
    ResolveUserStatus Status,
    int? Id, string? SubjectId, string? Name, string? Email, bool? IsActive, string? Role);
```

Lógica del handler (usa `IAppDbContext`):

1. Buscar `Person` por `SubjectId == command.SubjectId`.
2. Si no existe, buscar por `Email == command.Email` (case-sensitive igual que hoy). Si existe → `person.UpdateSubjectId(sub)` + `SaveChangesAsync` (vinculación de pre-registro).
3. Si sigue sin existir:
   - Si `command.Email` está en `BootstrapGestorEmails` (comparación `OrdinalIgnoreCase`) → crear con `Person.CreateFromClaims(sub, name, email, PersonRole.Gestor)` + guardar → continuar como encontrada.
   - Si no → devolver `Status = NotRegistered` (resto de campos null).
4. Si la persona (encontrada, vinculada o bootstrap) tiene `IsActive == false` → devolver `Status = Inactive`.
5. En caso feliz → `Status = Ok` con todos los datos.

### 2. `UserEndpoints.cs` — `/api/me` sin lógica

El endpoint solo extrae claims (`sub`, `name`/`preferred_username`, `email`), lee `Admin:InitialGestorEmails` de `IConfiguration`, envía el command via `ISender` y mapea:

- `Ok` → `200` con el mismo shape actual: `{ Id, SubjectId, Name, Email, IsActive, Role }`.
- `NotRegistered` → `Results.Problem("No tienes acceso a la aplicación. Solicita el alta a un gestor de la cartera.", statusCode: 403)`.
- `Inactive` → `Results.Problem("Tu usuario está desactivado. Contacta con un gestor de la cartera.", statusCode: 403)`.
- Sin claim `sub` → `401` (como hoy).

Actualizar `.WithDescription(...)`: ya no "crea el usuario si no existe"; describir el 403 (la spec OpenAPI la consume el Tool Server del agente IA — descripción en español).

### 3. `CurrentUser.ResolveAsync` — excluir inactivos y vincular por email

En `src/CarteraProyectos.Api/Endpoints/CurrentUser.cs`:

1. Añadir la condición `p.IsActive` a la búsqueda por `SubjectId`.
2. Si no resuelve por `sub`, hacer el mismo fallback de vinculación por email (claim `email` → `UpdateSubjectId` + save). Motivo: en el **primer login** de una persona pre-registrada, las llamadas API que llegan en paralelo con `/api/me` (p. ej. `/api/dashboard`) aún no encuentran el `SubjectId` vinculado y devolverían 401 — condición de carrera detectada en la validación E2E. Con el fallback en el helper, cualquier endpoint se autorrepara. La creación bootstrap sigue existiendo SOLO en `/api/me`.

Efecto: una persona desactivada resuelve a `null` en TODOS los endpoints que usan `CurrentUser.ResolveAsync` (devuelven 401 hoy) — defensa en profundidad aunque el frontend ya la haya expulsado via `/api/me`. Los endpoints `/api/agent/*` no cambian (ya validan `IsActive` en su `Guard`).

### 4. Tests unitarios (`tests/CarteraProyectos.UnitTests/Features/Users/ResolveCurrentUserHandlerTests.cs`)

xUnit + EF InMemory + Shouldly, patrón de los tests existentes de Persons. Casos mínimos:

1. **Happy path**: persona existente por `SubjectId` activa → `Ok` con sus datos.
2. **Vinculación**: persona pre-registrada (SubjectId null) con ese email → `Ok` y `SubjectId` actualizado en BD.
3. **Re-vinculación**: persona con `SubjectId` antiguo distinto y mismo email → `Ok` y `SubjectId` actualizado.
4. **No registrada**: sub y email desconocidos, email NO en bootstrap → `NotRegistered` y **no** se crea ninguna Person (count sin cambios).
5. **Bootstrap gestor**: email en `BootstrapGestorEmails` (probar case-insensitive) → `Ok`, Person creada con `Role = Gestor`.
6. **Inactiva por sub**: persona existente desactivada → `Inactive`.
7. **Inactiva pre-registrada**: persona por email desactivada → `Inactive` (y sin vincular o vinculada, indiferente, pero sin acceso).

Verificación: `dotnet build src/` y `dotnet test` en verde.

> **Modelo recomendado:** `claude-sonnet-4.6` — cambio de auth transversal (nuevo caso de uso + refactor de endpoint + regla global de resolución de usuario).

---

## Frontend

### 1. Página `features/access-denied/access-denied.component.ts`

Standalone, OnPush, NG-ZORRO `nz-result`:

- `nzStatus="403"`, título "Sin acceso".
- Subtítulo dinámico según query param `reason`: `inactive` → "Tu usuario está desactivado. Contacta con un gestor de la cartera."; por defecto → "No tienes acceso a la aplicación. Solicita el alta a un gestor de la cartera."
- Botón "Cerrar sesión" que llama a `OidcSecurityService.logoff()` (permite reintentar con otra cuenta).

### 2. Ruta

En `app.routes.ts`: ruta lazy `access-denied` con `loadComponent`, protegida por el mismo guard OIDC que el resto (el usuario SÍ está autenticado en Keycloak; lo que no tiene es Person).

### 3. `AppComponent` — reaccionar al 403 de `/api/me`

En el pipe del signal `me` (app.component.ts:154-162), sustituir el `catchError(() => of(null))`:

- Si `err.status === 403`: navegar a `/access-denied` (con `?reason=inactive` si el `detail`/mensaje del ProblemDetails contiene "desactivado") y devolver `of(null)`.
- Otros errores: comportamiento actual (`of(null)`).

Añadir un signal/computed `denied` (true tras el 403) y en el template **ocultar el sidebar y el header** (`@if (!denied())`) para que la pantalla de sin-acceso no muestre navegación que solo daría errores 401.

### 4. Verificación

Build del frontend **via Docker** (Node local no compila Angular 21): `docker compose build frontend`. Si hay tests unitarios Vitest afectados, actualizarlos.

> **Modelo recomendado:** `claude-sonnet-4.6` — toca app.component (shell), routing y un componente nuevo con contrato entre ellos.

---

## Fuera de alcance (lo hará el orquestador después)

- Actualizar CLAUDE.md (regla de negocio 4 y estado actual).
- E2E Playwright del flujo "usuario no registrado ve pantalla sin acceso" (requiere usuario extra en el realm de Keycloak sin Person en seed).
