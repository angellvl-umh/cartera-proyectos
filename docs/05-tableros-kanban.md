# Tableros Kanban

## Descripción

Visualización del trabajo en curso mediante tableros Kanban interactivos. Se ofrecen diferentes vistas: por proyecto, por persona, y por equipo.

## Historias de Usuario

### HU-KB-01: Ver Kanban de un proyecto

**Como** jefe de equipo,
**quiero** ver un tablero Kanban con todas las tareas de un proyecto organizadas por estado,
**para** visualizar de un vistazo el progreso del trabajo.

**Criterios de aceptación:**
- Columnas: Backlog, To Do, In Progress, In Review, Done
- Cada tarjeta muestra: título, asignado, prioridad, épica
- Se puede filtrar por épica o por persona asignada
- Se puede ocultar/mostrar la columna "Done" para reducir ruido

---

### HU-KB-02: Mover tareas en el Kanban arrastrando

**Como** desarrollador,
**quiero** arrastrar una tarjeta de una columna a otra en el Kanban,
**para** cambiar el estado de la tarea de forma rápida e intuitiva.

**Criterios de aceptación:**
- Drag & drop entre columnas actualiza el estado de la tarea
- El cambio se persiste inmediatamente en el backend
- Se muestra feedback visual durante el arrastre
- Solo se permite arrastrar si el usuario tiene permisos sobre esa tarea:
  - Desarrollador: puede arrastrar solo sus propias tareas asignadas
  - Jefe de equipo: puede arrastrar cualquier tarea de los equipos de su proyecto
  - Gestor de cartera: puede arrastrar cualquier tarea
- Todos los usuarios pueden **ver** el tablero completo del proyecto (incluidas tarjetas de otros)

---

### HU-KB-03: Ver Kanban personal (mis tareas)

**Como** desarrollador,
**quiero** ver un Kanban con todas mis tareas asignadas de todos los proyectos,
**para** tener una vista unificada de mi trabajo pendiente.

**Criterios de aceptación:**
- Se muestran todas las tareas asignadas al usuario, independientemente del proyecto
- Cada tarjeta indica a qué proyecto pertenece
- Se puede filtrar por proyecto
- Se puede arrastrar para cambiar estado igual que en el Kanban de proyecto

---

### HU-KB-04: Ver Kanban de equipo

**Como** jefe de equipo,
**quiero** ver un tablero Kanban con todas las tareas de mi equipo,
**para** supervisar el trabajo en curso de todos los miembros.

**Criterios de aceptación:**
- Se muestran las tareas asignadas a cualquier miembro del equipo
- Se puede filtrar por persona y por proyecto
- Se visualiza quién tiene cada tarea asignada
- Se identifican tareas sin asignar

---

### HU-KB-05: Reordenar tareas dentro de una columna

**Como** jefe de equipo,
**quiero** reordenar las tareas dentro de una misma columna del Kanban,
**para** indicar prioridad relativa.

**Criterios de aceptación:**
- Drag & drop vertical dentro de una columna cambia el orden
- El orden se persiste y se muestra consistentemente
- Otros usuarios ven el orden actualizado al refrescar
