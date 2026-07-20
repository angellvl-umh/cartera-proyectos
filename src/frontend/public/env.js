// Config runtime del frontend. En Docker, el entrypoint de nginx regenera
// este fichero a partir de env.template.js con envsubst (ver
// docker-entrypoint.d/15-envsubst-env.sh). En `ng serve` (fuera de Docker)
// se sirve tal cual, con el valor por defecto de desarrollo local.
window.__env = {
  keycloakAuthority: 'http://localhost:8080/realms/cartera',
};
