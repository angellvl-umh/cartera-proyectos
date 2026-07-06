-- Base de datos propia para Keycloak (identity broker).
-- Este script solo se ejecuta cuando el volumen de PostgreSQL se inicializa
-- por primera vez (docker-entrypoint-initdb.d). Para un volumen pgdata ya
-- existente, ejecutar manualmente:
--   docker compose exec db psql -U postgres -c "CREATE DATABASE keycloak;"
CREATE DATABASE keycloak;
