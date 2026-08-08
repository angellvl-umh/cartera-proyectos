## Purpose

Tools del chat nativo que generan exportaciones a Excel (`.xlsx`) del listado de proyectos y del informe semanal de cartera. Los ficheros se almacenan de forma efímera y se devuelven como enlace de descarga — nunca como binario — para no consumir contexto del modelo.

## Requirements

### Requirement: Exportación del listado de proyectos a Excel desde el chat
El sistema SHALL exponer una tool `export_projects_excel` que genere un fichero `.xlsx` con el listado de proyectos visibles para el usuario (mismos filtros opcionales que `get_projects`: estado), con cabecera en negrita y columnas de ancho automático, y devuelva un enlace de descarga en vez del binario.

#### Scenario: Exportar proyectos sin resultados
- **WHEN** el modelo invoca `export_projects_excel` con un filtro de estado que no coincide con ningún proyecto
- **THEN** el sistema devuelve un mensaje indicando que no hay proyectos que exportar, sin generar fichero ni enlace

#### Scenario: Exportar proyectos con resultados
- **WHEN** el modelo invoca `export_projects_excel` y hay proyectos que cumplen el filtro
- **THEN** el sistema genera un `.xlsx` con una fila de cabecera en negrita y una fila por proyecto, lo almacena de forma efímera, y devuelve una URL de descarga junto con el nombre de fichero sugerido

### Requirement: Exportación del informe semanal de cartera a Excel desde el chat
El sistema SHALL exponer una tool `export_weekly_portfolio_report_excel` que genere un `.xlsx` del informe semanal de cartera (mismos filtros opcionales que `get_weekly_portfolio_report`: año, equipo), marcando en una columna separada los proyectos en riesgo, con los proyectos en riesgo listados antes que el resto.

#### Scenario: Exportar el informe semanal con proyectos en riesgo
- **WHEN** el modelo invoca `export_weekly_portfolio_report_excel` y existen proyectos en riesgo esta semana
- **THEN** el sistema genera el `.xlsx` con los proyectos en riesgo en las primeras filas y una columna "En riesgo" marcada, y devuelve la URL de descarga

### Requirement: Los enlaces de descarga de exports son temporales y no adivinables
El sistema SHALL almacenar cada fichero exportado bajo un identificador no adivinable (tipo GUID) con expiración por tiempo de inactividad, y SHALL servirlo mediante un endpoint HTTP que no requiere la sesión autenticada del chat (capability URL), devolviendo 404 una vez expirado.

#### Scenario: Descargar un export dentro del periodo de validez
- **WHEN** un usuario abre la URL de descarga de un export minutos después de generarlo
- **THEN** el sistema devuelve el fichero `.xlsx` con el content-type y nombre de fichero correctos

#### Scenario: Descargar un export tras expirar
- **WHEN** un usuario abre la URL de descarga de un export después de que haya expirado por inactividad
- **THEN** el sistema devuelve 404
