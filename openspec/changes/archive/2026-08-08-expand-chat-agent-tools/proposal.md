## Why

El chat nativo del agente IA (`ChatToolCatalog`) cubre hoy tareas, proyectos, capacidad, personas, riesgos y dependencias, pero se quedó corto frente a dos cosas: (1) capacidades que ya existen en el resto de la plataforma (sprints, épicas, equipos, backlog, roadmap, forecast de capacidad, métricas ágiles, catálogos) y que el agente no puede consultar ni operar, y (2) la exportación a Excel y la generación de gráficos que sí existían en la integración anterior con Open WebUI (`infra/open-webui/cartera_tool.py`) y se perdieron al migrar al chat nativo (commit `10badb5`) porque esa integración usaba Python (openpyxl/matplotlib), inviable de mantener dentro del backend .NET. Sin esto, gestores y jefes de equipo tienen que salir del chat a la UI para tareas que antes resolvían con una frase.

## What Changes

- Añadir tools de solo lectura y de escritura para: Sprints (listar, crear, activar, completar con carry-over, burndown), Epics (listar, crear, actualizar), Teams (listar, actividad por equipo), Backlog (reordenar prioridad, asignación masiva a sprint), Roadmap de cartera, Forecast de capacidad, Velocity y Cycle Time de proyecto, asignación de equipo a proyecto, y catálogos (promotores, unidades orgánicas, tags).
- Todas las tools de escritura reutilizan los mismos comandos/queries que ya usan los endpoints REST equivalentes (o crean el `Agent*Command`/`Agent*Query` que falte siguiendo el patrón de `Core/Features/Agent/*`), respetan las mismas reglas de autorización, y exigen confirmación previa del usuario (regla ya vigente en `ChatSystemPrompt`).
- Añadir `export_projects_excel` y `export_weekly_portfolio_report_excel`: generan un `.xlsx` en el backend (nueva dependencia `ClosedXML`) y devuelven un enlace de descarga corto en vez de el binario, para no inflar el contexto del modelo.
- Añadir 5 tools de gráficos (`chart_team_capacity`, `chart_project_progress`, `chart_my_tasks_by_status`, `chart_projects_by_status`, `chart_projects_by_team`): generan un SVG construido a mano en el backend (sin librería de gráficos, mismo criterio que ya sigue el frontend) y devuelven un enlace de imagen markdown.
- Nuevo almacén temporal de blobs (exports + charts) basado en `IMemoryCache` con expiración deslizante, servido por endpoints públicos con ID no adivinable — sustituye conceptualmente al `AgentBlobStore` in-memory sin TTL de la integración anterior (ya eliminado).
- Actualizar `ChatSystemPrompt.Base` para reflejar las capacidades nuevas.

## Capabilities

### New Capabilities
- `chat-agent-domain-tools`: tools de lectura/escritura sobre sprints, épicas, equipos, backlog, roadmap, forecast de capacidad, métricas ágiles (velocity/cycle time), asignación de equipo a proyecto y catálogos, expuestas al agente de chat.
- `chat-agent-exports`: exportación a Excel descargable (listado de proyectos, informe semanal de cartera) desde una tool del chat.
- `chat-agent-charts`: generación de gráficos SVG descargables/embebibles (capacidad de equipos, progreso de proyectos, tareas por estado, proyectos por estado/equipo) desde una tool del chat.

### Modified Capabilities
(ninguna — no cambia el comportamiento de conversaciones, envío de mensajes, tool-calling loop ni auditoría ya definido en `ai-chat`; solo se añaden tools nuevas al catálogo)

## Impact

- **Backend (Core)**: nuevas particiones de `Core/Features/Chat/Tools/ChatToolCatalog.*.cs`; nuevos `Agent*Command`/`Agent*Query` en `Core/Features/Agent/*` donde no exista ya un handler reutilizable; nuevo servicio de blob store temporal (interfaz en Core, implementación en Infrastructure).
- **Backend (Api)**: nuevos endpoints públicos (fuera del pipeline de auth JWT) para servir charts y exports por ID efímero.
- **Backend (Infrastructure)**: nueva dependencia NuGet `ClosedXML`; registro de `IMemoryCache` si no está ya registrado; implementación del blob store.
- **Frontend**: sin cambios previstos salvo que el saneado de markdown de `chat-panel.component.ts` bloquee `<img src="...">` (a verificar durante la implementación).
- **Dependencias**: añade `ClosedXML` (MIT) al proyecto Infrastructure. No añade librería de gráficos.
- **Tests**: nuevos tests unitarios por cada tool/handler nuevo siguiendo el patrón ya usado en `tests/CarteraProyectos.UnitTests/Features/Chat/`.
