// Plantilla para envsubst (ver docker-entrypoint.d/15-envsubst-env.sh).
// Genera env.js en el arranque del contenedor a partir de la variable
// KEYCLOAK_AUTHORITY del servicio `frontend` en docker-compose.yml.
window.__env = {
  keycloakAuthority: '${KEYCLOAK_AUTHORITY}',
};
