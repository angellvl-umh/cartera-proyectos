# Datos de semilla — Cartera de Proyectos TIC

Script SQL idempotente con todos los datos de prueba del CPTI-2026: 10 promotores, 20 unidades orgánicas, 33 personas (incluye usuario gestor), 6 equipos, 85 proyectos con todos sus campos y asignaciones a equipos.

---

## Requisitos previos

- Docker Compose levantado (`docker compose up -d`) **o** PostgreSQL en `localhost:5432`
- `psql` disponible en PATH, o cualquier cliente SQL (DBeaver, pgAdmin)

---

## Cargar los datos de semilla

El script es **idempotente**: se puede ejecutar varias veces sin duplicar filas.

```bash
# Con Docker Compose (recomendado)
docker compose exec -T db psql -U postgres -d cartera_app < infra/seed.sql

# Con psql local (puerto 5432 mapeado al host)
psql -h localhost -U postgres -d cartera_app -f infra/seed.sql

# Con contraseña explícita
PGPASSWORD=postgres psql -h localhost -U postgres -d cartera_app -f infra/seed.sql
```

Al finalizar el script se muestra un resumen con los conteos:

```
personas | equipos | membresias | promotores | unidades | proyectos | asignaciones
      33 |       6 |         32 |         10 |       20 |        85 |            85
```

---

## Borrar todos los datos y recargar desde cero

### Paso 1 — Truncar todas las tablas (reinicia secuencias de IDs)

Ejecutar en psql, DBeaver o pgAdmin:

```sql
TRUNCATE TABLE
  "WorkItemEmbeddings", "Comments", "WorkItemAssignments", "WorkItems",
  "Sprints", "Epics", "ProjectNotes", "ProjectTags", "ProjectTeamAssignments",
  "PersonTeamMemberships", "Projects",
  "OrganicUnits", "Promoters", "Tags",
  "Teams", "Persons"
RESTART IDENTITY CASCADE;
```

Con Docker Compose en una sola línea:

```bash
docker compose exec -T db psql -U postgres -d cartera_app -c "
TRUNCATE TABLE
  \"WorkItemEmbeddings\", \"Comments\", \"WorkItemAssignments\", \"WorkItems\",
  \"Sprints\", \"Epics\", \"ProjectNotes\", \"ProjectTags\", \"ProjectTeamAssignments\",
  \"PersonTeamMemberships\", \"Projects\",
  \"OrganicUnits\", \"Promoters\", \"Tags\",
  \"Teams\", \"Persons\"
RESTART IDENTITY CASCADE;"
```

### Paso 2 — Recargar el seed

```bash
docker compose exec -T db psql -U postgres -d cartera_app < infra/seed.sql
```

---

## Notas

- **Usuario gestor**: el seed NO incluye la persona del gestor. La provisión ocurre automáticamente en el primer login: el backend crea la fila con el UUID real de Keycloak y le asigna rol `Gestor` porque `gestor@universidad.es` está en `Admin__InitialGestorEmails`. No es necesaria ninguna acción manual.
- **DataSeeder.cs**: el backend también carga estos mismos datos automáticamente al arrancar en modo `Development` (si la tabla `Projects` está vacía). El seed SQL es útil cuando se quiere cargar datos sin arrancar el backend, o para entornos de CI.
- **Enums almacenados como strings**: `Status`, `Complexity`, `SiptGroup` y `Role` se guardan como texto en PostgreSQL. Los valores válidos están documentados en `CLAUDE.md`.
