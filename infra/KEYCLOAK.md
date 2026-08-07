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

### Hostname dual (`KC_HOSTNAME` + backchannel dinámico)

El backend valida los JWT contra el discovery document por la red interna (`http://keycloak:8080`, `Auth__Authority`), pero el navegador necesita `authorization_endpoint` en la URL pública de Keycloak para el login OIDC del frontend. Por eso Keycloak fija:

```yaml
KC_HOSTNAME: ${KEYCLOAK_PUBLIC_URL:-http://localhost:8080}   # URLs frontend (navegador) e issuer
KC_HOSTNAME_BACKCHANNEL_DYNAMIC: "true"                       # token/jwks/userinfo según el host de la request
```

Con esto el `issuer` es estable (coincide con el `Auth__ValidIssuer` del backend) y los endpoints backchannel se resuelven en `keycloak:8080` para los contenedores. `KC_HOSTNAME` es un único valor fijo por instancia — no soporta "cualquier hostname válido" simultáneamente, así que cada entorno (local, Tailscale, producción) se levanta con su propio `.env`.

### Config por entorno

Dos variables en `.env` (ver `.env.example`) parametrizan todo el flujo de login para que el mismo stack funcione accedido por `localhost`, por un hostname de Tailscale, o por un dominio de producción, **sin rebuild de imágenes**:

| Variable | Qué controla | Default |
|----------|--------------|---------|
| `PUBLIC_URL` | Origen del **frontend** visto por el navegador → `redirectUris`/`webOrigins` del cliente `cartera-frontend` en el realm, y un origen extra de CORS del backend (`Cors__Origins__3`) | `http://localhost` |
| `KEYCLOAK_PUBLIC_URL` | Origen de **Keycloak** visto por el navegador → `KC_HOSTNAME`, `Auth__ValidIssuer` del backend, y el `authority` OIDC que resuelve el frontend en runtime | `http://localhost:8080` |

El frontend no lleva el `authority` hardcodeado: nginx genera `env.js` en el arranque del contenedor (`docker-entrypoint.d/15-envsubst-env.sh`, via `envsubst` sobre `public/env.template.js`) a partir de `KEYCLOAK_AUTHORITY` (calculada en `docker-compose.yml` desde `KEYCLOAK_PUBLIC_URL`), y `src/app/core/auth.config.ts` lo lee de `window.__env` en runtime — el mismo build sirve para los tres entornos.

**Aplicar un cambio de entorno:**
```bash
# .env
PUBLIC_URL=http://mipc.tailnet-xxxx.ts.net
KEYCLOAK_PUBLIC_URL=http://mipc.tailnet-xxxx.ts.net:8080

docker compose up -d --force-recreate keycloak backend frontend
```
`KC_HOSTNAME`, `Auth__ValidIssuer` y `env.js` se recalculan en cada arranque del contenedor — no requieren reimportar el realm.

**Excepción — `redirectUris`/`webOrigins` del cliente `cartera-frontend`:** esos valores vienen del **import** de `cartera-realm.json`, que (como se explica arriba) solo ocurre una vez. Si tu volumen `pgdata` ya tiene el realm importado, cambiar `PUBLIC_URL` en `.env` **no actualiza** la allowlist de Keycloak — hay que aplicarlo a mano una vez desde la consola admin (*Clients → cartera-frontend → Settings*: añadir `${PUBLIC_URL}/*` a Valid redirect URIs y `${PUBLIC_URL}` a Web origins) o vía Admin REST API. En un volumen nuevo (o el stack E2E), el import ya lo incluye automáticamente.

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
- Redirect URIs del cliente `cartera-frontend` y del OAuth client de Google con las URLs públicas reales.
- Backup de la BD `keycloak` junto con la de la app.
