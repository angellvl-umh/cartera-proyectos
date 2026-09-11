## ADDED Requirements

### Requirement: Composer siempre disponible
El panel de chat SHALL mostrar el composer (campo de texto y botón de enviar) siempre visible y utilizable, exista o no una conversación activa seleccionada.

#### Scenario: Abrir el panel sin conversación activa
- **WHEN** el usuario abre el panel de chat y no hay ninguna conversación seleccionada
- **THEN** el campo de texto está visible y habilitado para escribir, sin necesidad de pulsar ningún botón previo

#### Scenario: Panel con una conversación existente seleccionada
- **WHEN** el usuario tiene una conversación existente abierta
- **THEN** el composer sigue visible y utilizable exactamente igual que hoy

### Requirement: Creación implícita de conversación al primer envío
El sistema SHALL crear una conversación nueva automáticamente como parte del envío del primer mensaje, cuando no hay ninguna conversación activa seleccionada, sin requerir una acción explícita adicional del usuario.

#### Scenario: Enviar el primer mensaje sin conversación activa
- **WHEN** el usuario escribe un mensaje y lo envía sin tener ninguna conversación seleccionada
- **THEN** el sistema crea una conversación nueva, la marca como activa y envía el mensaje en ella, mostrando la respuesta del asistente como en cualquier otra conversación

#### Scenario: Título derivado del contenido del mensaje
- **WHEN** se crea una conversación de forma implícita al enviar el primer mensaje
- **THEN** el título de la conversación se deriva del propio texto del mensaje (primera línea, recortada si es muy larga), sin requerir que el usuario escriba un título

#### Scenario: Fallo al crear la conversación implícita
- **WHEN** la creación automática de la conversación falla (p. ej. error de red)
- **THEN** el sistema muestra un error y conserva el texto escrito por el usuario en el composer para reintentar, sin enviarlo como mensaje suelto

#### Scenario: Enviar un mensaje con conversación ya activa
- **WHEN** el usuario envía un mensaje teniendo ya una conversación activa
- **THEN** el mensaje se añade a esa conversación existente, sin crear ninguna conversación nueva

### Requirement: Sin conversaciones vacías huérfanas
El sistema SHALL NOT crear ninguna conversación persistida hasta que el usuario envíe efectivamente un primer mensaje en ella.

#### Scenario: Volver al estado de borrador desde una conversación existente
- **WHEN** el usuario tiene una conversación existente abierta y pulsa la acción de "nueva conversación"
- **THEN** el panel vuelve al estado sin conversación activa (borrador en blanco, listo para escribir) sin crear ninguna conversación nueva en el backend

#### Scenario: Cerrar el panel sin escribir nada
- **WHEN** el usuario abre el panel de chat, no selecciona ninguna conversación existente, no escribe ningún mensaje, y cierra el panel
- **THEN** no queda ninguna conversación nueva registrada en su historial

### Requirement: Aislamiento de respuestas asíncronas entre conversaciones
El sistema SHALL evitar que la respuesta de un envío de mensaje en curso sobrescriba la vista de una conversación o borrador distinto que el usuario haya abierto mientras tanto.

#### Scenario: Cambiar de conversación mientras una respuesta anterior sigue en curso
- **WHEN** el usuario envía un mensaje en una conversación, y antes de recibir la respuesta selecciona otra conversación o abre un borrador nuevo
- **THEN** al llegar la respuesta pendiente, esta no reemplaza los mensajes mostrados en la vista actualmente abierta, que pertenece a otra conversación
