# Keycloak como Identity Broker

Keycloak 26 es el **identity broker** permanente de la plataforma: la app (frontend OIDC y backend JWT) solo habla con el realm `cartera`, y Keycloak federa las distintas formas de iniciar sesión desde su propia pantalla de login:

| Método | Estado | Cómo |
|--------|--------|------|
| Usuario/contraseña local | ✅ | Cuenta creada por el Gestor desde la app (Admin API, contraseña temporal) o desde la consola |
| Google | ✅ (requiere OAuth client propio) | Identity Provider `google` del realm |
| SSO universitario (SAML2) | 🔜 pendiente de metadata | Identity Provider SAML a añadir (ver abajo) |

**El gate de acceso es de la aplicación, no de Keycloak**: da igual con qué IdP se autentique alguien — si no existe como `Person` pre-registrada (por email) o está desactivada, `/api/me` devuelve 403 y el frontend muestra `/access-denied`. La vinculación `Person ↔ sub` se hace por email en el primer login.

---

## Persistencia en PostgreSQL

Keycloak usa la base de datos `keycloak` del PostgreSQL del stack (`KC_DB: postgres` en `docker-compose.yml`), de modo que IdPs, usuarios brokered y cuentas locales sobreviven a reinicios.

- **Volumen nuevo** (y stack E2E con tmpfs): la BD se crea sola via `infra/docker/initdb/01-keycloak-db.sql`.
- **Volumen `pgdata` ya existente**: el script de initdb no corre; crearla una única vez a mano:
  ```bash
  docker compose exec db psql -U postgres -c "CREATE DATABASE keycloak;"
  ```
- El realm se importa (`--import-realm`) **solo si no existe**. Cambios posteriores en `cartera-realm.json` no se aplican a un realm ya importado: o se replican desde la consola admin, o se borra la BD keycloak para forzar re-import (se pierden usuarios brokered/locales creados en runtime).

## Google IdP

1. En [Google Cloud Console](https://console.cloud.google.com/apis/credentials): **Create Credentials → OAuth client ID** (tipo *Web application*).
   - Authorized redirect URI: `http://localhost:8080/realms/cartera/broker/google/endpoint`
   - (En producción, la URL pública equivalente del Keycloak.)
2. Exportar antes de levantar el stack (o en `.env`):
   ```
   GOOGLE_CLIENT_ID=...apps.googleusercontent.com
   GOOGLE_CLIENT_SECRET=...
   ```
3. El import del realm sustituye `${GOOGLE_CLIENT_ID}`/`${GOOGLE_CLIENT_SECRET}`. Si el realm ya estaba importado, configurar el IdP desde la consola: *Identity Providers → Add provider → Google* con esos valores.

Sin variables configuradas el stack arranca igual: el botón de Google aparece pero el flujo falla hasta poner credenciales reales.

Comportamiento: `trustEmail: true` — el email verificado de Google se acepta sin re-verificación. Cualquier cuenta de Google podrá autenticarse en Keycloak, pero **solo entra en la app quien esté pre-registrado** con ese email. Si ya existe un usuario Keycloak con el mismo email (p. ej. cuenta local), el flujo "first broker login" ofrece vincular ambas identidades en un único usuario (mismo `sub`).

## SSO universitario (SAML2) — pendiente

Cuando la universidad facilite el metadata XML de su IdP SAML:

1. Consola admin → realm `cartera` → *Identity Providers → Add provider → SAML v2.0*.
2. Importar el metadata XML (o URL de metadata) del IdP universitario; alias sugerido: `universidad`.
3. Mappers de atributos: mapear el atributo de email del assertion (p. ej. `urn:oid:0.9.2342.19200300.100.1.3` / `mail`) al email del usuario, y nombre completo a `firstName`/`lastName`.
4. `trustEmail: true` y `syncMode: IMPORT`, igual que Google.
5. Facilitar al equipo de la universidad el SP metadata de Keycloak: `http://<keycloak>/realms/cartera/broker/universidad/endpoint/descriptor`.

El gate por pre-registro de la app aplica igual: no hay que tocar código.

## Cuentas locales desde la app (`cartera-admin`)

Al pre-registrar una Person, el Gestor puede marcar «Crear credenciales locales»: el backend usa el cliente confidencial `cartera-admin` (service account con rol `manage-users`) para crear el usuario en Keycloak con contraseña temporal (required action `UPDATE_PASSWORD`: cambio obligatorio en el primer login). La contraseña se muestra una única vez al Gestor.

- Secret del cliente: env `KC_ADMIN_CLIENT_SECRET` (default dev `cartera-admin-secret` — **cambiar en cualquier entorno no local**).
- Si Keycloak no responde, la Person se crea igualmente y se avisa al Gestor (la cuenta puede crearse después, o el usuario puede entrar con Google/SSO).

## Producción — checklist

- `start` (no `start-dev`), HTTPS (`KC_HOSTNAME`, certificados), `KC_BOOTSTRAP_ADMIN_*` con credenciales fuertes y rotadas.
- Secrets (`GOOGLE_CLIENT_SECRET`, `KC_ADMIN_CLIENT_SECRET`) via gestor de secretos, nunca defaults.
- Redirect URIs de los clientes y del OAuth client de Google con las URLs públicas reales.
- Backup de la BD `keycloak` junto con la de la app.
