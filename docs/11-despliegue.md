# Despliegue e Infraestructura

## Descripción

Configuración del despliegue on-premise con Docker Compose incluyendo todos los servicios necesarios. El único servicio externo es AWS Bedrock para los modelos LLM.

## Servicios Docker Compose

| Servicio | Imagen | Puerto |
|----------|--------|--------|
| frontend | nginx + Angular build | 80 |
| backend | .NET 10 API | 5000 |
| db | postgres:18 + pgvector | 5432 |
| openwebui | Open WebUI | 3000 |
| litellm | LiteLLM Proxy | 4000 |
| keycloak | Keycloak 26+ (solo dev local) | 8080 |

## Historias de Usuario

### HU-DP-01: Levantar el entorno completo con Docker Compose

**Como** administrador,
**quiero** levantar toda la plataforma con un solo comando `docker compose up`,
**para** simplificar el despliegue y la gestión del entorno.

**Criterios de aceptación:**
- Docker Compose incluye: PostgreSQL, Backend .NET, Frontend Angular (nginx), Open WebUI, LiteLLM
- Todas las dependencias se resuelven automáticamente (orden de arranque, health checks)
- Las migraciones de BD se ejecutan al arrancar el backend
- Variables de entorno documentadas para configuración (SSO, Bedrock, BD)

---

### HU-DP-02: Configurar LiteLLM como proxy a Bedrock

**Como** administrador,
**quiero** configurar LiteLLM para que se conecte a AWS Bedrock,
**para** que Open WebUI pueda usar los modelos LLM de Bedrock.

**Criterios de aceptación:**
- Archivo de configuración con los modelos disponibles en Bedrock
- Autenticación con AWS credentials (access key / role)
- Endpoint compatible con OpenAI API
- Seguimiento de costes por usuario habilitado
- Logs de uso accesibles

---

### HU-DP-03: Configurar Open WebUI con la plataforma

**Como** administrador,
**quiero** configurar Open WebUI para que use LiteLLM como provider y la API como Tool Server,
**para** que los usuarios puedan interactuar con la cartera desde el chat.

**Criterios de aceptación:**
- Open WebUI apunta a LiteLLM como proveedor de modelos
- La API de la plataforma está registrada como Tool Server
- Los usuarios de Open WebUI se mapean con los usuarios de la plataforma (misma identidad SSO)
- El modelo tiene acceso a las tools al chatear

---

### HU-DP-04: Monitorización y logs

**Como** administrador,
**quiero** tener acceso a logs centralizados y métricas básicas,
**para** diagnosticar problemas y supervisar el uso del sistema.

**Criterios de aceptación:**
- Logs estructurados (JSON) del backend .NET
- Logs accesibles vía docker logs o volumen compartido
- Health checks en todos los servicios
- Endpoint `/metrics` básico en el backend (requests, errores, latencia)
