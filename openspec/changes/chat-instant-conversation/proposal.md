## Why

Hoy, para escribir el primer mensaje del chat IA hace falta pulsar antes el botón "Nueva conversación": el área de composición (textarea + enviar) ni siquiera se renderiza si no hay una conversación activa seleccionada (`chat-panel.component.ts`, guard `@if (activeConvId() === null)`). Además, pulsar ese botón crea de inmediato una conversación vacía en el backend (`POST /api/chat/conversations`), así que si el usuario cierra el panel sin llegar a escribir, queda una conversación fantasma con 0 mensajes para siempre en su historial. El usuario pide un comportamiento como el de ChatGPT/Claude: el cuadro de texto está siempre listo para escribir, y "empezar una conversación nueva" es simplemente escribir y enviar — sin ningún botón de por medio.

## What Changes

- El composer (textarea + botón enviar) del panel de chat pasa a estar siempre visible y habilitado, haya o no una conversación activa seleccionada.
- Enviar el primer mensaje sin conversación activa crea la conversación de forma transparente en el mismo paso: el frontend genera un título a partir del propio texto del mensaje (truncado) y encadena `POST /api/chat/conversations` → `POST /api/chat/conversations/{id}/messages`, sin acción previa del usuario.
- El botón "Nueva conversación" deja de crear una conversación vacía al pulsarlo: pasa a limpiar solo la vista local (vuelve al estado "sin conversación seleccionada", listo para escribir) sin llamar al backend. Elimina el efecto secundario de conversaciones huérfanas con 0 mensajes.
- Abrir el panel de chat ya deja el composer listo para escribir sin ninguna acción adicional (hoy, al abrir sin conversación activa, se muestra un `nz-empty` en lugar del input).
- Se corrige una condición de carrera latente: si el usuario cambia de conversación (o abre un borrador nuevo) mientras una respuesta anterior sigue en curso, esa respuesta tardía ya no debe sobrescribir la vista de la conversación que se está viendo ahora.

**Sin cambios de contrato en el backend** — se reutilizan tal cual los endpoints `POST /api/chat/conversations` y `POST /api/chat/conversations/{id}/messages` ya existentes; el cambio es puramente de orquestación en el frontend.

## Capabilities

### New Capabilities
- `chat-conversation-flow`: cómo se inicia una conversación de chat desde el panel del frontend — composer siempre disponible, creación implícita de la conversación al primer envío, sin conversaciones vacías huérfanas.

### Modified Capabilities
(ninguna — no se modifica el contrato de los endpoints de `/api/chat/*`, solo la orquestación del frontend sobre ellos)

## Impact

- **Frontend**: `src/frontend/src/app/features/chat/chat-panel.component.ts` (único fichero de producción tocado; sin cambios en `chat.service.ts` ni en el backend).
- **Backend**: ninguno.
- **Tests**: nuevos tests Vitest en `chat-panel.component.spec.ts` (o fichero nuevo si se prefiere separar) para el nuevo flujo de creación implícita y la generación de título; los tests existentes de renderizado de markdown no se ven afectados.
