## ADDED Requirements

### Requirement: Conversaciones de chat por usuario
El sistema SHALL permitir a cada persona autenticada crear, listar y consultar sus propias conversaciones de chat con el asistente IA, persistidas en la base de datos de la aplicación. Cada conversación tiene un título y pertenece a una única persona.

#### Scenario: Crear una conversación nueva
- **WHEN** una persona autenticada llama a `POST /api/chat/conversations`
- **THEN** el sistema crea una conversación vacía asociada a esa persona y devuelve su id

#### Scenario: Listar las conversaciones propias
- **WHEN** una persona autenticada llama a `GET /api/chat/conversations`
- **THEN** el sistema devuelve únicamente las conversaciones cuyo propietario es esa persona, ordenadas por actividad más reciente primero

#### Scenario: Una persona no puede ver conversaciones de otra
- **WHEN** una persona autenticada llama a `GET /api/chat/conversations/{id}/messages` con el id de una conversación que pertenece a otra persona
- **THEN** el sistema devuelve `404 Not Found` (no revela si la conversación existe)

### Requirement: Envío de mensajes y respuesta del asistente
El sistema SHALL permitir enviar un mensaje de usuario a una conversación y obtener la respuesta del asistente, persistiendo ambos (mensaje de usuario y respuesta) en el historial de la conversación.

#### Scenario: Enviar un mensaje sin necesidad de herramientas
- **WHEN** una persona autenticada llama a `POST /api/chat/conversations/{id}/messages` con un texto que no requiere invocar ninguna tool
- **THEN** el sistema persiste el mensaje del usuario, obtiene una respuesta del modelo configurado, la persiste como mensaje del asistente, y la devuelve en la respuesta HTTP

#### Scenario: Enviar un mensaje que requiere una tool
- **WHEN** el modelo, al procesar el mensaje, solicita ejecutar una tool (p. ej. "¿qué tengo pendiente?" → `get_my_tasks`)
- **THEN** el sistema ejecuta la tool correspondiente, devuelve su resultado al modelo, y persiste en la conversación el resultado final que el modelo compone a partir de ese resultado

#### Scenario: Tope de iteraciones de tool-calling
- **WHEN** el modelo encadena solicitudes de tools sin llegar a una respuesta final tras 5 iteraciones dentro del mismo turno
- **THEN** el sistema detiene el bucle, persiste y devuelve el último contenido textual disponible del modelo junto con un aviso de que se alcanzó el límite

### Requirement: Las tools ejecutan con los permisos del usuario autenticado
El sistema SHALL resolver la identidad de la persona que ejecuta cada tool a partir de la sesión autenticada de la petición HTTP (JWT), nunca a partir de un parámetro que el modelo pueda rellenar en la llamada a la tool. Cada tool SHALL aplicar exactamente las mismas reglas de autorización que su equivalente ya existente en el resto de la API.

#### Scenario: Una tool de escritura respeta las reglas de permisos del dominio
- **WHEN** el modelo invoca la tool de cambio de estado de proyecto para una persona que no pertenece a ningún equipo asignado al proyecto y no es Gestor
- **THEN** el sistema rechaza la operación con el mismo error de autorización que devolvería el endpoint equivalente, y el asistente informa del fallo en su respuesta

#### Scenario: El modelo no puede suplantar a otro usuario
- **WHEN** el mensaje del usuario o la llamada a una tool intenta especificar un identificador de persona distinto al de la sesión autenticada
- **THEN** el sistema ignora ese valor y usa siempre la persona resuelta de la sesión autenticada para ejecutar la tool

### Requirement: Auditoría de acciones del agente
El sistema SHALL registrar en `AgentActionLog` cada ejecución de una tool que modifique datos, con la misma información (persona, nombre de la acción, payload) que se registraba antes de este change, sin distinguir si la llamada vino del chat nativo o de un origen anterior.

#### Scenario: Una tool de escritura queda auditada
- **WHEN** el modelo invoca con éxito una tool que crea o modifica datos (p. ej. crear una tarea)
- **THEN** el sistema añade una entrada a `AgentActionLog` con la persona que originó el mensaje de chat, el nombre de la acción y un resumen de los parámetros

### Requirement: Modelo de IA configurado por la aplicación
El sistema SHALL usar un modelo de lenguaje determinado por configuración de la aplicación (no por el usuario) para todas las conversaciones, apuntando al endpoint compatible con OpenAI expuesto por LiteLLM.

#### Scenario: El usuario no puede elegir modelo
- **WHEN** una persona envía un mensaje de chat
- **THEN** el sistema usa el modelo configurado en la aplicación sin exponer ningún parámetro de selección de modelo en la petición

### Requirement: Panel de chat integrado en la aplicación
El frontend Angular SHALL ofrecer un panel de chat (drawer) accesible desde el layout principal en cualquier pantalla de la aplicación, con lista de conversaciones, creación de conversación nueva, envío de mensajes y visualización de la respuesta del asistente.

#### Scenario: Abrir el panel de chat desde cualquier pantalla
- **WHEN** una persona autenticada hace clic en el icono de chat del header, estando en cualquier ruta de la aplicación
- **THEN** se abre el panel lateral de chat sin navegar fuera de la pantalla actual

#### Scenario: Continuar una conversación existente
- **WHEN** una persona abre el panel de chat y selecciona una conversación anterior de su lista
- **THEN** el panel carga y muestra el historial completo de esa conversación, y permite seguir escribiendo en ella
