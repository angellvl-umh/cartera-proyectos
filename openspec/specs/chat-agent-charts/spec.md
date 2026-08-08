## Purpose

Tools del chat nativo que generan gráficos visuales (SVG) a partir de datos ya disponibles en otras tools de lectura (capacidad de equipos, progreso de proyectos, tareas del usuario por estado, proyectos por estado, proyectos por equipo). Los gráficos se almacenan de forma efímera y se devuelven como enlace de imagen — nunca como binario o base64 — para no consumir contexto del modelo.

## Requirements

### Requirement: Gráficos visuales generados desde el chat
El sistema SHALL exponer tools que generen un gráfico SVG a partir de datos ya disponibles en otras tools de lectura (capacidad de equipos, progreso de proyectos, tareas del usuario por estado, proyectos por estado, proyectos por equipo), y devuelvan un enlace de imagen en vez del binario o de datos base64, para no consumir contexto del modelo.

#### Scenario: Generar gráfico de capacidad por equipo
- **WHEN** el modelo invoca `chart_team_capacity`
- **THEN** el sistema genera un SVG de barras horizontales con las tareas activas por persona, coloreadas según su nivel de carga (verde/amarillo/rojo), lo almacena de forma efímera, y devuelve una URL de imagen

#### Scenario: Generar gráfico de progreso de proyectos sin proyectos visibles
- **WHEN** el modelo invoca `chart_project_progress` y el usuario no tiene proyectos asociados a sus equipos
- **THEN** el sistema devuelve un mensaje indicando que no hay proyectos disponibles, sin generar imagen

#### Scenario: Generar gráfico de tareas propias con tipo de gráfico explícito
- **WHEN** el modelo invoca `chart_my_tasks_by_status` con `chartType: "bar"`
- **THEN** el sistema genera un gráfico de barras (en vez del donut por defecto) con la distribución de tareas del usuario por estado

#### Scenario: Generar gráfico de proyectos por estado o por equipo
- **WHEN** el modelo invoca `chart_projects_by_status` o `chart_projects_by_team`
- **THEN** el sistema genera el gráfico correspondiente (tarta o barras según `chartType`, con valor por defecto) agrupando los proyectos visibles para el usuario

### Requirement: Los enlaces de imagen de los gráficos son temporales y no adivinables
El sistema SHALL almacenar cada gráfico generado bajo un identificador no adivinable con expiración por tiempo de inactividad, y SHALL servirlo con content-type `image/svg+xml` mediante un endpoint HTTP que no requiere la sesión autenticada del chat, devolviendo 404 una vez expirado.

#### Scenario: Ver un gráfico embebido en la conversación
- **WHEN** el navegador del usuario renderiza el mensaje del asistente que contiene el enlace de imagen del gráfico
- **THEN** el gráfico se muestra embebido en la conversación sin peticiones adicionales de autenticación

#### Scenario: Acceder a un gráfico tras expirar
- **WHEN** un usuario abre la URL de un gráfico después de que haya expirado por inactividad
- **THEN** el sistema devuelve 404
