# Informes y Seguimiento

## Descripción

Generación de informes exportables sobre el estado de la cartera, avance de proyectos y actividad de los equipos.

## Historias de Usuario

### HU-IN-01: Generar informe de estado de cartera

**Como** gestor de cartera,
**quiero** generar un informe con el estado de todos los proyectos de la cartera de un año,
**para** reportar el avance a la dirección.

**Criterios de aceptación:**
- Se selecciona el año de cartera
- El informe incluye: lista de proyectos con estado, % avance (tareas Done / total tareas × 100), equipo asignado
- Resumen: proyectos por estado (gráfico), proyectos en riesgo
- Exportable a PDF y Excel

---

### HU-IN-02: Generar informe de avance de un proyecto

**Como** jefe de equipo,
**quiero** generar un informe de avance de un proyecto específico,
**para** documentar el progreso y compartirlo con la unidad solicitante.

**Criterios de aceptación:**
- Incluye: épicas con % completado (tareas Done / total tareas de la épica × 100), tareas completadas vs pendientes
- Historial de actividad reciente (últimos cambios de estado, comentarios)
- Hitos: tareas marcadas con `IsHito = true`. Se muestran agrupadas en "alcanzados" (Done) y "próximos" (no Done), ordenados por `HitoDate`
- Exportable a PDF

---

### HU-IN-03: Ver actividad reciente

**Como** gestor de cartera,
**quiero** ver un feed de actividad reciente en la plataforma,
**para** estar al día de lo que está pasando sin tener que revisar proyecto por proyecto.

**Criterios de aceptación:**
- Se muestra: cambios de estado, nuevas tareas, tareas completadas, comentarios
- Filtrable por proyecto, equipo o persona
- Orden cronológico inverso (más reciente primero)
- Paginado (20 ítems por página)

---

### HU-IN-04: Informe de productividad de equipo

**Como** gestor de cartera,
**quiero** ver métricas de productividad por equipo en un período,
**para** identificar tendencias y posibles problemas.

**Criterios de aceptación:**
- Métricas: tareas completadas, tiempo medio en cada estado, tareas creadas vs completadas
- Selección de rango de fechas
- Comparativa entre equipos
- Visualización con gráficos
