## Context

`ChatPanelComponent` (`src/frontend/src/app/features/chat/chat-panel.component.ts`) gestiona todo el estado del panel con signals locales (`conversations`, `activeConvId`, `rawMessages`, `loadingConvs`, `loadingMsgs`, `sending`), sin servicio de estado propio — `ChatService` es un cliente HTTP puro. Hoy:

- El template solo renderiza `.input-bar` dentro del `@else` de `@if (activeConvId() === null)` (líneas ~254-309): sin conversación activa no hay dónde escribir.
- `startNewConversation()` (líneas 415-432) llama a `chatService.createConversation(title)` de inmediato, con un título por defecto `Chat DD/MM/YYYY HH:MM`, y selecciona la conversación creada.
- `sendMessage()` (líneas 449-499) retorna sin hacer nada si `activeConvId() === null` (línea 453) — nunca se ha podido invocar en la práctica porque el composer no existe en ese estado.
- El backend (`SendChatMessageCommand`, `CreateConversationCommand`) no cambia: sigue exigiendo `ConversationId > 0` para enviar un mensaje, sin fallback de creación automática.

## Goals / Non-Goals

**Goals:**
- El composer está siempre visible y utilizable, exista o no conversación activa.
- Iniciar una conversación nueva es una consecuencia de enviar el primer mensaje, no un paso previo explícito.
- El botón "Nueva conversación" sigue existiendo (para abandonar la vista de una conversación existente y volver a un borrador en blanco) pero deja de tener efecto en el backend por sí solo.
- Ninguna conversación vacía se persiste nunca (ni por abrir el panel, ni por pulsar "Nueva conversación", ni por cerrar el panel a medio escribir).

**Non-Goals:**
- No se cambia el contrato de los endpoints `/api/chat/*` ni se añade un modo "crear conversación con el primer mensaje" en el backend — la creación sigue siendo dos llamadas HTTP encadenadas desde el frontend, igual que hoy hace `startNewConversation()`.
- No se implementa título generado por IA/resumen del mensaje (como hace ChatGPT de forma asíncrona) — el título se deriva del propio texto del primer mensaje, truncado en cliente. Queda como posible mejora futura, no bloquea esta feature.
- No se persiste el texto de un borrador no enviado entre aperturas/cierres del panel más allá de lo que ya ofrece gratis mantener el componente vivo mientras la app no se recarga (no se añade `localStorage` ni servicio de borradores).

## Decisions

**El "estado sin conversación" (`activeConvId() === null`) pasa a significar "borrador listo para escribir", no "nada que hacer".**
Se elimina el guard `@if (activeConvId() === null) { <empty> } @else { <mensajes + input> }` y se sustituye por: el `.input-bar` se renderiza siempre; el área de mensajes muestra el `nz-empty` "Sin mensajes todavía" (reutilizando el que ya existe para conversaciones vacías) cuando `activeConvId() === null || visibleMessages().length === 0`, y la lista de mensajes normal en cualquier otro caso. Así no hace falta un tercer estado visual nuevo: el borrador se ve exactamente como una conversación recién creada sin mensajes.

**`sendMessage()` crea la conversación de forma perezosa, en el propio flujo de envío, no antes.**
```
sendMessage():
  text = inputText.trim(); si vacío o sending() → return
  convId = activeConvId()
  si convId === null:
    sending.set(true)
    título = deriveTitle(text)   // ver más abajo
    chatService.createConversation(título).subscribe({
      next: ({id}) => { activeConvId.set(id); loadConversations(); continueSend(id, text) }
      error: → sending.set(false); message.error(...); no se pierde el texto escrito
    })
  si no:
    continueSend(convId, text)   // comportamiento actual, sin cambios
```
`continueSend(convId, text)` es el cuerpo actual de `sendMessage()` a partir de la línea 455 (mensaje optimista, `chatService.sendMessage`, recarga de mensajes). Se extrae tal cual a un método privado para no duplicar lógica entre la rama "crear primero" y la rama "conversación ya existente" — no es una abstracción nueva de más, es el mismo código que hoy vive todo junto en `sendMessage()`, partido en dos porque ahora hay dos puntos de entrada.

**Título derivado del texto del mensaje: primera línea, recortada a 60 caracteres.**
`deriveTitle(text)`: toma `text.split('\n')[0].trim()`, y si supera 60 caracteres lo corta a 60 y añade `…`. Si tras eso queda vacío (no debería ocurrir: `sendMessage` ya exige `text.trim()` no vacío antes de llegar aquí), no hace falta contemplar el caso. Sin llamada a IA ni al backend para resumir — determinista y sin coste de red adicional. 60 caracteres es el mismo orden de magnitud que el ancho de la columna de 220px donde se trunca visualmente con ellipsis CSS (`.conv-title`), así que no hace falta afinar más.

**`startNewConversation()` deja de llamar al backend: solo resetea estado local.**
Nuevo cuerpo: `activeConvId.set(null); rawMessages.set([]); inputText = ''; sending.set(false)`. Se limpia también `inputText` — si el usuario estaba a media frase en la conversación anterior, ese texto no tiene sentido en el borrador nuevo (evita enviar por error texto pensado para otra conversación). Ya no hace falta el `subscribe` a `createConversation` ni la recarga de `listConversations()` en este método (no ha cambiado nada en el backend que recargar).

**Guardas de conversación activa en las respuestas asíncronas de envío, para evitar que una respuesta tardía pise la vista actual.**
`continueSend(convId, text)` captura `convId` por closure (como ya hace hoy), pero las dos ramas del `subscribe` (`next`/`error`) comprueban `this.activeConvId() === convId` antes de tocar `rawMessages`, `sending` o el mensaje optimista:
- Si el usuario ya cambió de conversación o abrió un borrador nuevo cuando llega la respuesta, se actualiza igualmente el contador/`updatedAt` de esa conversación en la lista lateral (para que el historial quede correcto), pero **no** se toca `rawMessages` ni `sending` de la vista actual, que pertenece a otra conversación.
- `sending.set(true)` al empezar a enviar sigue siendo inmediato (es la propia acción del usuario, siempre sobre la vista visible en ese instante); el guard aplica solo a los efectos que llegan *después*, de forma asíncrona.
Esta condición de carrera existe también hoy en teoría (nada impide clicar otra conversación en la sidebar mientras se envía), pero se vuelve mucho más alcanzable con el nuevo flujo: antes, escribir y enviar era la única acción posible dentro de una conversación abierta; ahora, con el composer siempre activo, es natural abrir un borrador nuevo o seleccionar otra conversación mientras la anterior todavía está "escribiendo".

## Risks / Trade-offs

- [Riesgo] Con el composer siempre visible, un usuario podría enviar mensajes muy seguidos a borradores distintos generando varias conversaciones nuevas sin darse cuenta → Mitigación: ninguna adicional; es el mismo riesgo que existe hoy en ChatGPT/Claude y no se considera un problema — cada envío exitoso sí corresponde a una intención real de escribir.
- [Riesgo] `deriveTitle` puede producir títulos poco descriptivos para mensajes que empiezan igual (p. ej. "Hola" repetido) → Mitigación: ninguna, es exactamente lo mismo que le pasaría a un título manual; fuera de alcance mejorar con resumen por IA.

## Migration Plan

Puramente aditivo/reordenación de lógica ya existente en un único componente de frontend, sin cambios de API, sin migración de datos, sin flag de compatibilidad — no hay conversaciones "a medio crear" que migrar. Rollback trivial: revertir el commit.
