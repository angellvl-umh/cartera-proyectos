# Capacidad y Carga de Trabajo

## Descripción

Visualización de la carga de trabajo de personas y equipos para facilitar la toma de decisiones sobre asignación de nuevos proyectos.

## Historias de Usuario

### HU-CA-01: Ver carga de trabajo de un equipo

**Como** gestor de cartera,
**quiero** ver la carga de trabajo actual de cada equipo,
**para** decidir a qué equipo asignar un nuevo proyecto.

**Criterios de aceptación:**
- Se muestra por equipo: número de proyectos activos, tareas en curso, tareas pendientes
- Se muestra la carga por persona dentro del equipo
- Indicador visual según nivel de carga de tareas activas por persona: verde (≤3), amarillo (4–6), rojo (≥7)
- Se puede comparar equipos lado a lado

---

### HU-CA-02: Ver carga de trabajo de una persona

**Como** jefe de equipo,
**quiero** ver en qué está trabajando cada miembro de mi equipo,
**para** equilibrar la carga y detectar sobreasignación.

**Criterios de aceptación:**
- Se listan todas las tareas activas (In Progress, In Review) de la persona
- Se muestran las tareas pendientes (To Do) asignadas
- Se indica en qué proyectos está involucrada
- Se muestra en cuántos equipos participa

---

### HU-CA-03: Simular impacto de asignar un proyecto

**Como** gestor de cartera,
**quiero** ver el impacto que tendría asignar un nuevo proyecto a un equipo,
**para** tomar decisiones informadas sobre la planificación.

**Criterios de aceptación:**
- Al seleccionar un equipo candidato se muestra su carga actual
- Se indica si el equipo tiene capacidad disponible (basado en tareas activas por persona)
- Se sugieren equipos con mayor disponibilidad
- Se advierte si un equipo ya está en nivel de carga alto

---

### HU-CA-04: Dashboard resumen de capacidad

**Como** gestor de cartera,
**quiero** ver un panel resumen con la capacidad de todos los equipos,
**para** tener una foto global de la organización.

**Criterios de aceptación:**
- Vista tipo dashboard con todos los equipos
- Métricas por equipo: personas, tareas activas, proyectos asignados
- Gráfico de distribución de carga
- Filtro por período (tareas creadas/completadas en un rango de fechas)
