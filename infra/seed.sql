-- Cartera de Proyectos TIC — seed de datos de prueba
-- Idempotente: se puede ejecutar varias veces sin duplicar datos

BEGIN;

-- ─── 1. PERSONAS ─────────────────────────────────────────────────────────────

INSERT INTO "Persons" ("SubjectId", "Name", "Email", "Role") VALUES
  -- ACADEMICO
  ('seed-ppujante',           'Pablo Pujante',      'ppujante@umh.es',           'JefeEquipo'),
  ('seed-hlillo',             'Héctor Lillo',        'hlillo@umh.es',             'Desarrollador'),
  ('seed-jmascunan',          'Juan Mascuñán',       'jmascunan@umh.es',          'Desarrollador'),
  ('seed-jose.sanzi',         'José Sanz',           'jose.sanzi@umh.es',         'Desarrollador'),
  ('seed-bvalero',            'Beatriz Valero',      'bvalero@umh.es',            'Desarrollador'),
  ('seed-jvaron',             'Javier Varón',        'jvaron@umh.es',             'Desarrollador'),
  ('seed-rmordente',          'Roberto Mordente',    'rmordente@umh.es',          'Desarrollador'),
  ('seed-miguel.fernandezm',  'Miguel Fernández',    'miguel.fernandezm@umh.es',  'Desarrollador'),
  ('seed-manuel.calvachel',   'Manuel Calvache',     'manuel.calvachel@umh.es',   'Desarrollador'),
  -- OBSERVATORIO
  ('seed-icastellanos',       'Isabel Castellanos',  'icastellanos@umh.es',       'JefeEquipo'),
  ('seed-rcalero',            'Rafael Calero',       'rcalero@umh.es',            'Desarrollador'),
  -- RRHH
  ('seed-imiras',             'Inmaculada Miras',    'imiras@umh.es',             'JefeEquipo'),
  ('seed-l.diaz',             'Luis Díaz',           'l.diaz@umh.es',             'Desarrollador'),
  ('seed-haraceli',           'Helena Araceli',      'haraceli@umh.es',           'Desarrollador'),
  -- INVESTIGACIÓN/ECONOMICO
  ('seed-rgomez',             'Rafael Gómez',        'rgomez@umh.es',             'JefeEquipo'),
  ('seed-j.marin',            'Juan Marín',          'j.marin@umh.es',            'Desarrollador'),
  ('seed-francisco.fernandezm','Francisco Fernández','francisco.fernandezm@umh.es','Desarrollador'),
  ('seed-fmartinez',          'Fernando Martínez',   'fmartinez@umh.es',          'Desarrollador'),
  ('seed-manuel.navarroe',    'Manuel Navarro',      'manuel.navarroe@umh.es',    'Desarrollador'),
  ('seed-paula.perezm',       'Paula Pérez',         'paula.perezm@umh.es',       'Desarrollador'),
  -- SEDE
  ('seed-iamoros',            'Irene Amorós',        'iamoros@umh.es',            'JefeEquipo'),
  ('seed-jmarcos',            'Javier Marcos',       'jmarcos@umh.es',            'Desarrollador'),
  ('seed-jalmela',            'Juan Almela',         'jalmela@umh.es',            'Desarrollador'),
  ('seed-jorge.laraa',        'Jorge Lara',          'jorge.laraa@umh.es',        'Desarrollador'),
  -- WEB/TRANSVERSAL
  ('seed-jdo',                'Jesús Do',            'jdo@umh.es',                'JefeEquipo'),
  ('seed-vmalvarez',          'Virginia Álvarez',    'vmalvarez@umh.es',          'Desarrollador'),
  ('seed-lgomis',             'Luis Gomis',          'lgomis@umh.es',             'Desarrollador'),
  ('seed-amoreno',            'Antonio Moreno',      'amoreno@umh.es',            'Desarrollador'),
  ('seed-ytrush',             'Yolanda Trush',       'ytrush@umh.es',             'Desarrollador'),
  ('seed-apinar',             'Antonio Pinar',       'apinar@umh.es',             'Desarrollador'),
  ('seed-a.zaragoza',         'Andrés Zaragoza',     'a.zaragoza@umh.es',         'Desarrollador'),
  ('seed-ivan.sanchezm',      'Iván Sánchez',        'ivan.sanchezm@umh.es',      'Desarrollador')
ON CONFLICT ("Email") DO NOTHING;

-- ─── 2. EQUIPOS ──────────────────────────────────────────────────────────────

INSERT INTO "Teams" ("Name", "Description", "LeadPersonId")
SELECT t.name, t.description, p."Id"
FROM (VALUES
  ('ACADEMICO',               'Grupo de desarrollo de aplicaciones académicas y de gestión de estudios',        'ppujante@umh.es'),
  ('OBSERVATORIO',            'Grupo de desarrollo del Observatorio Ocupacional y prácticas',                  'icastellanos@umh.es'),
  ('RRHH',                    'Grupo de desarrollo de aplicaciones de Recursos Humanos y nómina',               'imiras@umh.es'),
  ('INVESTIGACIÓN/ECONOMICO', 'Grupo de desarrollo de aplicaciones de investigación y gestión económica',      'rgomez@umh.es'),
  ('SEDE',                    'Grupo de desarrollo de aplicaciones de Sede Electrónica y contratación',         'iamoros@umh.es'),
  ('WEB/TRANSVERSAL',         'Grupo de desarrollo web y proyectos transversales de infraestructura digital',  'jdo@umh.es')
) AS t(name, description, lead_email)
JOIN "Persons" p ON p."Email" = t.lead_email
ON CONFLICT ("Name") DO UPDATE SET
  "Description" = EXCLUDED."Description",
  "LeadPersonId" = EXCLUDED."LeadPersonId";

-- ─── 3. MEMBRESÍAS EQUIPO ─────────────────────────────────────────────────────

INSERT INTO "PersonTeamMemberships" ("PersonId", "TeamId", "JoinedAt")
SELECT p."Id", t."Id", '2026-01-15'
FROM (VALUES
  -- ACADEMICO
  ('ppujante@umh.es',           'ACADEMICO'),
  ('hlillo@umh.es',             'ACADEMICO'),
  ('jmascunan@umh.es',          'ACADEMICO'),
  ('jose.sanzi@umh.es',         'ACADEMICO'),
  ('bvalero@umh.es',            'ACADEMICO'),
  ('jvaron@umh.es',             'ACADEMICO'),
  ('rmordente@umh.es',          'ACADEMICO'),
  ('miguel.fernandezm@umh.es',  'ACADEMICO'),
  ('manuel.calvachel@umh.es',   'ACADEMICO'),
  -- OBSERVATORIO
  ('icastellanos@umh.es',       'OBSERVATORIO'),
  ('rcalero@umh.es',            'OBSERVATORIO'),
  -- RRHH
  ('imiras@umh.es',             'RRHH'),
  ('l.diaz@umh.es',             'RRHH'),
  ('haraceli@umh.es',           'RRHH'),
  -- INVESTIGACIÓN/ECONOMICO
  ('rgomez@umh.es',             'INVESTIGACIÓN/ECONOMICO'),
  ('j.marin@umh.es',            'INVESTIGACIÓN/ECONOMICO'),
  ('francisco.fernandezm@umh.es','INVESTIGACIÓN/ECONOMICO'),
  ('fmartinez@umh.es',          'INVESTIGACIÓN/ECONOMICO'),
  ('manuel.navarroe@umh.es',    'INVESTIGACIÓN/ECONOMICO'),
  ('paula.perezm@umh.es',       'INVESTIGACIÓN/ECONOMICO'),
  -- SEDE
  ('iamoros@umh.es',            'SEDE'),
  ('jmarcos@umh.es',            'SEDE'),
  ('jalmela@umh.es',            'SEDE'),
  ('jorge.laraa@umh.es',        'SEDE'),
  -- WEB/TRANSVERSAL
  ('jdo@umh.es',                'WEB/TRANSVERSAL'),
  ('vmalvarez@umh.es',          'WEB/TRANSVERSAL'),
  ('lgomis@umh.es',             'WEB/TRANSVERSAL'),
  ('amoreno@umh.es',            'WEB/TRANSVERSAL'),
  ('ytrush@umh.es',             'WEB/TRANSVERSAL'),
  ('apinar@umh.es',             'WEB/TRANSVERSAL'),
  ('a.zaragoza@umh.es',         'WEB/TRANSVERSAL'),
  ('ivan.sanchezm@umh.es',      'WEB/TRANSVERSAL')
) AS m(email, team_name)
JOIN "Persons" p ON p."Email" = m.email
JOIN "Teams"   t ON t."Name"  = m.team_name
ON CONFLICT ("PersonId", "TeamId") DO NOTHING;

-- ─── 4. PROYECTOS ────────────────────────────────────────────────────────────
-- Idempotencia: sólo inserta si no existe ya un proyecto con el mismo título en 2026

INSERT INTO "Projects" ("Title", "Description", "RequestingUnit", "Complexity", "Status", "PortfolioYear")
SELECT v.title, v.description, v.requesting_unit, v.complexity, v.status, 2026
FROM (VALUES
  ('PERMISOS 2.0: Nueva interfaz web aplicación de Permisos',
   'La aplicación de Permisos es una aplicación web Fullstack desarrollada hace muchos años con tecnologías desactualizadas, y a día de hoy sólo es posible ejecutar la aplicación en el navegador Edge con Compatibilidad de Internet Explorer habilitada. Se necesita rehacer el frontal para que funcione en todos los navegadores.',
   'Servicio de Innovación y Planificación Tecnológica', 'VeryHigh', 'Proposed'),

  ('Automatización de la generación de los censos para centros e institutos de investigación',
   'Es necesario desarrollar un procedimiento para la automatización de la generación de los censos dado un centro o instituto de investigación, tal y como se realiza actualmente con el resto de elecciones.',
   'Junta Electoral', 'VeryHigh', 'Proposed'),

  ('Propuesta mejora de la aplicación de Reserva de Estancias',
   'Propuestas de mejora de la aplicación de Reservas de Estancias. Se proponen cambios para mejorar la aplicación tanto en la parte de perfil Usuario como perfil Administrador.',
   'Servicio de Infraestructura Informática', 'Medium', 'InProgress'),

  ('Adecuación parcial de la aplicación gestión-red',
   'El proyecto consiste en la adecuación de la aplicación gestión-red que actualmente sólo se puede usar con el navegador Internet Explorer para que se pueda usar con otros navegadores del tipo Chrome o Firefox.',
   'Servicio de Infraestructura Informática', 'VeryHigh', 'Completed'),

  ('MIGRACIÓN DE TIPOS DE ENSEÑANZA NO REGLADA DE CULTURA A LA PLATAFORMA FÓRMATE',
   'Se trata de migrar el sistema de matriculación de nuestras actividades a la nueva plataforma FÓRMATE.',
   'Oficina de Cultura e Igualdad', 'High', 'Proposed'),

  ('Mejoras SATDI Aplicación de Pedidos',
   'Con el seguimiento de los expedientes se ha detectado la necesidad de la implantación de mejoras y actualizaciones que automaticen y simplifiquen procedimientos actualmente realizados fuera de la aplicación, ampliando sus funcionalidades y mejorando la experiencia del usuario.',
   'Servicio de Personal Técnico, de Administración e Investigación', 'VeryHigh', 'InProgress'),

  ('MEJORAS EN LA APLICACIÓN DE PLAN DIRECTOR',
   'Se trata de incluir en la aplicación de Plan Director una serie de mejoras que optimicen su funcionamiento y eviten tener que realizar cálculos manuales de los incentivos de calidad.',
   'Servicio de Calidad', 'High', 'Completed'),

  ('Migración Sistema de Encuestas Calidad de Moodle a LimeSurvey',
   'El objetivo principal de este proyecto es el de migrar el actual sistema de encuestas de calidad basado en Moodle a la plataforma especializada LimeSurvey, con el fin de proporcionar herramientas más específicas para la generación y gestión de encuestas.',
   'Servicio de Calidad', 'VeryHigh', 'Paused'),

  ('CPTI26-00- Plantillas (II)',
   'El proyecto pretende permitir un sistema de plantillas en el que dependiendo de las condiciones específicas de un convenio se genere la documentación de prácticas adaptada al caso sin necesidad de revisión manual por parte del usuario.',
   'Observatorio Ocupacional', 'VeryHigh', 'Completed'),

  ('CPTI26-OO-01- Gestor de Anexos II',
   'Gestor de Anexos II - RPAE e integración en el gestor: finalizar la integración en la aplicación Observatorio del proceso de gestión documental de prácticas con funcionalidades de gestión de remesas, firmas agrupadas de anexos (RPAE), generación de expedientes en SEDE y actualización automatizada de representantes.',
   'Observatorio Ocupacional', 'VeryHigh', 'Approved'),

  ('CPTI26-OO-03 - Prácticas Internas',
   'El proyecto tiene por objeto sincronizar la información de UOR y PDI/PAS existente en las distintas bases de datos para que esté actualizada en el OO para la gestión de prácticas, además de las actividades de control económico de los anexos.',
   'Observatorio Ocupacional', 'VeryHigh', 'InProgress'),

  ('CPTI26-OO-05 Gestión de Ofertas de Empleo para Titulados Oficiales',
   'El proyecto está orientado a sustituir la funcionalidad de OBSGESTION que actualmente se basa en forms y ASP clásico. El objetivo es que las entidades colaboradoras publiquen ofertas de empleo, valoren CV de titulados y realicen sus procesos de selección.',
   'Observatorio Ocupacional', 'VeryHigh', 'Proposed'),

  ('CPTI26-OO-04 - Cuestionarios',
   'Actualmente para la evaluación y seguimiento de prácticas se usa la antigua aplicación CUESTIONARIOS sin soporte. Se quiere dotar a la aplicación Observatorio de un sistema de envío configurable y dirigible que cumpla las expectativas de los distintos títulos.',
   'Observatorio Ocupacional', 'VeryHigh', 'Proposed'),

  ('CPTI26-OO-02 - Iteración del proyecto',
   'Este proyecto incluye varias funcionalidades esenciales para el funcionamiento de las prácticas curriculares (8000 anuales), que afectan a más de 50 titulaciones: recoger condiciones de convenios, dar cabida a estudiantes FPO y reforzar la información de asignaciones en tiempo real.',
   'Observatorio Ocupacional', 'VeryHigh', 'Proposed'),

  ('Formulario específico en Sede electrónica para la presentación de documentación de Becas del Ministerio',
   'El objetivo de este proyecto es implementar en Sede Electrónica un formulario específico para la presentación de documentación vinculada a la convocatoria de Becas del Ministerio de Educación, Formación Profesional y Deportes.',
   'Servicio de Gestión de Estudios', 'High', 'Completed'),

  ('Módulo aplicación Becas Conselleria',
   'El objetivo de este proyecto es potenciar la ya existente aplicación de Becas de Conselleria, incorporando un nuevo módulo que permita integrar en dicha aplicación las nuevas convocatorias (Exención de Tasas, GVA Talent, Premios de Excelencia Académica y cualquier otra convocatoria sobrevenida).',
   'Servicio de Gestión de Estudios', 'High', 'Proposed'),

  ('Módulo aplicación Becas y Ayudas UMH',
   'El objetivo de este proyecto es potenciar la ya existente aplicación de Becas y Ayudas UMH, incorporando un nuevo módulo que permita integrar las nuevas convocatorias (Santander Ayudas al Estudio, Santander Excelencia 360° y otras) y la gestión de distintas actuaciones en convocatorias de otros Servicios.',
   'Servicio de Gestión de Estudios', 'High', 'Proposed'),

  ('CPTI26-OO-06 - Sistema de KPI y Cuadro de Mando Personalizable',
   'El Observatorio genera anualmente una gran cantidad de documentación y situaciones que requieren un control adecuado. Se propone la creación de un cuadro de mandos y el soporte a un sistema de BI que permita esta monitorización.',
   'Observatorio Ocupacional', 'Medium', 'Proposed'),

  ('Módulo aplicación para la compensación de tasas por precios públicos',
   'El objetivo de este proyecto es crear una aplicación que permita realizar las distintas compensaciones económicas por precios públicos: Compensación por Familias Numerosas, Compensación tasas Beca Ministerio, Compensación tasa Beca Generalitat Valenciana y otras.',
   'Servicio de Gestión de Estudios', 'High', 'Proposed'),

  ('CPTI26-OO-08 - Gestión de Afiliación a la Seguridad Social',
   'El proyecto busca completar la implantación de la D.A.52 de la LGSS que dispone el alta en prácticas de estudiantes universitarios, desarrollando un sistema de control del resultado de altas/bajas y determinando el impacto económico real de la medida.',
   'Observatorio Ocupacional', 'Medium', 'Proposed'),

  ('Emisión de certificados de docencia y dirección de enseñanzas de formación permanente a través de la sede electrónica de la UMH',
   'El objetivo de este proyecto es implementar los certificados de docencia y de dirección de las enseñanzas de formación permanente en la sede electrónica de la UMH.',
   'Servicio de Gestión de Estudios', 'High', 'Proposed'),

  ('Reconocimiento de créditos entre los grados que conforman un doble grado',
   'El objetivo consiste en adaptar la gestión de los Programas de Estudios Simultáneos (Dobles Grados) para permitir el reconocimiento de asignaturas de cualquiera de los grados mediante la superación de otras asignaturas del Doble Grado.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'InProgress'),

  ('Formulario específico en Sede electrónica para la solicitud de expedición de duplicados de títulos oficiales',
   'Formulario específico para la solicitud de expedición de duplicados de títulos oficiales, evitando así la presentación a través de una instancia general.',
   'Servicio de Gestión de Estudios', 'Medium', 'Proposed'),

  ('Nueva aplicación para la gestión de los Títulos Oficiales',
   'El objetivo de este proyecto es terminar de desarrollar la nueva aplicación para la gestión de títulos oficiales de la UMH, dando respuesta a las distintas situaciones que se presentan en el Servicio de Gestión de Estudios.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('Transferencia de los expedientes de solicitud de expedición de títulos',
   'El objetivo de este proyecto es transferir los expedientes de solicitud de expedición de títulos oficiales en SEDE desde los Centros de Gestión y Doctorado al SGE.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('Interoperabilidad de datos por recubrimiento',
   'El objetivo de este proyecto es que las aplicaciones de la UMH puedan consultar los datos en las correspondientes plataformas públicas disponibles, según el Catálogo de Servicios de Verificación y Consulta de datos SCSP, en cumplimiento del artículo 28.2 de la Ley 39/2015.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('Modificación de matrícula',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de matrícula parcial y superación de matrícula máxima.',
   'Servicio de Gestión de Estudios', 'Medium', 'Proposed'),

  ('Traslado de expediente',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de traslado de expediente de grado y de pruebas de acceso a la universidad.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('PROTOCOLO GESTIÓN COBRO RECIBOS',
   'El control de impagos de las tasas de matrícula en los estudios de Grado y Máster Universitario es un proceso bastante manual. Se necesita una automatización para el envío de avisos y recordatorios a los estudiantes con pagos pendientes.',
   'CEGECA-San Juan', 'Medium', 'Proposed'),

  ('Anulación de matrícula',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de anulación de matrícula voluntaria y por causa de fuerza mayor.',
   'Servicio de Gestión de Estudios', 'Medium', 'Proposed'),

  ('Aplicación matrícula selectividad',
   'El objetivo del proyecto es obtener para cada exención y bonificación prevista en el Decreto 101/2024 el importe total de la matrícula de la PAU, necesario para solicitar las compensaciones CNEA. Se desarrollará una aplicación de registro para estudiantes de selectividad.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'InProgress'),

  ('Informe Subvenciones CNEA',
   'El objetivo de este proyecto es obtener un informe en formato Excel para solicitar la compensación por Costes Derivados de la Normativa Estatal y Autonómica (CNEA). Aborda el componente académico del cuadro Excel que proporciona la DGU.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('Gestión eficiente de aulas',
   'Implantar un sistema sencillo, digital y verificable que garantice que las aulas reservadas en estancias de la UMH para clases y otras actividades sean efectivamente utilizadas, evitando bloqueos innecesarios y asegurando un uso eficiente de los espacios.',
   'Servicio de Innovación y Planificación Tecnológica', 'VeryHigh', 'Completed'),

  ('Modificaciones en Sexenios AVAP',
   'Solicitud de modificaciones en la aplicación de sexenios AVAP acorde con los cambios que presenta la aplicación de ANECA.',
   'Vicerrectorado de Investigación y Transferencia', 'VeryHigh', 'Completed'),

  ('Gestor de Equipos de Investigación Científica (GEIC)',
   'En la CPTI de 2025 se inició el desarrollo de un nuevo gestor de los equipos de investigación (GEIC). En esta convocatoria se solicita ampliar funcionalidades y poner en producción GEIC para que esté a disposición de investigadores en 2026.',
   'Servicio de Apoyo Técnico a la Docencia y a la Investigación', 'VeryHigh', 'Completed'),

  ('Aplicación de registro de dedicación a proyectos',
   'Desarrollo de una aplicación para registro de dedicación a proyectos, con objeto de recoger las dedicaciones horarias del personal investigador necesarias para la gestión y justificación de los costes de personal de sus proyectos.',
   'Servicio de Gestión de la Investigación', 'VeryHigh', 'Proposed'),

  ('MEJORA - SIGITT 2.0 JUSTIFICACIÓN ECONÓMICA DE PROYECTOS DE INVESTIGACIÓN',
   'Dotar a SIGITT 2.0 de los complementos y mejoras necesarios para su plena utilización e implementación definitiva.',
   'Servicio de Gestión de la Investigación', 'VeryHigh', 'Proposed'),

  ('Mejoras en la aplicación del espacio opositor',
   'El proyecto tiene como objetivo mejorar algunas características de la aplicación Espacio Opositor y agilizar las gestiones administrativas de las convocatorias eliminando las gestiones que se realizan en Hominis-Universitas XXI.',
   'Servicio de Personal Técnico, de Administración e Investigación', 'High', 'Proposed'),

  ('Migración de datos de UXXI Investigación a SIGITT2.0',
   'La aplicación UXXI Investigación de OCU lleva años sin soporte. El objetivo general es la realización de la migración de datos de UXXI Investigación a SIGITT2.0, de forma que sea posible apagar definitivamente UXXI Investigación y la infraestructura asociada.',
   'Servicio de Infraestructura Informática', 'High', 'Proposed'),

  ('Gestión Telefonía UMH',
   'Mejoras para la aplicación Gestión Telefonía UMH.',
   'Servicio de Infraestructura Informática', 'High', 'Completed'),

  ('Certificados de traslado de la PAU Selectividad',
   'El objetivo de este proyecto es modificar la emisión de certificados de traslado de PAU, que se realiza desde la aplicación de Selectividad, de tal forma que se realice a través de la sede electrónica y se obtengan los certificados con sello de órgano.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('Compensaciones exceso horario',
   'El objetivo principal es desarrollar una nueva aplicación que permita el registro y cálculo automatizado de las horas extraordinarias realizadas y previamente autorizadas por los empleados, reemplazando el proceso manual actual.',
   'Servicio de Personal Técnico, de Administración e Investigación', 'Medium', 'Proposed'),

  ('Ausencias',
   'Desarrollo de una nueva aplicación de gestión de ausencias. La solicitud se basa en la necesidad de una nueva herramienta debido a la obsolescencia del sistema actual y al aumento de usuarios, tipos de permisos y autorizadores.',
   'Servicio de Personal Técnico, de Administración e Investigación', 'High', 'Proposed'),

  ('CERTIFICADO HISTÓRICO FORMACIÓN PTGAS',
   'Proyecto para la creación de un Certificado Histórico de las actividades formativas del Personal Técnico, de Gestión y de Administración y Servicios (PTGAS), accesible a través de la Sede Electrónica.',
   'Servicio de Personal Técnico, de Administración e Investigación', 'Low', 'Proposed'),

  ('Continuación de estudios',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de continuación de estudios por distintos motivos.',
   'Servicio de Gestión de Estudios', 'High', 'Proposed'),

  ('Gestor de bolsas de trabajo',
   'Proyecto de desarrollo de un Gestor de bolsas de trabajo destinado al PTGAS para centralizar la gestión de las bolsas de empleo en una única aplicación automatizada, eliminando el riesgo de errores de los procesos manuales actuales.',
   'Servicio de Personal Técnico, de Administración e Investigación', 'High', 'Proposed'),

  ('Reserva de Instalaciones Deportivas UMH - Fase 2',
   'Concluida la Fase 1, centrada en el desarrollo del núcleo funcional de la nueva aplicación que sustituirá a Cronos, se ha identificado un conjunto de funcionalidades complementarias necesarias para satisfacer plenamente los requerimientos de la Oficina de Campus Saludables y Deportes.',
   'Oficina de Deportes', 'High', 'InProgress'),

  ('Mejoras de Escuela de Verano y Aula Junior',
   'El proyecto busca mejorar y agilizar la gestión de la Escuela de Verano y Aula Junior, reduciendo incidencias para usuarios internos y externos. Se optimizará el proceso de validación de la documentación requerida.',
   'Oficina de Deportes', 'High', 'Proposed'),

  ('Ticketing UMH',
   'Implantar mejoras en Ticketing UMH.',
   'Servicio de Infraestructura Informática', 'VeryHigh', 'InProgress'),

  ('Modificaciones programa DOCENTIA_UMH',
   'El objetivo del proyecto es solicitar mejoras y nuevas funcionalidades en la aplicación DOCENTIA, motivadas por las recomendaciones del informe final de la comisión de implantación de la ANECA y la adaptación al Programa de Apoyo DOCENTIA publicado en enero de 2025.',
   'Servicio de Profesorado, Nómina y Seguridad Social', 'VeryHigh', 'InProgress'),

  ('Gestor de currículum',
   'Mejoras a desarrollar en la aplicación Gestor de Currículum, incluyendo la revisión y actualización de la normativa del Procedimiento de Evaluación de la Actividad Investigadora, Transferencia Tecnológica y Difusión de la Ciencia de la UMH.',
   'Vicerrectorado de Investigación y Transferencia', 'VeryHigh', 'Proposed'),

  ('Comprobación Documental',
   'Mejoras a desarrollar en la aplicación Comprobación Documental, con la finalidad de facilitar el proceso de validación de méritos que se realiza a los investigadores de la universidad.',
   'Vicerrectorado de Investigación y Transferencia', 'Medium', 'Proposed'),

  ('Grupos de investigación',
   'Mejoras a desarrollar en la aplicación Grupos Investigación, incluyendo la revisión y actualización de la normativa del Reglamento de Grupos de Investigación de la UMH.',
   'Vicerrectorado de Investigación y Transferencia', 'VeryHigh', 'Approved'),

  ('Reingeniería Gestor de CV',
   'Desarrollar una nueva interfaz respetando el flujo actual de la aplicación. La prioridad será generar una interfaz modular para poder cambiarla en un futuro y reutilizar los módulos. Se creará un frontend y un backend para desacoplar funcionalidades.',
   'Vicerrectorado de Investigación y Transferencia', 'High', 'Proposed'),

  ('Certificados de Investigación',
   'El objeto de este proyecto es la expedición a través de la SEDE electrónica de la UMH de los certificados de investigación que actualmente se expiden manualmente.',
   'Servicio de Transferencia de Conocimiento', 'VeryHigh', 'InProgress'),

  ('Simplificación de procedimiento de prestaciones de servicio',
   'El objeto de este proyecto es simplificar el procedimiento de prestaciones de servicio, tanto periódicas como no-periódicas, mediante una nueva aplicación llamada Prestaciones de Servicio que permita ver el estado del trámite.',
   'Servicio de Transferencia de Conocimiento', 'VeryHigh', 'InProgress'),

  ('Mejoras aplicación de preinscripción',
   'El objetivo de este proyecto es mejorar la aplicación de preinscripción, teniendo en cuenta las necesidades que se han detectado en el último curso académico.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Approved'),

  ('Revisión de la integración UXXI-SIGITT2',
   'El objeto de este proyecto es abordar algunos problemas con la integración UXXI-SIGITT2.',
   'Servicio de Transferencia de Conocimiento', 'Medium', 'Proposed'),

  ('Mejoras aplicación certificados TFM',
   'El objetivo de este proyecto es mejorar la aplicación de Certificado TFM, adecuándola a las necesidades que se han detectado una vez se ha puesto en funcionamiento.',
   'Servicio de Gestión de Estudios', 'High', 'Proposed'),

  ('Mejoras de rendimiento en SIGITT2',
   'El objeto de este proyecto es mejorar el rendimiento de la aplicación SIGITT2 puesto que los gestores de STC y SGI pierden mucho tiempo mientras se cargan o almacenan los datos.',
   'Servicio de Transferencia de Conocimiento', 'VeryHigh', 'InProgress'),

  ('Revisión de la integración GestorCV-SIGITT2',
   'El objeto de este proyecto es abordar algunos problemas con la integración GestorCV-SIGITT2.',
   'Servicio de Transferencia de Conocimiento', 'Medium', 'Proposed'),

  ('Mejora aplicación Retribuciones Adicionales',
   'Mejora de la aplicación de méritos autonómicos, introduciendo comprobación de los límites y ampliando la posibilidad a personal contratado.',
   'Servicio de Profesorado, Nómina y Seguridad Social', 'VeryHigh', 'Completed'),

  ('MEJORAS EN GISBAP',
   'Mejorar la aplicación de gestión de subvenciones (GISBAP) incluyendo nuevas funcionalidades que permitan la simplificación administrativa y agilicen el trabajo. Se requieren integraciones con servicios de intermediación de datos y con el sistema económico UXXI.',
   'Servicio de Modernización y Coordinación Administrativa', 'VeryHigh', 'Proposed'),

  ('MEJORAS GESTIÓN DE CONVENIOS UMH',
   'Mejorar la aplicación Gestión de convenios UMH con las solicitudes de mejora que se han detectado sobre la versión inicial tras la puesta en producción.',
   'Servicio de Modernización y Coordinación Administrativa', 'VeryHigh', 'Proposed'),

  ('Acceso a la Sede electrónica para extranjeros y usuarios externos',
   'El sistema de Autenticación de Ciudadano UE (eIDAS) permite a los ciudadanos de la UE usar su identificación electrónica para acceder a servicios públicos en otros países. Se solicita integración con el nodo eIDAS español a través del sistema Cl@ve y registro de usuarios externos (RUE).',
   'Servicio de Modernización y Coordinación Administrativa', 'VeryHigh', 'Proposed'),

  ('HISTÓRICO INFORMACIÓN DE REGISTRO GENERAL (MASTIN)',
   'La aplicación Mastin lleva años sin soporte. Se propone el diseño de una aplicación o conector ODBC para acceso a registros históricos de Mastín no migrados a Geiser, garantizando que los asientos de libros de registro general sean accesibles.',
   'Servicio de Modernización y Coordinación Administrativa', 'High', 'Completed'),

  ('Web de reserva de actividades para centros de educación secundaria y bachillerato',
   'El objeto de este proyecto es contar con una web de gestión de reserva para que los centros de educación secundaria puedan reservar las actividades que la UMH les ofrece dentro de los programas de difusión y captación.',
   'Servicio de Comunicación, Márketing y Atención al Estudiantado', 'VeryHigh', 'Proposed'),

  ('MEJORAS GESTOR DE EXPEDIENTES Y PORTAFIRMAS UMH',
   'Mejorar la aplicación Gestor de Expedientes UMH y la aplicación Portafirmas UMH con las solicitudes de mejora recibidas que han sido calificadas con prioridad crítica.',
   'Servicio de Modernización y Coordinación Administrativa', 'VeryHigh', 'Proposed'),

  ('ERASMUS WITHOUT PAPER (Acuerdo Académicos conectar con la EWP)',
   'Una vez finalizada la fase 2 de los modelos de cambios y la firma de los acuerdos académicos, la parte de la aplicación destinada a la gestión de acuerdos académicos deberá estar conectada a la red EWP para gestionar en línea los acuerdos de aprendizaje Erasmus+.',
   'Servicio de Relaciones Internacionales y Cooperación', 'VeryHigh', 'InProgress'),

  ('ERASMUS WITHOUT PAPER (TRANSCRIPT OF RECORDS - Conectar con la EWP)',
   'Una vez desarrollada la parte de la aplicación de acuerdo académicos, deberemos estar conectados a la red EWP para gestionar los Transcript of Records entre los países miembros de la UE y terceros países asociados al Programa Erasmus+.',
   'Servicio de Relaciones Internacionales y Cooperación', 'VeryHigh', 'Proposed'),

  ('MOVILIDAD- Propia Estudiante Visitante PARA ESTUDIOS UMH',
   'Procedimiento de admisión de estudiante visitante en la UMH: procedimiento para la admisión de estudiantes visitantes que deseen cursar alguna asignatura y/o realizar un periodo de actividades tuteladas en la UMH, fuera de los programas oficiales de movilidad.',
   'Servicio de Relaciones Internacionales y Cooperación', 'High', 'Proposed'),

  ('MOVILIDAD-REPOSITORIO DE DOCUMENTACIÓN PARA EL ESTUDIANTADO QUE PARTICIPA EN PROGRAMAS DE MOVILIDAD',
   'La creación de un repositorio de documentación para el estudiantado que tenga una plaza de movilidad aceptada dentro de un programa de movilidad.',
   'Servicio de Relaciones Internacionales y Cooperación', 'VeryHigh', 'Proposed'),

  ('MOVILIDAD- REPOSITORIO DE DOCUMENTACIÓN PARA CONVOCATORIAS DE MOVILIDAD DE PDI Y PTGAS',
   'Utilizar los documentos subidos en el espacio opositor para las convocatorias de movilidad internacional del PDI y PTGAS.',
   'Servicio de Relaciones Internacionales y Cooperación', 'High', 'Proposed'),

  ('APLICACIÓN PARA PROYECTOS INTERNACIONALES Y COOPERACIÓN AL DESARROLLO',
   'Se necesita adaptar el módulo de SIGITT2.0 para la gestión de proyectos internacionales y de cooperación al desarrollo (CID) que se gestionan en el Servicio de Relaciones Internacionales y Cooperación.',
   'Servicio de Relaciones Internacionales y Cooperación', 'VeryHigh', 'Proposed'),

  ('Plataforma de inscripciones para Vida UMH',
   'Se trata de diseñar una plataforma de gestión de las inscripciones en las actividades de Vida UMH, tanto las gratuitas como las de pago.',
   'Servicio de Comunicación, Márketing y Atención al Estudiantado', 'VeryHigh', 'Proposed'),

  ('Mejoras SII Aplicación Compras Menores para solicitud a Cartera Proyectos TI 2026 de la UMH',
   'Solicitud de mejoras y adaptación de la aplicación de Compras Menores para agilizar el flujo de comunicaciones entre el Autorizador y Peticionario.',
   'Servicio de Infraestructura Informática', 'High', 'Completed'),

  ('Mejoras SII Aplicación Pedidos para solicitud a Cartera Proyectos TI 2026 de la UMH',
   'Mejoras y adaptación de la Aplicación de Pedidos para mejorar la comunicación con los proveedores del nuevo acuerdo marco de Servicios TIC, permitiendo agilizar el flujo de comunicaciones entre el Peticionario y las empresas proveedoras.',
   'Servicio de Infraestructura Informática', 'VeryHigh', 'InProgress'),

  ('Mejoras Applicación de Seguimiento de Doctorado y Trámites Depósito de Tesis',
   'El objetivo de este proyecto es continuar con la mejora de la aplicación de Seguimiento de Doctorados iniciada en la Cartera de Proyectos de 2025, incluyendo la internacionalización de la Escuela de Doctorado con el acceso de evaluadores, tribunales y estudiantes extranjeros.',
   'Escuela de Doctorado', 'VeryHigh', 'InProgress'),

  ('Mejoras aplicación gestión TFG/M',
   'El objetivo general del proyecto es la mejora de la aplicación de gestión del TFG/M.',
   'Servicio de Gestión de Estudios', 'VeryHigh', 'Proposed'),

  ('REGISTRO DE APODERAMIENTOS Y REGISTRO DE FUNCIONARIOS HABILITADOS UMH',
   'El Registro Electrónico de Apoderamiento permitirá hacer constar las representaciones que los ciudadanos otorguen a terceros para actuar en su nombre de forma electrónica ante la UMH, de acuerdo con la Ley 39/2015.',
   'Servicio de Modernización y Coordinación Administrativa', 'VeryHigh', 'Proposed'),

  ('Herramienta gestión expedientes SDA',
   'Se precisa de una herramienta para la correcta tramitación de los expedientes de contratación por sistema dinámico de adquisición.',
   'Servicio de Planificación y Racionalización de la Contratación', 'VeryHigh', 'InProgress'),

  ('Automatización de las inscripciones de SABIEX',
   'Migrar SABIEX a Fórmate y configurar este tipo de enseñanza como un título propio.',
   'Oficina de Cultura e Igualdad', 'VeryHigh', 'InProgress'),

  ('Elecciones Delegado General de Estudiantes 2026',
   'Elecciones Delegado General de Estudiantes 2026.',
   'Junta Electoral', 'Medium', 'Completed'),

  ('Recibo reserva de plaza preinscripción máster',
   'Se debe abonar un recibo de reserva de plaza de preinscripción de máster para poder matricularse en el máster.',
   'Servicio de Gestión de Estudios', 'Medium', 'Approved'),

  ('Asignaturas equivalentes en Campus Virtual',
   'El proyecto tiene como objetivo facilitar la gestión conjunta de asignaturas equivalentes en el campus virtual. Se definirá por curso académico qué asignaturas y bloques se consideran equivalentes, de manera que la asignatura principal agrupe y gestione el conjunto de asignaturas asociadas.',
   'Servicio de Gestión de Estudios', 'Medium', 'InProgress')

) AS v(title, description, requesting_unit, complexity, status)
WHERE NOT EXISTS (
  SELECT 1 FROM "Projects" p
  WHERE p."Title" = v.title AND p."PortfolioYear" = 2026
);

-- ─── 5. ASIGNACIONES PROYECTO → EQUIPO ───────────────────────────────────────

INSERT INTO "ProjectTeamAssignments" ("ProjectId", "TeamId", "IsPrimary")
SELECT p."Id", t."Id", true
FROM (VALUES
  ('PERMISOS 2.0: Nueva interfaz web aplicación de Permisos',                                                    'WEB/TRANSVERSAL'),
  ('Automatización de la generación de los censos para centros e institutos de investigación',                    'RRHH'),
  ('Propuesta mejora de la aplicación de Reserva de Estancias',                                                   'WEB/TRANSVERSAL'),
  ('Adecuación parcial de la aplicación gestión-red',                                                             'WEB/TRANSVERSAL'),
  ('MIGRACIÓN DE TIPOS DE ENSEÑANZA NO REGLADA DE CULTURA A LA PLATAFORMA FÓRMATE',                              'ACADEMICO'),
  ('Mejoras SATDI Aplicación de Pedidos',                                                                         'SEDE'),
  ('MEJORAS EN LA APLICACIÓN DE PLAN DIRECTOR',                                                                   'RRHH'),
  ('Migración Sistema de Encuestas Calidad de Moodle a LimeSurvey',                                               'WEB/TRANSVERSAL'),
  ('CPTI26-00- Plantillas (II)',                                                                                   'OBSERVATORIO'),
  ('CPTI26-OO-01- Gestor de Anexos II',                                                                           'OBSERVATORIO'),
  ('CPTI26-OO-03 - Prácticas Internas',                                                                           'OBSERVATORIO'),
  ('CPTI26-OO-05 Gestión de Ofertas de Empleo para Titulados Oficiales',                                          'OBSERVATORIO'),
  ('CPTI26-OO-04 - Cuestionarios',                                                                                'OBSERVATORIO'),
  ('CPTI26-OO-02 - Iteración del proyecto',                                                                       'OBSERVATORIO'),
  ('Formulario específico en Sede electrónica para la presentación de documentación de Becas del Ministerio',     'ACADEMICO'),
  ('Módulo aplicación Becas Conselleria',                                                                         'ACADEMICO'),
  ('Módulo aplicación Becas y Ayudas UMH',                                                                        'ACADEMICO'),
  ('CPTI26-OO-06 - Sistema de KPI y Cuadro de Mando Personalizable',                                             'OBSERVATORIO'),
  ('Módulo aplicación para la compensación de tasas por precios públicos',                                        'ACADEMICO'),
  ('CPTI26-OO-08 - Gestión de Afiliación a la Seguridad Social',                                                 'OBSERVATORIO'),
  ('Emisión de certificados de docencia y dirección de enseñanzas de formación permanente a través de la sede electrónica de la UMH',  'ACADEMICO'),
  ('Reconocimiento de créditos entre los grados que conforman un doble grado',                                    'ACADEMICO'),
  ('Formulario específico en Sede electrónica para la solicitud de expedición de duplicados de títulos oficiales', 'ACADEMICO'),
  ('Nueva aplicación para la gestión de los Títulos Oficiales',                                                   'ACADEMICO'),
  ('Transferencia de los expedientes de solicitud de expedición de títulos',                                       'ACADEMICO'),
  ('Interoperabilidad de datos por recubrimiento',                                                                 'ACADEMICO'),
  ('Modificación de matrícula',                                                                                    'ACADEMICO'),
  ('Traslado de expediente',                                                                                       'ACADEMICO'),
  ('PROTOCOLO GESTIÓN COBRO RECIBOS',                                                                             'ACADEMICO'),
  ('Anulación de matrícula',                                                                                       'ACADEMICO'),
  ('Aplicación matrícula selectividad',                                                                            'ACADEMICO'),
  ('Informe Subvenciones CNEA',                                                                                    'ACADEMICO'),
  ('Gestión eficiente de aulas',                                                                                   'WEB/TRANSVERSAL'),
  ('Modificaciones en Sexenios AVAP',                                                                             'RRHH'),
  ('Gestor de Equipos de Investigación Científica (GEIC)',                                                        'WEB/TRANSVERSAL'),
  ('Aplicación de registro de dedicación a proyectos',                                                            'INVESTIGACIÓN/ECONOMICO'),
  ('MEJORA - SIGITT 2.0 JUSTIFICACIÓN ECONÓMICA DE PROYECTOS DE INVESTIGACIÓN',                                   'INVESTIGACIÓN/ECONOMICO'),
  ('Mejoras en la aplicación del espacio opositor',                                                               'SEDE'),
  ('Migración de datos de UXXI Investigación a SIGITT2.0',                                                        'INVESTIGACIÓN/ECONOMICO'),
  ('Gestión Telefonía UMH',                                                                                       'WEB/TRANSVERSAL'),
  ('Certificados de traslado de la PAU Selectividad',                                                             'ACADEMICO'),
  ('Compensaciones exceso horario',                                                                               'RRHH'),
  ('Ausencias',                                                                                                    'RRHH'),
  ('CERTIFICADO HISTÓRICO FORMACIÓN PTGAS',                                                                       'ACADEMICO'),
  ('Continuación de estudios',                                                                                     'ACADEMICO'),
  ('Gestor de bolsas de trabajo',                                                                                  'RRHH'),
  ('Reserva de Instalaciones Deportivas UMH - Fase 2',                                                            'WEB/TRANSVERSAL'),
  ('Mejoras de Escuela de Verano y Aula Junior',                                                                   'WEB/TRANSVERSAL'),
  ('Ticketing UMH',                                                                                               'WEB/TRANSVERSAL'),
  ('Modificaciones programa DOCENTIA_UMH',                                                                        'ACADEMICO'),
  ('Gestor de currículum',                                                                                        'INVESTIGACIÓN/ECONOMICO'),
  ('Comprobación Documental',                                                                                      'INVESTIGACIÓN/ECONOMICO'),
  ('Grupos de investigación',                                                                                      'INVESTIGACIÓN/ECONOMICO'),
  ('Reingeniería Gestor de CV',                                                                                    'INVESTIGACIÓN/ECONOMICO'),
  ('Certificados de Investigación',                                                                               'INVESTIGACIÓN/ECONOMICO'),
  ('Simplificación de procedimiento de prestaciones de servicio',                                                  'INVESTIGACIÓN/ECONOMICO'),
  ('Mejoras aplicación de preinscripción',                                                                        'ACADEMICO'),
  ('Revisión de la integración UXXI-SIGITT2',                                                                     'INVESTIGACIÓN/ECONOMICO'),
  ('Mejoras aplicación certificados TFM',                                                                         'ACADEMICO'),
  ('Mejoras de rendimiento en SIGITT2',                                                                            'INVESTIGACIÓN/ECONOMICO'),
  ('Revisión de la integración GestorCV-SIGITT2',                                                                  'INVESTIGACIÓN/ECONOMICO'),
  ('Mejora aplicación Retribuciones Adicionales',                                                                  'RRHH'),
  ('MEJORAS EN GISBAP',                                                                                            'SEDE'),
  ('MEJORAS GESTIÓN DE CONVENIOS UMH',                                                                            'SEDE'),
  ('Acceso a la Sede electrónica para extranjeros y usuarios externos',                                            'WEB/TRANSVERSAL'),
  ('HISTÓRICO INFORMACIÓN DE REGISTRO GENERAL (MASTIN)',                                                           'SEDE'),
  ('Web de reserva de actividades para centros de educación secundaria y bachillerato',                            'WEB/TRANSVERSAL'),
  ('MEJORAS GESTOR DE EXPEDIENTES Y PORTAFIRMAS UMH',                                                             'SEDE'),
  ('ERASMUS WITHOUT PAPER (Acuerdo Académicos conectar con la EWP)',                                               'ACADEMICO'),
  ('ERASMUS WITHOUT PAPER (TRANSCRIPT OF RECORDS - Conectar con la EWP)',                                          'ACADEMICO'),
  ('MOVILIDAD- Propia Estudiante Visitante PARA ESTUDIOS UMH',                                                    'ACADEMICO'),
  ('MOVILIDAD-REPOSITORIO DE DOCUMENTACIÓN PARA EL ESTUDIANTADO QUE PARTICIPA EN PROGRAMAS DE MOVILIDAD',         'ACADEMICO'),
  ('MOVILIDAD- REPOSITORIO DE DOCUMENTACIÓN PARA CONVOCATORIAS DE MOVILIDAD DE PDI Y PTGAS',                      'ACADEMICO'),
  ('APLICACIÓN PARA PROYECTOS INTERNACIONALES Y COOPERACIÓN AL DESARROLLO',                                       'INVESTIGACIÓN/ECONOMICO'),
  ('Plataforma de inscripciones para Vida UMH',                                                                   'WEB/TRANSVERSAL'),
  ('Mejoras SII Aplicación Compras Menores para solicitud a Cartera Proyectos TI 2026 de la UMH',                  'SEDE'),
  ('Mejoras SII Aplicación Pedidos para solicitud a Cartera Proyectos TI 2026 de la UMH',                          'SEDE'),
  ('Mejoras Applicación de Seguimiento de Doctorado y Trámites Depósito de Tesis',                                 'ACADEMICO'),
  ('Mejoras aplicación gestión TFG/M',                                                                            'ACADEMICO'),
  ('REGISTRO DE APODERAMIENTOS Y REGISTRO DE FUNCIONARIOS HABILITADOS UMH',                                       'SEDE'),
  ('Herramienta gestión expedientes SDA',                                                                         'SEDE'),
  ('Automatización de las inscripciones de SABIEX',                                                               'ACADEMICO'),
  ('Elecciones Delegado General de Estudiantes 2026',                                                             'RRHH'),
  ('Recibo reserva de plaza preinscripción máster',                                                               'ACADEMICO'),
  ('Asignaturas equivalentes en Campus Virtual',                                                                   'ACADEMICO')
) AS a(project_title, team_name)
JOIN "Projects" p ON p."Title" = a.project_title AND p."PortfolioYear" = 2026
JOIN "Teams"    t ON t."Name"  = a.team_name
ON CONFLICT ("ProjectId", "TeamId") DO NOTHING;

COMMIT;

-- Resumen
SELECT
  (SELECT COUNT(*) FROM "Persons")  AS personas,
  (SELECT COUNT(*) FROM "Teams")    AS equipos,
  (SELECT COUNT(*) FROM "PersonTeamMemberships") AS membresías,
  (SELECT COUNT(*) FROM "Projects") AS proyectos,
  (SELECT COUNT(*) FROM "ProjectTeamAssignments") AS asignaciones;
