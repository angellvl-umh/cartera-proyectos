## Why

Hoy el agente IA vive en Open WebUI: un servicio Python aparte con su propio cliente OIDC contra Keycloak, su propia base de datos, y un "Tool Server" HTTP (`/api/agent/*`) que Open WebUI invoca pasando la identidad del usuario por la cabecera `X-Open-WebUI-User-Email`. Eso añade un servicio más que operar, un segundo cliente SSO que mantener sincronizado (`infra/KEYCLOAK.md`), y una superficie de autorización débil: cualquier llamante que sepa el email de un usuario y la API key del Tool Server puede suplantarlo, porque los permisos no pasan por el pipeline de autorización real de la API. Moviendo el chat y las tools al propio backend, el modelo solo puede invocar lo que el usuario autenticado de la petición HTTP puede hacer, con el mismo pipeline MediatR/autorización que ya usa el resto de la aplicación — y se elimina un servicio, un cliente OIDC y una sincronización manual de definiciones de tools en Python.

## What Changes

- Nueva capacidad de chat con IA integrada en la app: conversaciones con título por usuario, mensajes persistidos en la base de datos de la propia aplicación (Postgres vía EF Core), sin componente externo de almacenamiento.
- Backend: servicio de chat sobre el SDK oficial `OpenAI` para .NET, apuntando al endpoint OpenAI-compatible de LiteLLM (que se mantiene sin cambios como proxy hacia AWS Bedrock). El modelo a usar es un parámetro de configuración de la aplicación (appsettings/env), no seleccionable por el usuario.
- Bucle de tool-calling en el backend: las funciones que el modelo puede invocar delegan directamente en los mismos casos de uso MediatR (`ISender`) que hoy exponen los endpoints `/api/agent/*`, ejecutándose con la identidad ya autenticada de la petición — sin cabeceras de suplantación ni servicio Python intermedio.
- Auditoría de tool calls reutilizando/adaptando `AgentActionLog` + `AgentAuditBehavior`.
- Frontend: panel lateral (drawer) global accesible desde el header, disponible en cualquier pantalla de la app; lista de conversaciones, crear conversación nueva, enviar mensaje, ver respuesta (sin streaming en esta primera versión).
- **BREAKING**: se elimina el Tool Server HTTP (`/api/agent/*`) — su única consumidora era Open WebUI, que desaparece en este mismo change. Cualquier integración externa que dependiera de esos endpoints (no se conoce ninguna) dejaría de funcionar.
- **BREAKING**: se retira el servicio `open-webui` de `docker-compose.yml` y su cliente Keycloak (`cartera-openwebui`) y documentación asociada en `infra/KEYCLOAK.md`. `litellm` se mantiene sin cambios.

## Capabilities

### New Capabilities
- `ai-chat`: chat con IA integrado en la aplicación — conversaciones persistidas por usuario, envío de mensajes, bucle de tool-calling contra los casos de uso de dominio existentes (tareas, proyectos, capacidad, riesgos, dependencias, etc.), auditoría de las acciones del agente, y panel de chat en el frontend Angular.

### Modified Capabilities
- Ninguna capacidad existente trackeada en `openspec/specs/` cambia sus requisitos. El Tool Server (`/api/agent/*`) y la integración con Open WebUI se implementaron antes de adoptar OpenSpec en este repo y no tienen spec propia — su retirada se documenta como parte del Impact de este change, no como una capability modificada.

## Impact

- **Backend**: nuevo paquete NuGet `OpenAI`; nuevas entidades `Conversation`/`ChatMessage` (+ migración EF Core); nuevo `ChatService`/handler con el bucle de tool-calling; nuevo endpoint group `/api/chat/*`; eliminación del endpoint group `/api/agent/*` y de las clases wrapper `IAgentAuditable` que solo existían para ese Tool Server (se reemplazan por invocación directa de los mismos `IRequest`/`IRequestHandler` desde las nuevas tool definitions).
- **Frontend**: nuevo componente de panel de chat (drawer) + servicio HTTP, integrado en el layout principal (junto al sidebar/header ya existente).
- **Infra**: `docker-compose.yml` pierde el servicio `open-webui`; `infra/KEYCLOAK.md` pierde la sección del cliente `cartera-openwebui`; `litellm` no cambia.
- **Tests**: nuevos tests unitarios del bucle de tool-calling y de los endpoints de chat; los tests E2E de Playwright no cubrían Open WebUI (vive fuera del frontend Angular), así que no hay regresión ahí salvo añadir cobertura del nuevo panel si procede.
