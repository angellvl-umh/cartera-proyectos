## Why

El menú lateral (`app.component.ts`) es fijo (252px, siempre visible) y no se adapta a pantallas pequeñas ni se puede ocultar manualmente para ganar espacio de trabajo. Ya existe un signal `collapsed` a medio construir (solo oculta el subtítulo de la cabecera del menú) y los iconos `menu-fold`/`menu-unfold` ya están registrados en `app.config.ts` sin usarse — la intención de esta feature ya estaba prevista pero nunca se terminó de conectar.

## What Changes

- Botón de plegado manual (icono `menu-fold`/`menu-unfold`) en la cabecera, visible siempre, que alterna el menú entre expandido (252px, con etiquetas de texto) y plegado (72px, solo iconos con tooltip).
- Colapso automático responsive: por debajo de un ancho de viewport de escritorio pequeño/tablet (breakpoint `md`, se usará `BreakpointObserver` de Angular CDK, ya instalado), el menú se pliega automáticamente a modo icono, igual que el plegado manual; el usuario puede seguir alternando manualmente por encima de ese comportamiento automático.
- El estado (expandido/plegado) se recuerda en `localStorage` para que no salte al valor por defecto en cada recarga de página.

**Decisión de alcance:** se implementa como colapso a "riel de iconos" (72px), no como un drawer/overlay a pantalla completa para móvil. Esta app es una herramienta de gestión de uso interno, de escritorio/tablet — un riel de iconos responde igual de bien a "reducir la pantalla" (achicar la ventana del navegador, portátil pequeño, tablet) sin la complejidad añadida de un overlay con backdrop. Si el uso real en pantallas de móvil resulta insuficiente con este enfoque, se puede revisar en un change posterior.

## Capabilities

### New Capabilities
- `sidebar-collapse`: menú lateral plegable manualmente y de forma responsive, con persistencia del estado.

## Impact

- `src/frontend/src/app/app.component.ts` (único fichero de código a tocar; layout, signal `collapsed`, nuevo control de `localStorage` y `BreakpointObserver`)
