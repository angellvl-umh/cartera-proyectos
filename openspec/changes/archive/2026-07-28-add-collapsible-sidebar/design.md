## Context

`app.component.ts` ya tiene un signal `collapsed = signal(false)` (línea 154) pero solo se usa para ocultar el subtítulo "Gestión de proyectos" bajo el logo (línea 41) — el ancho del `<aside>` es fijo (`width:252px` inline, línea 38) y no hay ningún control que cambie `collapsed`. Los iconos `MenuFoldOutline`/`MenuUnfoldOutline` ya están registrados en `app.config.ts` sin usarse en ningún componente. El layout es un flex manual (no usa `nz-layout`/`nz-sider` de NG-ZORRO), por lo que la implementación se hace directamente sobre ese `<aside>`/`<div>` existente.

## Goals / Non-Goals

**Goals:**
- Botón visible en la cabecera para plegar/desplegar el menú manualmente en cualquier momento.
- Colapso automático cuando el viewport se reduce por debajo de un breakpoint razonable (tablet/portátil pequeño).
- El estado plegado/expandido persiste entre recargas (`localStorage`).
- Los ítems de navegación siguen siendo utilizables en modo plegado (iconos con tooltip, sin perder el destino del enlace).

**Non-Goals:**
- No se implementa un drawer/overlay de pantalla completa para móvil en este change (ver "Decisión de alcance" en el proposal).
- No se toca el resto de componentes de la app: el layout ya usa flexbox donde el contenido principal (`flex:1 1 auto`) se ajusta automáticamente al ancho que le deje el `<aside>`, así que no hace falta ningún cambio en otras pantallas.
- No se añade un menú de "favoritos"/reordenación ni se cambia la estructura de navegación en sí (misma lista de enlaces, mismo agrupado Principal/Administración).

## Decisions

- **Riel de iconos (72px) en vez de drawer/overlay para móvil** — ver proposal. Alternativa descartada: ocultar el `<aside>` por completo en pantallas estrechas y mostrarlo como overlay con backdrop al pulsar un botón; se descarta por complejidad añadida (gestión de backdrop, click-outside-to-close, animación de entrada/salida, z-index) desproporcionada para una herramienta interna de escritorio/tablet donde el caso de uso principal de "reducir la pantalla" es achicar la ventana o un portátil pequeño, no un teléfono.
- **`BreakpointObserver` de Angular CDK** (ya es dependencia del proyecto, usado hoy solo por `DragDropModule`) para detectar el breakpoint responsive, en vez de un listener manual de `window.resize` + `matchMedia` a mano — es la forma idiomática en Angular, ya disponible sin añadir dependencias nuevas, y gestiona correctamente el ciclo de vida de la suscripción.
- **Breakpoint elegido: `992px`** (equivalente aproximado al breakpoint `lg` de Bootstrap/most admin templates — por debajo de un portátil pequeño/tablet en horizontal). Se implementa con `breakpointObserver.observe('(max-width: 992px)')`.
- **Interacción entre colapso automático y manual:** el cruce del breakpoint fuerza `collapsed` al valor correspondiente (plegado por debajo, expandido por encima) SOLO en el momento en que se cruza el breakpoint; una vez por debajo del breakpoint, el usuario puede seguir pulsando el botón manual para expandir temporalmente el menú (queda expandido hasta que vuelva a cruzar el breakpoint o lo pliegue de nuevo). Esto evita que el toggle manual quede "bloqueado" en pantallas pequeñas.
- **Persistencia con `localStorage`** bajo una clave simple (p. ej. `sidebar-collapsed`), leída al inicializar el componente y escrita en cada cambio de `collapsed`. Si no hay valor guardado, el estado inicial lo decide el breakpoint actual (plegado si ya se arranca en una ventana estrecha).
- **Modo plegado = solo iconos con tooltip** (`nz-tooltip` de NG-ZORRO, ya usado en otras partes de la app) en cada `sidebar-nav-item`, para no perder la identificación de cada enlace cuando el texto se oculta.

## Risks / Trade-offs

- [Riesgo] Con `nzMode="multiple"`-style riel de iconos en vez de overlay, en un teléfono real (viewport <400px) 72px de riel fijo sigue restando espacio útil de forma notable. → Mitigación: aceptado conscientemente (ver Non-Goals); revisar con un change posterior si el uso móvil real lo justifica.
- [Riesgo] Persistir en `localStorage` un estado que luego se sobrescribe por el breakpoint podría sorprender al usuario (abre en plegado porque la última vez la ventana era pequeña, aunque ahora la tenga grande). → Mitigación: el criterio ya definido arriba (el breakpoint solo fuerza el estado al cruzarse, no en cada carga) evita este caso — en cada carga se respeta el valor guardado si el breakpoint actual no lo contradice.
