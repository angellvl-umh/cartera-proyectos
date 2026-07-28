# sidebar-collapse

## Purpose

Menú lateral plegable manualmente y de forma responsive, con persistencia del estado, para ganar espacio de trabajo en pantallas pequeñas o a demanda del usuario.

## Requirements

### Requirement: Plegado manual del menú lateral
El sistema SHALL exponer un botón siempre visible en la cabecera que alterna el menú lateral entre expandido (con etiquetas de texto) y plegado (solo iconos).

#### Scenario: El usuario pliega el menú
- **WHEN** el usuario pulsa el botón de plegar con el menú expandido
- **THEN** el menú pasa a mostrar solo iconos (sin etiquetas de texto) y el icono del botón cambia a "desplegar"

#### Scenario: El usuario despliega el menú
- **WHEN** el usuario pulsa el botón de desplegar con el menú plegado
- **THEN** el menú vuelve a mostrar las etiquetas de texto junto a los iconos

#### Scenario: Navegación disponible en modo plegado
- **WHEN** el menú está plegado
- **THEN** cada ítem de navegación sigue siendo un enlace funcional a su ruta, mostrando su etiqueta como tooltip al pasar el cursor

### Requirement: Colapso automático responsive
El sistema SHALL plegar automáticamente el menú lateral cuando el ancho del viewport cruza hacia abajo de 992px, y desplegarlo automáticamente al volver a cruzar hacia arriba de ese umbral, sin impedir que el usuario lo alterne manualmente en cualquier momento.

#### Scenario: Reducir la ventana del navegador
- **WHEN** el ancho del viewport pasa de ser mayor a 992px a ser menor o igual
- **THEN** el menú lateral se pliega automáticamente

#### Scenario: Ampliar la ventana del navegador
- **WHEN** el ancho del viewport pasa de ser menor o igual a 992px a ser mayor
- **THEN** el menú lateral se despliega automáticamente

#### Scenario: Alternar manualmente en pantalla estrecha
- **WHEN** el viewport está por debajo de 992px (menú plegado automáticamente) y el usuario pulsa el botón de desplegar
- **THEN** el menú se despliega y permanece así hasta que el usuario lo pliegue de nuevo o el viewport vuelva a cruzar el umbral de 992px

### Requirement: Persistencia del estado plegado/expandido
El sistema SHALL recordar el último estado (plegado o expandido) del menú lateral entre recargas de página, salvo cuando el breakpoint responsive lo sobrescriba al cruzarse.

#### Scenario: Recargar la página tras plegar manualmente
- **WHEN** el usuario pliega el menú y recarga la página sin cambiar el ancho del viewport
- **THEN** el menú se muestra plegado tras la recarga
