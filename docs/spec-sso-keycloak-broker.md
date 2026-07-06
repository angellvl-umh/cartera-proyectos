# Spec: alta de credenciales locales en Keycloak desde la app

## Contexto

Keycloak pasa a ser identity broker (Google + SAML2 universitario futuro + cuentas locales); la infra ya está hecha (compose con KC_DB postgres, realm con IdP google y cliente `cartera-admin`). Esta spec cubre la parte de aplicación: cuando el Gestor pre-registra una `Person` puede pedir que se cree también su cuenta usuario/contraseña en Keycloak vía Admin API, con contraseña temporal.

Realm ya configurado (`infra/keycloak/cartera-realm.json`): cliente confidencial `cartera-admin` (service account con roles `manage-users`, `view-users`, `query-users` de `realm-management`), secret por env. Config del backend ya en `docker-compose.yml`:

```
Keycloak__BaseUrl        http://keycloak:8080
Keycloak__Realm          cartera
Keycloak__AdminClientId  cartera-admin
Keycloak__AdminClientSecret  (default dev: cartera-admin-secret)
```

---

## Backend

### 1. Interfaz `Core/Interfaces/IIdentityProviderService.cs`

```csharp
namespace CarteraProyectos.Core.Interfaces;

public enum IdentityCredentialsStatus { Created, AlreadyExists, Unavailable }

public record IdentityCredentialsResult(IdentityCredentialsStatus Status, string? TemporaryPassword);

/// <summary>Gestión de cuentas locales en el identity provider (Keycloak).</summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Crea un usuario local con contraseña temporal (required action UPDATE_PASSWORD).
    /// AlreadyExists si el username/email ya existe (no es error).
    /// Unavailable si el IdP no responde (no debe tumbar el alta de la Person).
    /// </summary>
    Task<IdentityCredentialsResult> CreateUserWithTemporaryPasswordAsync(
        string name, string email, CancellationToken ct);
}
```

### 2. `Infrastructure/Identity/KeycloakAdminService.cs`

Implementación con `HttpClient` (registrar con `AddHttpClient<KeycloakAdminService>` y exponer como `IIdentityProviderService` en el DI de Infrastructure/Program.cs, donde se registren los demás servicios):

1. Token: `POST {BaseUrl}/realms/{Realm}/protocol/openid-connect/token` con `grant_type=client_credentials`, `client_id`, `client_secret` (form-urlencoded). No hace falta cachear el token (uso esporádico).
2. Crear usuario: `POST {BaseUrl}/admin/realms/{Realm}/users` con Bearer token y body:
   ```json
   {
     "username": "<email>",
     "email": "<email>",
     "firstName": "<name>",
     "enabled": true,
     "emailVerified": true,
     "requiredActions": ["UPDATE_PASSWORD"],
     "credentials": [{ "type": "password", "value": "<temp>", "temporary": true }]
   }
   ```
3. Contraseña temporal: generar 12 caracteres aleatorios criptográficos (`RandomNumberGenerator`) de un alfabeto sin ambiguos (sin `l/1/O/0`), p. ej. `Guid` NO — usar alfabeto explícito.
4. Mapeo de respuestas: `201` → `Created` (+password); `409` → `AlreadyExists`; cualquier excepción de red/5xx/fallo de token → `Unavailable` (loggear con `ILogger`, no lanzar).
5. Config con `IConfiguration` (claves de arriba) o un options record `KeycloakOptions` — seguir el estilo del proyecto (mirar cómo lee config `BedrockEmbeddingService`).

### 3. Extender `Core/Features/Persons/CreatePerson.cs`

- `CreatePersonCommand`: añadir `bool CreateLocalCredentials = false`. 
- El resultado pasa de `int` a `CreatePersonResult`:
  ```csharp
  public record CreatePersonResult(int Id, string? TemporaryPassword, string? CredentialsWarning);
  ```
- Handler (inyectar `IIdentityProviderService`): tras crear la Person, si `CreateLocalCredentials`:
  - `Created` → devolver la password temporal.
  - `AlreadyExists` → `CredentialsWarning = "Ya existía una cuenta en el proveedor de identidad para ese email; no se ha creado una nueva."`
  - `Unavailable` → `CredentialsWarning = "La persona se ha creado, pero no se pudo crear la cuenta local en el proveedor de identidad. Puede crearse más tarde o el usuario puede entrar con Google/SSO."`
  - La Person se crea SIEMPRE aunque Keycloak falle.
- Actualizar los call sites del command: `PersonEndpoints.cs` (añadir `CreateLocalCredentials` al `CreatePersonRequest`, devolver `{ id, temporaryPassword, credentialsWarning }` en el 201) y el wrapper del agente (`Core/Features/Agent/*` / `AgentEndpoints.cs`): el agente NO crea credenciales — pasar `CreateLocalCredentials: false` y adaptar al nuevo tipo de resultado usando solo el `Id`.
- Actualizar `.WithDescription` del endpoint (en español, es la spec del Tool Server).

### 4. Tests unitarios (`tests/.../Features/Persons/CreatePersonHandlerTests.cs` — ampliar los existentes)

Sustituir `IIdentityProviderService` con NSubstitute:

1. Sin flag → no se llama al servicio, resultado sin password ni warning (adaptar tests existentes al nuevo tipo de resultado).
2. Con flag y `Created` → `TemporaryPassword` presente.
3. Con flag y `AlreadyExists` → warning correspondiente, sin password.
4. Con flag y `Unavailable` → Person creada igualmente + warning.
5. Los casos de negocio existentes (no Gestor, email duplicado) siguen pasando.

Verificar: `dotnet build src/` y `dotnet test` en verde.

> **Modelo recomendado:** `claude-sonnet-4.6` — integración nueva + cambio de contrato que toca varios archivos.

---

## Frontend

En el formulario de alta de persona (`src/frontend/src/app/features/persons/persons-list/persons-list.component.ts`):

1. Checkbox NG-ZORRO (`nz-checkbox`) en el formulario de creación (solo alta, no edición): «Crear credenciales locales (usuario/contraseña)» → campo `createLocalCredentials` en el POST a `/api/persons`.
2. La respuesta 201 ahora es `{ id: number; temporaryPassword?: string; credentialsWarning?: string }`:
   - Si `temporaryPassword` → modal NZ (`NzModalService`) con la contraseña en texto monoespaciado, aviso «Apúntala ahora: no se puede volver a consultar. El usuario deberá cambiarla en su primer inicio de sesión.» y botón para copiar al portapapeles (`navigator.clipboard`).
   - Si `credentialsWarning` → `nz-message` warning con el texto recibido.
3. Mantener el refresco del listado tras crear, como ya hace el componente.

Verificar compilación con `docker compose build frontend` (el Node local no compila Angular 21).

> **Modelo recomendado:** `claude-haiku-4.5` — cambios acotados a un componente siguiendo patrones existentes.
