# Autenticación y Roles

## Descripción

Integración con el SSO de la universidad para autenticación y sistema de autorización basado en roles para controlar el acceso a funcionalidades.

## Roles del sistema

| Rol | Permisos principales |
|-----|---------------------|
| Gestor de cartera | Acceso total: CRUD proyectos, equipos, personas, informes, asignar roles |
| Jefe de equipo | Gestionar tareas de su equipo, ver carga, generar informes de sus proyectos |
| Desarrollador | Ver sus tareas, actualizar estado, crear tareas, autoasignarse |

## Historias de Usuario

### HU-AU-01: Iniciar sesión con SSO

**Como** usuario de la universidad,
**quiero** iniciar sesión con mis credenciales universitarias,
**para** acceder a la plataforma sin crear una cuenta nueva.

**Criterios de aceptación:**
- El login redirige al SSO de la universidad (OAuth 2.0 PKCE)
- Una vez autenticado, se redirige de vuelta a la aplicación
- Si el usuario no existe en la plataforma, se crea automáticamente con rol Desarrollador
- La sesión se mantiene activa con refresh tokens

---

### HU-AU-02: Gestionar roles de usuarios

**Como** gestor de cartera,
**quiero** asignar roles a los usuarios de la plataforma,
**para** controlar quién puede hacer qué.

**Criterios de aceptación:**
- Roles disponibles: Gestor de cartera, Jefe de equipo, Desarrollador
- Un usuario puede tener un solo rol principal
- Solo el Gestor de cartera puede cambiar roles
- El cambio de rol se aplica inmediatamente

---

### HU-AU-03: Restricción de acciones por rol

**Como** sistema,
**quiero** restringir las acciones disponibles según el rol del usuario,
**para** mantener la integridad de los datos y la gobernanza.

**Criterios de aceptación:**
- **Gestor de cartera**: acceso total
- **Jefe de equipo**: gestionar tareas de su equipo, ver carga de su equipo, generar informes de sus proyectos
- **Desarrollador**: ver sus tareas, actualizar estado de sus tareas, crear tareas, autoasignarse
- Los endpoints de la API devuelven 403 si el usuario no tiene permisos
- El frontend oculta opciones no disponibles para el rol

---

### HU-AU-04: API Key para agentes IA

**Como** administrador,
**quiero** generar API Keys para que los agentes IA accedan a la plataforma,
**para** permitir la integración con Open WebUI de forma segura.

**Criterios de aceptación:**
- Se pueden generar API Keys asociadas a un usuario (y sus permisos)
- Las API Keys tienen un nombre descriptivo y fecha de expiración opcional
- Se pueden revocar en cualquier momento
- Las peticiones con API Key se registran en el log de auditoría
