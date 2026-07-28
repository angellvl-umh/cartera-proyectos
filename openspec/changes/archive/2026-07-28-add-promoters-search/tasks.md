## 1. Backend

- [x] 1.1 Añadir parámetro `Q` opcional a `GetPromotersQuery` (record en `src/CarteraProyectos.Core/Features/Promoters/GetPromoters.cs`)
- [x] 1.2 En `GetPromotersHandler`, filtrar por `Promoter.Name.Contains(request.Q)` cuando `Q` no sea null/vacío, igual que `GetOrganicUnitsHandler`
- [x] 1.3 Exponer el parámetro `q` (query string, opcional) en `GET /api/promoters` (`src/CarteraProyectos.Api/Endpoints/PromoterEndpoints.cs`)
- [x] 1.4 Test unitario: `GetPromotersHandler` filtra correctamente con `q` (coincidencia parcial, sin coincidencias, y sin `q` devuelve todo)

## 2. Frontend

- [x] 2.1 Actualizar `CatalogsService.getPromoters` (`src/frontend/src/app/features/admin/catalogs.service.ts`) para aceptar un `q?: string` opcional y añadirlo como query param solo si tiene valor
- [x] 2.2 Añadir caja de búsqueda (`nz-input` con icono de búsqueda) en `promoters-list.component.ts`, con debounce antes de recargar la tabla
- [x] 2.3 Al buscar, reiniciar la paginación a la página 1 y recargar la tabla con el texto introducido
- [x] 2.4 Al vaciar la caja de búsqueda, volver a mostrar el listado completo paginado

## 3. Tests

- [x] 3.1 Verificar (manual o E2E ligero) que el buscador de promotores filtra la tabla sin errores de consola y respeta la paginación al limpiar el filtro
