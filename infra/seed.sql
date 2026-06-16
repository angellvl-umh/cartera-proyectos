-- Cartera de Proyectos TIC — seed de datos v2 (esquema post AddProjectExtendedFields)
-- Idempotente: se puede ejecutar varias veces sin duplicar datos.

BEGIN;

-- ─── 1. PROMOTORES ────────────────────────────────────────────────────────────

INSERT INTO "Promoters" ("Name") VALUES
  ('Vicerrectorado de Infraestructuras'),
  ('Vicerrectorado de Investigación y Transferencia'),
  ('Vicerrectorado de Cultura, Igualdad y Diversidad'),
  ('Gerencia'),
  ('Vicerrectorado de Estudiantes y Coordinación'),
  ('Vicerrectorado de Estudios'),
  ('Rectorado'),
  ('Secretaría General'),
  ('Vicerrectorado de Internacionalización y Cooperación'),
  ('Vicerrectorado de Profesorado')
ON CONFLICT ("Name") DO NOTHING;

-- ─── 2. UNIDADES ORGÁNICAS ────────────────────────────────────────────────────

INSERT INTO "OrganicUnits" ("Name", "Code") VALUES
  ('Servicio de Innovación y Planificación Tecnológica',             NULL),
  ('Junta Electoral',                                                NULL),
  ('Servicio de Infraestructura Informática',                        NULL),
  ('Oficina de Cultura e Igualdad',                                  NULL),
  ('Servicio de Personal Técnico, de Administración e Investigación',NULL),
  ('Servicio de Calidad',                                            NULL),
  ('Observatorio Ocupacional',                                       NULL),
  ('Servicio de Gestión de Estudios',                                NULL),
  ('Vicerrectorado de Investigación y Transferencia',                NULL),
  ('Servicio de Gestión de la Investigación',                        NULL),
  ('Servicio de Apoyo Técnico a la Docencia y a la Investigación',   NULL),
  ('Servicio de Transferencia de Conocimiento',                      NULL),
  ('Servicio de Modernización y Coordinación Administrativa',        NULL),
  ('Servicio de Relaciones Internacionales y Cooperación',           NULL),
  ('Servicio de Comunicación, Márketing y Atención al Estudiantado', NULL),
  ('Oficina de Deportes',                                            NULL),
  ('Servicio de Profesorado, Nómina y Seguridad Social',             NULL),
  ('CEGECA-San Juan',                                                NULL),
  ('Escuela de Doctorado',                                           NULL),
  ('Servicio de Planificación y Racionalización de la Contratación', NULL)
ON CONFLICT ("Name") DO NOTHING;

-- ─── 3. PERSONAS ─────────────────────────────────────────────────────────────

INSERT INTO "Persons" ("SubjectId", "Name", "Email", "Role") VALUES
  -- ACADEMICO
  ('seed-ppujante',             'Pablo Pujante',         'ppujante@umh.es',              'JefeEquipo'),
  ('seed-hlillo',               'Héctor Lillo',           'hlillo@umh.es',                'Desarrollador'),
  ('seed-jmascunan',            'Juan Mascuñán',          'jmascunan@umh.es',             'Desarrollador'),
  ('seed-jose.sanzi',           'José Sanz',              'jose.sanzi@umh.es',            'Desarrollador'),
  ('seed-bvalero',              'Beatriz Valero',         'bvalero@umh.es',               'Desarrollador'),
  ('seed-jvaron',               'Javier Varón',           'jvaron@umh.es',                'Desarrollador'),
  ('seed-rmordente',            'Roberto Mordente',       'rmordente@umh.es',             'Desarrollador'),
  ('seed-miguel.fernandezm',    'Miguel Fernández',       'miguel.fernandezm@umh.es',     'Desarrollador'),
  ('seed-manuel.calvachel',     'Manuel Calvache',        'manuel.calvachel@umh.es',      'Desarrollador'),
  -- OBSERVATORIO
  ('seed-icastellanos',         'Isabel Castellanos',     'icastellanos@umh.es',          'JefeEquipo'),
  ('seed-rcalero',              'Rafael Calero',          'rcalero@umh.es',               'Desarrollador'),
  -- RRHH
  ('seed-imiras',               'Inmaculada Miras',       'imiras@umh.es',                'JefeEquipo'),
  ('seed-l.diaz',               'Luis Díaz',              'l.diaz@umh.es',                'Desarrollador'),
  ('seed-haraceli',             'Helena Araceli',         'haraceli@umh.es',              'Desarrollador'),
  -- INVESTIGACIÓN/ECONOMICO
  ('seed-rgomez',               'Rafael Gómez',           'rgomez@umh.es',                'JefeEquipo'),
  ('seed-j.marin',              'Juan Marín',             'j.marin@umh.es',               'Desarrollador'),
  ('seed-francisco.fernandezm', 'Francisco Fernández',    'francisco.fernandezm@umh.es',  'Desarrollador'),
  ('seed-fmartinez',            'Fernando Martínez',      'fmartinez@umh.es',             'Desarrollador'),
  ('seed-manuel.navarroe',      'Manuel Navarro',         'manuel.navarroe@umh.es',       'Desarrollador'),
  ('seed-paula.perezm',         'Paula Pérez',            'paula.perezm@umh.es',          'Desarrollador'),
  -- SEDE
  ('seed-iamoros',              'Irene Amorós',           'iamoros@umh.es',               'JefeEquipo'),
  ('seed-jmarcos',              'Javier Marcos',          'jmarcos@umh.es',               'Desarrollador'),
  ('seed-jalmela',              'Juan Almela',            'jalmela@umh.es',               'Desarrollador'),
  ('seed-jorge.laraa',          'Jorge Lara',             'jorge.laraa@umh.es',           'Desarrollador'),
  -- WEB/TRANSVERSAL
  ('seed-jdo',                  'Jesús Do',               'jdo@umh.es',                   'JefeEquipo'),
  ('seed-vmalvarez',            'Virginia Álvarez',       'vmalvarez@umh.es',             'Desarrollador'),
  ('seed-lgomis',               'Luis Gomis',             'lgomis@umh.es',                'Desarrollador'),
  ('seed-amoreno',              'Antonio Moreno',         'amoreno@umh.es',               'Desarrollador'),
  ('seed-ytrush',               'Yolanda Trush',          'ytrush@umh.es',                'Desarrollador'),
  ('seed-apinar',               'Antonio Pinar',          'apinar@umh.es',                'Desarrollador'),
  ('seed-a.zaragoza',           'Andrés Zaragoza',        'a.zaragoza@umh.es',            'Desarrollador'),
  ('seed-ivan.sanchezm',        'Iván Sánchez',           'ivan.sanchezm@umh.es',         'Desarrollador')
ON CONFLICT ("Email") DO NOTHING;

-- ─── 4. EQUIPOS ──────────────────────────────────────────────────────────────

INSERT INTO "Teams" ("Name", "Description", "LeadPersonId")
SELECT t.name, t.description, p."Id"
FROM (VALUES
  ('ACADEMICO',               'Grupo de desarrollo de aplicaciones académicas y de gestión de estudios',       'ppujante@umh.es'),
  ('OBSERVATORIO',            'Grupo de desarrollo del Observatorio Ocupacional y prácticas',                  'icastellanos@umh.es'),
  ('RRHH',                    'Grupo de desarrollo de aplicaciones de Recursos Humanos y nómina',              'imiras@umh.es'),
  ('INVESTIGACIÓN/ECONOMICO', 'Grupo de desarrollo de aplicaciones de investigación y gestión económica',     'rgomez@umh.es'),
  ('SEDE',                    'Grupo de desarrollo de aplicaciones de Sede Electrónica y contratación',        'iamoros@umh.es'),
  ('WEB/TRANSVERSAL',         'Grupo de desarrollo web y proyectos transversales de infraestructura digital', 'jdo@umh.es')
) AS t(name, description, lead_email)
JOIN "Persons" p ON p."Email" = t.lead_email
ON CONFLICT ("Name") DO UPDATE SET
  "Description"  = EXCLUDED."Description",
  "LeadPersonId" = EXCLUDED."LeadPersonId";

-- ─── 5. MEMBRESÍAS EQUIPO ─────────────────────────────────────────────────────

INSERT INTO "PersonTeamMemberships" ("PersonId", "TeamId", "JoinedAt")
SELECT p."Id", t."Id", '2026-01-15'
FROM (VALUES
  -- ACADEMICO
  ('ppujante@umh.es',            'ACADEMICO'),
  ('hlillo@umh.es',              'ACADEMICO'),
  ('jmascunan@umh.es',           'ACADEMICO'),
  ('jose.sanzi@umh.es',          'ACADEMICO'),
  ('bvalero@umh.es',             'ACADEMICO'),
  ('jvaron@umh.es',              'ACADEMICO'),
  ('rmordente@umh.es',           'ACADEMICO'),
  ('miguel.fernandezm@umh.es',   'ACADEMICO'),
  ('manuel.calvachel@umh.es',    'ACADEMICO'),
  -- OBSERVATORIO
  ('icastellanos@umh.es',        'OBSERVATORIO'),
  ('rcalero@umh.es',             'OBSERVATORIO'),
  -- RRHH
  ('imiras@umh.es',              'RRHH'),
  ('l.diaz@umh.es',              'RRHH'),
  ('haraceli@umh.es',            'RRHH'),
  -- INVESTIGACIÓN/ECONOMICO
  ('rgomez@umh.es',              'INVESTIGACIÓN/ECONOMICO'),
  ('j.marin@umh.es',             'INVESTIGACIÓN/ECONOMICO'),
  ('francisco.fernandezm@umh.es','INVESTIGACIÓN/ECONOMICO'),
  ('fmartinez@umh.es',           'INVESTIGACIÓN/ECONOMICO'),
  ('manuel.navarroe@umh.es',     'INVESTIGACIÓN/ECONOMICO'),
  ('paula.perezm@umh.es',        'INVESTIGACIÓN/ECONOMICO'),
  -- SEDE
  ('iamoros@umh.es',             'SEDE'),
  ('jmarcos@umh.es',             'SEDE'),
  ('jalmela@umh.es',             'SEDE'),
  ('jorge.laraa@umh.es',         'SEDE'),
  -- WEB/TRANSVERSAL
  ('jdo@umh.es',                 'WEB/TRANSVERSAL'),
  ('vmalvarez@umh.es',           'WEB/TRANSVERSAL'),
  ('lgomis@umh.es',              'WEB/TRANSVERSAL'),
  ('amoreno@umh.es',             'WEB/TRANSVERSAL'),
  ('ytrush@umh.es',              'WEB/TRANSVERSAL'),
  ('apinar@umh.es',              'WEB/TRANSVERSAL'),
  ('a.zaragoza@umh.es',          'WEB/TRANSVERSAL'),
  ('ivan.sanchezm@umh.es',       'WEB/TRANSVERSAL')
) AS m(email, team_name)
JOIN "Persons" p ON p."Email" = m.email
JOIN "Teams"   t ON t."Name"  = m.team_name
ON CONFLICT ("PersonId", "TeamId") DO NOTHING;

-- ─── 6. PROYECTOS ────────────────────────────────────────────────────────────
-- FKs de Promoter y OrganicUnit resueltas por nombre en el SELECT.
-- Columnas nullable (gp, uor, prev, desired_date, specs_url, epic_url) pasan como
-- text o NULL; se castean en el SELECT.
-- Idempotencia: WHERE NOT EXISTS por (Title, PortfolioYear).

INSERT INTO "Projects" (
  "Title", "Description", "Status", "Complexity", "PortfolioYear",
  "PromoterId", "OrganicUnitId",
  "GroupPriority", "SiptGroup", "UorOrder", "PreviousReferenceId",
  "DesiredDeploymentDate", "SpecificationsUrl", "EpicUrl"
)
SELECT
  v.title, v.description, v.status, v.complexity, 2026,
  pr."Id", ou."Id",
  v.gp::integer, v.sg, v.uor::integer, v.prev::integer,
  v.ddate::date, v.specs, v.epic
FROM (VALUES
  -- 1
  ('PERMISOS 2.0: Nueva interfaz web aplicación de Permisos'::text,
   'La aplicación de Permisos es una aplicación web Fullstack desarrollada hace muchos años con tecnologías desactualizadas, y a día de hoy sólo es posible ejecutar la aplicación en el navegador Edge con Compatibilidad de Internet Explorer habilitada. Se necesita rehacer el frontal para que funcione en todos los navegadores.'::text,
   'Stopped'::text,'Small'::text,
   'Vicerrectorado de Infraestructuras'::text,'Servicio de Innovación y Planificación Tecnológica'::text,
   '5'::text,'WebTransversal'::text,'13'::text,NULL::text,'2026-06-30'::text,
   'https://drive.google.com/open?id=16RrRoj-3BWGSEYWIpt6A4idgi2yGcowl'::text,NULL::text),
  -- 2
  ('Automatización de la generación de los censos para centros e institutos de investigación',
   'Es necesario desarrollar un procedimiento para la automatización de la generación de los censos dado un centro o instituto de investigación, tal y como se realiza actualmente con el resto de elecciones.',
   'Stopped','Small',
   'Vicerrectorado de Investigación y Transferencia','Junta Electoral',
   '5','RRHH','5',NULL,'2026-01-01',
   'https://drive.google.com/open?id=16WRDmzJnl3owgX-GMRNURzPtUrwXNKSl',NULL),
  -- 3
  ('Propuesta mejora de la aplicación de Reserva de Estancias',
   'Propuestas de mejora de la aplicación de Reservas de Estancias. Se proponen cambios para mejorar la aplicación tanto en la parte de perfil Usuario como perfil Administrador.',
   'InSprint','Medium',
   'Vicerrectorado de Infraestructuras','Servicio de Infraestructura Informática',
   '3','WebTransversal','9','27','2026-05-01',
   'https://drive.google.com/open?id=1b-KWDbjYzriEVm8sTkGK7ofYl792vvw2',NULL),
  -- 4
  ('Adecuación parcial de la aplicación gestión-red',
   'El proyecto consiste en la adecuación de la aplicación gestión-red que actualmente sólo se puede usar con el navegador Internet Explorer o Microsoft Edge (en modo de compatibilidad) para que se pueda usar con otros navegadores del tipo Chrome o Firefox.',
   'Completed','Medium',
   'Vicerrectorado de Infraestructuras','Servicio de Infraestructura Informática',
   '5','WebTransversal','12','40','2026-07-31',
   'https://drive.google.com/open?id=1WYLLDdPcDxZ9qJDZ1JDeLw-cZ0pIbm1j',NULL),
  -- 5
  ('MIGRACIÓN DE TIPOS DE ENSEÑANZA NO REGLADA DE CULTURA A LA PLATAFORMA FÓRMATE',
   'Se trata de migrar el sistema de matriculación de nuestras actividades a la nueva plataforma FÓRMATE.',
   'PlanningWithClient','Medium',
   'Vicerrectorado de Cultura, Igualdad y Diversidad','Oficina de Cultura e Igualdad',
   '4','Academico','6',NULL,'2026-11-30',
   'https://drive.google.com/open?id=1vqOd3x4frKamtfXNEyBPBJlhY_1tBWYG',NULL),
  -- 6
  ('Mejoras SATDI Aplicación de Pedidos',
   'Con el seguimiento de los expedientes 2024_SDA_01 y 2025_AM_02, cuyos trámites se realizan mediante la Aplicación de Pedidos, así como tras atender a demandas de los usuarios, se ha detectado la necesidad de la implantación de mejoras y actualizaciones que automaticen y simplifiquen procedimientos actualmente realizados fuera de ella.',
   'DevelopmentOutsideSprint','Medium',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Personal Técnico, de Administración e Investigación',
   '5','Sede','6',NULL,'2026-06-01',
   'https://drive.google.com/open?id=1v-vPwsfvLb27ZGU_RW9PWAYf6MRI4m9z',NULL),
  -- 7
  ('MEJORAS EN LA APLICACIÓN DE PLAN DIRECTOR',
   'Se trata de incluir en la aplicación de Plan Director una serie de mejoras que optimicen su funcionamiento y eviten tener que realizar cálculos manuales de los incentivos de calidad.',
   'Completed','Small',
   'Gerencia','Servicio de Calidad',
   '4','RRHH','2',NULL,'2025-12-31',
   'https://drive.google.com/open?id=1ACO0Jf8vF98_C75dqzLdKfjnNHlkKPWQ',
   'https://cau-old.umh.es/browse/CALIDAD-425'),
  -- 8
  ('Migración Sistema de Encuestas Calidad de Moodle a LimeSurvey',
   'El objetivo principal de este proyecto es el de migrar el actual sistema de encuestas de calidad basado en Moodle a la plataforma especializada de gestión de Encuestas LimeSurvey con el fin de proporcionar herramientas más específicas para la generación y gestión de encuestas, así como dotar de independencia en la creación y distribución de las mismas.',
   'PostponedByClient','Medium',
   'Vicerrectorado de Estudios','Servicio de Calidad',
   '5','WebTransversal','3','57','2027-07-01',
   'https://drive.google.com/open?id=1VgqW50DOaSzlPFXnfEBZZAymHKkp4NLx',NULL),
  -- 9
  ('CPTI26-00- Plantillas (II)',
   'El proyecto pretende permitir un sistema de plantillas, en el que dependiendo de las condiciones específicas de un convenio se generen la documentación de prácticas adaptada al caso sin necesidad de que el usuario que haga el envío deba revisarlo. Además, plantillas automatizadas y envíos editables para poder personalizar las comunicaciones y evitar pérdidas o malos entendidos.',
   'Completed','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '5','Observatorio','1',NULL,'2026-02-28',
   'https://drive.google.com/open?id=1Quy7tCZlUyWdsrjetifQgkpxwrMel_N3',NULL),
  -- 10
  ('CPTI26-OO-01- Gestor de Anexos II',
   'CPTI2601 - Gestor de Anexos II - RPAE e integración en el gestor, tiene por objetivo finalizar de integrar en la aplicación Observatorio el proceso de gestión documental de prácticas con 5 funcionalidades básicas: Gestión de remesas de documentación, Gestión de firmas agrupadas de anexos (RPAE), Generación de Expedientes en SEDE, Actualización automatizada de representantes.',
   'PlanningSprint','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '5','Observatorio','2','86','2026-05-30',
   'https://drive.google.com/open?id=1QXSAzoMRXQ2K50xnmoJeX08WTCrYftDe',
   'https://cau-old.umh.es/browse/EPRACTICAS-2536'),
  -- 11
  ('CPTI26-OO-03 - Prácticas Internas',
   'El proyecto tiene por objeto sincronizar la información de las UOR y PDI/PAS existente en las distintas bases de datos, de modo que se encuentre actualizada en el OO para la gestión de prácticas. Además, incluye las actividades de control económico de los anexos a las normas de ejecución del presupuesto para la gestión de prácticas internas.',
   'InSprint','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '5','Observatorio','4','90','2026-07-31',
   'https://drive.google.com/open?id=1_AXcSRdJDjbKQJUN15P12YY9a2uk03C-',
   'https://cau-old.umh.es/browse/EPRACTICAS-2520'),
  -- 12
  ('CPTI26-OO-05 Gestión de Ofertas de Empleo para Titulados Oficiales',
   'El proyecto está orientado a sustituir esta funcionalidad de OBSGESTION que actualmente se basa en forms y ASP clásico. El objetivo es que las entidades colaboradoras publiquen ofertas de empleo, valoren CV de titulados y realicen sus procesos de selección de nuestros titulados.',
   'Stopped','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '5','Observatorio','6','93','2026-10-31',
   'https://drive.google.com/open?id=1WnwgwKZr8GIQDQXG8TwJbwNV6Ef83dlt',NULL),
  -- 13
  ('CPTI26-OO-04 - Cuestionarios',
   'Actualmente para la evaluación y seguimiento de prácticas se usa la antigua aplicación CUESTIONARIOS que no cuenta ya con soporte. Estos cuestionarios son la base para la evaluación obligatoria de prácticas (RD592/14). El sistema quiere dotar a la aplicación Observatorio de un sistema de envío configurable, dirigible y que pueda cumplir las expectativas de los distintos títulos.',
   'Stopped','Large',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '5','Observatorio','5','89','2026-12-31',
   'https://drive.google.com/open?id=19IBulrEXEUE-uYeFKf238jGWqZOwQXfG',NULL),
  -- 14
  ('CPTI26-OO-02 - Iteración del proyecto',
   'Este proyecto incluye varias funcionalidades esenciales para el funcionamiento de las prácticas curriculares (8000 anuales), que afectan a más de 50 titulaciones: recoger las condiciones de los convenios y validarlas en las asignaciones de plazas, dar cabida a estudiantes FPO en el sistema, reforzar la información de las asignaciones en tiempo real e integrar tutores de prácticas clínicas hospitalarias.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '5','Observatorio','3','87','2026-04-30',
   'https://drive.google.com/open?id=1_H-riwj7oIzQjIb29Ht_B0lfhCUKnHpK',NULL),
  -- 15
  ('Formulario específico en Sede electrónica para la presentación de documentación de Becas del Ministerio',
   'El objetivo de este proyecto es implementar en Sede Electrónica un formulario específico para la presentación de documentación vinculada a la convocatoria de Becas del Ministerio de Educación, Formación Profesional y Deportes, evitando así la presentación a través de una instancia general.',
   'Completed','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '4','Academico','7',NULL,'2026-06-30',
   'https://drive.google.com/open?id=1deoeyplcmd8fFvpj6UtHl3tbSH2R5vbr',NULL),
  -- 16
  ('Módulo aplicación Becas Conselleria',
   'El objetivo de este proyecto es potenciar la ya existente aplicación de Becas de Conselleria, incorporando un nuevo módulo que permita integrar en dicha aplicación las nuevas convocatorias (Exención de Tasas, GVA Talent, Premios de Excelencia Académica y cualquier otra convocatoria sobrevenida).',
   'DevelopmentOutsideSprint','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '4','Academico','19','44','2026-09-30',
   'https://drive.google.com/open?id=1W3-kv_YP30mxTn4Q47DoLFUv1ZM4eByU',NULL),
  -- 17
  ('Módulo aplicación Becas y Ayudas UMH',
   'El objetivo de este proyecto es potenciar la ya existente aplicación de Becas y Ayudas UMH, incorporando un nuevo módulo que permita integrar en dicha aplicación las nuevas convocatorias (Santander Ayudas al Estudio, Santander Excelencia 360º y cualquier otra convocatoria sobrevenida).',
   'Stopped','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '4','Academico','15','46','2026-06-30',
   'https://drive.google.com/open?id=1Wzkb86nawtI7RDVyFs-DX8s_kiw37Ou4',NULL),
  -- 18
  ('CPTI26-OO-06 - Sistema de KPI y Cuadro de Mando Personalizable',
   'El Observatorio genera anualmente una gran cantidad de documentación y situaciones que requieren un control adecuado. Se propone la creación de un cuadro de mandos y el soporte a un sistema de BI que permita la monitorización y generación de informes ágiles.',
   'Stopped','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '3','Observatorio','7','92','2026-12-31',
   'https://drive.google.com/open?id=1JIfOOB6_vteGyfgECoGh10Iqf_MjkSnC',NULL),
  -- 19
  ('Módulo aplicación para la compensación de tasas por precios públicos',
   'El objetivo de este proyecto es crear una aplicación que permita realizar las distintas compensaciones económicas por precios públicos de las distintas administraciones: Compensación por Familias Numerosas, Compensación tasas Beca Ministerio, Compensación tasa Beca Generalitat Valenciana, Compensación tasas Ministerio parte no compensada.',
   'Stopped','Medium',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '4','Academico','21',NULL,'2026-09-30',
   'https://drive.google.com/open?id=1eAqNXCbHUZeY6G71ZPPFx2e4rpr31m3V',NULL),
  -- 20
  ('CPTI26-OO-08 - Gestión de Afiliación a la Seguridad Social',
   'El proyecto busca completar la implantación de la D.A.52 de la LGSS que dispone el alta en prácticas de estudiantes universitarios. Se pretende desarrollar un sistema de control del resultado de estas altas y bajas y poder determinar el impacto económico real de la medida.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Observatorio Ocupacional',
   '3','Observatorio','8','94','2026-12-31',
   'https://drive.google.com/open?id=17anjdAKJekzFC6h81KrDwUxyAVMUzgNo',NULL),
  -- 21
  ('Emisión de certificados de docencia y dirección de enseñanzas de formación permanente a través de la sede electrónica de la UMH.',
   'El objetivo de este proyecto es implementar los certificados de docencia y de dirección de las enseñanzas de formación permanente en la sede electrónica de la UMH.',
   'Stopped','Small',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '4','Academico','23','58','2026-10-15',
   'https://drive.google.com/open?id=1vNhpiQoEf2aDC1WEkvbBb_vEGLIACKyz',NULL),
  -- 22
  ('Reconocimiento de créditos entre los grados que conforman un doble grado.',
   'El objetivo de este proyecto consiste en adaptar la gestión de los Programas de Estudios Simultáneos (Dobles Grados) para permitir el reconocimiento de una asignatura o grupo de asignaturas de cualquiera de los grados mediante la superación de más de una asignatura del Doble Grado.',
   'DevelopmentOutsideSprint','Medium',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '5','Academico','11','60','2026-01-30',
   'https://drive.google.com/open?id=193DQEn7n6o4ywbxWLUZWAcuBkkeBZBH8',NULL),
  -- 23
  ('Formulario específico en Sede electrónica para la solicitud de expedición de duplicados de títulos oficiales',
   'Formulario específico para la solicitud de expedición de duplicados de títulos oficiales, evitando así la presentación a través de una instancia general.',
   'Stopped','Small',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '3','Academico','24',NULL,'2026-10-30',
   'https://drive.google.com/open?id=18h58FkYJXIiE9NB3bjqh1utqopnKdb02',NULL),
  -- 24
  ('Nueva aplicación para la gestión de los Títulos Oficiales',
   'El objetivo de este proyecto es terminar de desarrollar la nueva aplicación para la gestión de títulos oficiales de la UMH de la cual ya hay un proyecto avanzado. Con este proyecto se pretende dar respuesta a las distintas situaciones que en la gestión de los expedientes de títulos oficiales se presentan en el Servicio de Gestión de Estudios.',
   'Stopped','Medium',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '5','Academico','20','43','2026-04-30',
   'https://drive.google.com/open?id=1w3g-A60jxPDEhsuu5skIBVQj0D7cuY2o',NULL),
  -- 25
  ('Transferencia de los expedientes de solicitud de expedición de títulos',
   'El objetivo de este proyecto es transferir los expedientes de solicitud de expedición de títulos oficiales en SEDE desde los Centros de Gestión y Doctorado al SGE.',
   'Stopped','Small',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '5','Academico','9',NULL,'2026-04-30',
   'https://drive.google.com/open?id=1rEkmZ5qy84TEW6ZzJP2GqoGRc-umOa2b',NULL),
  -- 26
  ('Interoperabilidad de datos por recubrimiento',
   'El objetivo de este proyecto es que las aplicaciones de la UMH puedan consultar los datos en las correspondientes plataformas públicas disponibles, según el Catálogo de Servicios de Verificación y Consulta de datos SCSP, con el fin de dar cumplimiento al artículo 28.2 de la Ley 39/2015.',
   'Stopped','Large',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '5','Academico','14','31','2026-05-30',
   'https://drive.google.com/open?id=1theY-emMNcxlXzC5GbYReiTJjMXaTkMj',NULL),
  -- 27
  ('Modificación de matrícula',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de matrícula parcial y superación de matrícula máxima.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '3','Academico','26','34','2026-07-30',
   'https://drive.google.com/open?id=1ndGhtoBom6H0T1FkdHnsD17vo-PK8FXN',NULL),
  -- 28
  ('Traslado de expediente',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de traslado de expediente de grado y de pruebas de acceso a la universidad.',
   'Stopped','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '5','Academico','27','35','2026-06-30',
   'https://drive.google.com/open?id=1WbxjraIC9fzcOcieu0lDtn7U1s4AN_9P',NULL),
  -- 29
  ('PROTOCOLO GESTIÓN COBRO RECIBOS',
   'El control de impagos de las tasas de matrícula en los estudios de Grado y Máster Universitario es un proceso bastante manual y sería conveniente una automatización para automatizar el envío de avisos y recordatorios a los estudiantes con pagos pendientes.',
   'Stopped','Small',
   'Gerencia','CEGECA-San Juan',
   '3','Academico','13','20','2026-05-04',
   'https://drive.google.com/open?id=1lyX3WmOshhxz70PcZiE3t6twsEcZzwJI',NULL),
  -- 30
  ('Anulación de matrícula',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de anulación de matrícula voluntaria y por causa de fuerza mayor.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '3','Academico','28','36','2026-07-30',
   'https://drive.google.com/open?id=1hUW8kbIJYLBuSKMveQ5dm58wNptUiOHz',NULL),
  -- 31
  ('Aplicación matrícula selectividad',
   'El objetivo de este proyecto es obtener para cada exención y bonificación previstas en el Decreto 101/2024, de 2 de agosto, del Consell, por el que se regulan los precios públicos de los servicios académicos universitarios, el importe total de la matrícula de la PAU. Este dato es necesario para solicitar las compensaciones CNEA.',
   'InSprint','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '5','Academico','3',NULL,'2026-04-30',
   'https://drive.google.com/open?id=1DX9HdgubqqWyQhm8Wwx0uuMB354tB0XN',
   'https://cau-old.umh.es/browse/ACCESOG-1085'),
  -- 32
  ('Informe Subvenciones CNEA',
   'El objetivo de este proyecto es obtener un informe en formato Excel para solicitar la compensación por Costes Derivados de la Normativa Estatal y Autonómica. Anualmente la Dirección General de Universidades solicita a la Gerencia certificación firmada acreditativa de la compensación por los citados costes.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '5','Academico','1',NULL,'2026-04-30',
   'https://drive.google.com/open?id=19tRcsMEzmDgxwoBVQb4PdL5TGuoEY_Ne',NULL),
  -- 33
  ('Gestión eficiente de aulas',
   'Implantar un sistema sencillo, digital y verificable que garantice que las aulas reservadas en estancias de la Universidad Miguel Hernández de Elche (UMH) para clases y otras actividades sean efectivamente utilizadas, evitando bloqueos innecesarios y asegurando un uso eficiente de los espacios.',
   'Completed','Small',
   'Rectorado','Servicio de Innovación y Planificación Tecnológica',
   '5','WebTransversal','1',NULL,'2026-02-08',
   'https://drive.google.com/open?id=15LCfTdoAcy93AiWUiTe50RS7jIde3RxF',NULL),
  -- 34
  ('Modificaciones en Sexenios AVAP',
   'Solicitud de modificaciones en la aplicación de sexenios AVAP acorde con los cambios que presenta la aplicación de ANECA. Se incorporarán los cambios necesarios una vez la ANECA abra su aplicación.',
   'Completed','Small',
   'Vicerrectorado de Investigación y Transferencia','Vicerrectorado de Investigación y Transferencia',
   '5','RRHH','1',NULL,'2025-12-22',
   'https://drive.google.com/open?id=1Id9h5aTGjJ_CEYx9V1McDVN-hA-9vlLS',
   'https://cau-old.umh.es/browse/EXPEDIENTE-735'),
  -- 35
  ('Gestor de Equipos de Investigación Científica (GEIC)',
   'En la CPTI de 2025 se inició el desarrollo de un nuevo gestor de los equipos de investigación (GEIC) respondiendo a nuevas necesidades de seguimiento de utilización de los grandes equipos. En esta convocatoria se solicita ampliar funcionalidades y poner en producción GEIC para que esté a disposición de investigadores en 2026.',
   'Completed','Medium',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Apoyo Técnico a la Docencia y a la Investigación',
   '5','WebTransversal','6','45','2025-03-01',
   'https://drive.google.com/open?id=1D7ALIUuXBNj0LOtndzKd7r4RKmDuvwmF',NULL),
  -- 36
  ('Aplicación de registro de dedicación a proyectos',
   'Desarrollo de una aplicación para registro de dedicación a proyectos, con objeto de recoger las dedicaciones horarias del personal investigador necesarias para la gestión y justificación de los costes de personal de sus proyectos.',
   'Stopped','Medium',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Gestión de la Investigación',
   '5','InvestigacionEconomico','8',NULL,'2026-02-01',
   'https://drive.google.com/open?id=1c4dKcHkkIIt3IdZtltbwpTbIZOXk',NULL),
  -- 37
  ('MEJORA - SIGITT 2.0 JUSTIFICACIÓN ECONÓMICA DE PROYECTOS DE INVESTIGACIÓN',
   'Dotar a SIGITT 2.0 de los complementos y mejoras necesarios para su plena utilización e implementación definitiva. Se busca compendiar la información y simplificar las pantallas, completando la información para que esté toda disponible en el mismo aplicativo.',
   'Stopped','Large',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Gestión de la Investigación',
   '5','InvestigacionEconomico','5','91','2026-03-30',
   'https://drive.google.com/open?id=1l5EAy2H9C3Xh7FLEKfJOz8_RgZdv20qq',NULL),
  -- 38
  ('Mejoras en la aplicación del espacio opositor',
   'Por un lado, tiene como objetivo mejorar algunas características de la aplicación Espacio opositor. Por otro lado, agilizar las gestiones administrativas de las convocatorias eliminando las gestiones que se realizan en Hominis-Universitas XXI. Se pretende que la gestión se realice solo con el Gestor de expedientes y el espacio opositor.',
   'Stopped','VeryLarge',
   'Gerencia','Servicio de Personal Técnico, de Administración e Investigación',
   '4','Sede','5',NULL,'2026-12-31',
   'https://drive.google.com/open?id=19HRrLHtPB82PnKmhL-Yd0qiY0iGWDTwP',NULL),
  -- 39
  ('Migración de datos de UXXI Investigación a SIGITT2.0',
   'La aplicación UXXI Investigación de OCU, lleva años sin soporte, al igual que toda la infraestructura necesaria para que siga funcionando. El objetivo es la realización de la migración de datos de UXXI Investigación a SIGITT2.0, de forma que sea posible apagar definitivamente UXXI Investigación.',
   'Stopped','Medium',
   'Vicerrectorado de Infraestructuras','Servicio de Infraestructura Informática',
   '4','InvestigacionEconomico','7',NULL,'2026-12-31',
   'https://drive.google.com/open?id=1KbeNs0EW6ASas4HFguKE53cjMDCpeAab',NULL),
  -- 40
  ('Gestión Telefonía UMH',
   'Mejoras para la aplicación Gestión Telefonía UMH para una mejora en las solicitudes y visualización de contenidos.',
   'Completed','Small',
   'Vicerrectorado de Infraestructuras','Servicio de Infraestructura Informática',
   '4','WebTransversal','11','41','2026-01-12',
   'https://drive.google.com/open?id=1xsZdmrnEqRpF0BJVj9R7wVf8SwDCLh35',NULL),
  -- 41
  ('Certificados de traslado de la PAU Selectividad',
   'El objetivo de este proyecto es modificar la emisión de certificados de traslado de PAU, que se realiza desde la aplicación de Selectividad, de tal forma que se realice a través de la sede electrónica y obtener los certificados con sello de órgano.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '5','Academico','25','29','2026-05-30',
   'https://drive.google.com/open?id=1p7RPbUph9Czvm9yzFsAeH44Ilf2WmKE6',NULL),
  -- 42
  ('Compensaciones exceso horario',
   'El objetivo principal es desarrollar una nueva aplicación que permita el registro y cálculo automatizado de las horas extraordinarias realizadas y previamente autorizadas por los empleados, reemplazando el proceso manual actual.',
   'Stopped','Medium',
   'Gerencia','Servicio de Personal Técnico, de Administración e Investigación',
   '3','RRHH','7','8','2026-12-31',
   'https://drive.google.com/open?id=1hXswTq24FC3Uf8LyegI7vbQqcx1Et68G',NULL),
  -- 43
  ('Ausencias',
   'Desarrollo de una nueva aplicación de gestión de ausencias. La solicitud se basa en la necesidad de una nueva herramienta debido a la obsolescencia del sistema actual y al aumento de usuarios, tipos de permisos y autorizadores.',
   'Stopped','Large',
   'Gerencia','Servicio de Personal Técnico, de Administración e Investigación',
   '4','RRHH','6',NULL,'2026-12-31',
   'https://drive.google.com/open?id=1EDBIoo39vsvDay2bkBxWplyfB1a_K4D3',NULL),
  -- 44
  ('CERTIFICADO HISTÓRICO FORMACIÓN PTGAS',
   'Proyecto para la creación de un Certificado Histórico de las actividades formativas del Personal Técnico, de Gestión y de Administración y Servicios (PTGAS), accesible a través de la Sede Electrónica. Esta funcionalidad es una demanda histórica de los sindicatos.',
   'Stopped','VerySmall',
   'Gerencia','Servicio de Personal Técnico, de Administración e Investigación',
   '2','Academico','10',NULL,'2026-12-31',
   'https://drive.google.com/open?id=1TzCNpHhmVg9eJ7IdPpR6TiIdHygTK7mZ',NULL),
  -- 45
  ('Continuación de estudios',
   'El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de continuación de estudios por distintos motivos.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Gestión de Estudios',
   '4','Academico','29','33','2026-05-30',
   'https://drive.google.com/open?id=1MlcK1laeW64tWhz2BXE63lWuGue9Ot',NULL),
  -- 46
  ('Gestor de bolsas de trabajo',
   'Proyecto de desarrollo de un Gestor de bolsas de trabajo destinado al PTGAS para centralizar la gestión de las bolsas de empleo en una única aplicación automatizada, eliminando el riesgo de errores que conllevan los actuales procesos manuales.',
   'Stopped','Large',
   'Gerencia','Servicio de Personal Técnico, de Administración e Investigación',
   '4','RRHH','4','6','2026-12-31',
   'https://drive.google.com/open?id=1WHtksUZOj77o-SStQmWf_EyF8itYGzkv',NULL),
  -- 47
  ('Reserva de Instalaciones Deportivas UMH - Fase 2',
   'Concluida la Fase 1 del proyecto, se ha identificado un conjunto de funcionalidades complementarias necesarias para satisfacer plenamente los requerimientos de la Oficina de Campus Saludables y Deportes. Esta Fase 2 se orienta a la incorporación, mejora y finalización de estas funcionalidades pendientes.',
   'InSprint','Small',
   'Rectorado','Oficina de Deportes',
   '4','WebTransversal','5',NULL,'2026-01-15',
   'https://drive.google.com/open?id=1g171D8LWkDlhDjqRjWrFk1eT0h3rco7i',NULL),
  -- 48
  ('Mejoras de Escuela de Verano y Aula Junior',
   'El proyecto busca mejorar y agilizar la gestión de la Escuela de Verano y Aula Junior, reduciendo incidencias para usuarios internos y externos. Se optimizará el proceso de validación de la documentación requerida, actualmente gestionado por correo electrónico.',
   'PlanningWithClient','Medium',
   'Rectorado','Oficina de Deportes',
   '4','WebTransversal','4',NULL,'2026-03-15',
   'https://drive.google.com/open?id=15G5c4R-krVCMDDMm8_G8x10ZbYGsRh0R',NULL),
  -- 49
  ('Ticketing UMH',
   'Implantar mejoras en Ticketing UMH para beneficio de todos los técnicos de SII y SIPT.',
   'InSprint','Small',
   'Vicerrectorado de Infraestructuras','Servicio de Infraestructura Informática',
   '5','WebTransversal','10',NULL,'2026-03-31',
   'https://drive.google.com/open?id=1RWGRRiOKmBrByoePvB6S9QWuuYQ-wF9i',NULL),
  -- 50
  ('Modificaciones programa DOCENTIA_UMH',
   'El objetivo del proyecto es solicitar mejoras y nuevas funcionalidades en la aplicación del DOCENTIA. Estas modificaciones están motivadas para incorporar las recomendaciones del informe final de la comisión de implantación del diseño de la ANECA, así como por la adaptación del modelo al Programa de Apoyo para la Evaluación de la Calidad de la Actividad Docente.',
   'InSprint','Medium',
   'Vicerrectorado de Profesorado','Servicio de Profesorado, Nómina y Seguridad Social',
   '5','Academico','4',NULL,'2026-03-20',
   'https://drive.google.com/open?id=1jjk63sB0EgS7PEar9P-tpXWbuHQJVabI',
   'https://cau-old.umh.es/browse/CALIDEVAL-606'),
  -- 51
  ('Gestor de currículo',
   'Mejoras a desarrollar en la aplicación Gestor de Currículo, incluyendo la revisión y actualización de la normativa del Procedimiento de Evaluación de la Actividad Investigadora, Transferencia Tecnológica y Difusión de la Ciencia de la Universidad Miguel Hernández.',
   'Stopped','Medium',
   'Vicerrectorado de Investigación y Transferencia','Vicerrectorado de Investigación y Transferencia',
   '5','InvestigacionEconomico','9',NULL,'2026-09-30',
   'https://drive.google.com/open?id=1j6Wjjoh_WkLS68B-uM4PqiL4gQUkx-zy',NULL),
  -- 52
  ('Comprobación Documental',
   'Mejoras a desarrollar en la aplicación Comprobación Documental, con la finalidad de facilitar el proceso de validación de méritos que se realiza a los investigadores de la universidad.',
   'Stopped','Small',
   'Vicerrectorado de Investigación y Transferencia','Vicerrectorado de Investigación y Transferencia',
   '3','InvestigacionEconomico','10',NULL,'2026-09-30',
   'https://drive.google.com/open?id=11q47pHy21EOHGf4UvFtnO-VLnVmTodm5',NULL),
  -- 53
  ('Grupos de investigación',
   'Mejoras a desarrollar en la aplicación Grupos Investigación, incluyendo la revisión y actualización de la normativa del Reglamento de Grupos de Investigación de la Universidad Miguel Hernández, cuya última actualización es de noviembre de 2014.',
   'PlanningSprint','Medium',
   'Vicerrectorado de Investigación y Transferencia','Vicerrectorado de Investigación y Transferencia',
   '5','InvestigacionEconomico','11','84','2025-09-30',
   'https://drive.google.com/open?id=1oulH-G3k_7H2JBEZ9YlXLK4EuVzGO9gA',
   'https://cau-old.umh.es/browse/INVESTIGA-3424'),
  -- 54
  ('Reingeniería Gestor de CV',
   'Desarrollar una nueva interfaz respetando el flujo actual de la aplicación. La prioridad será generar una interfaz modular para poder cambiarla en un futuro y reutilizar los módulos creados en esta reingeniería. Se creará un frontend y un backend para desacoplar funcionalidades.',
   'Stopped','Large',
   'Vicerrectorado de Investigación y Transferencia','Vicerrectorado de Investigación y Transferencia',
   '4','InvestigacionEconomico','13',NULL,'2026-09-30',
   'https://drive.google.com/open?id=1uFwyjndFxzn50ki8RmEuKa1qOQ-SIP7s',NULL),
  -- 55
  ('Certificados de Investigación',
   'El objeto de este proyecto es la expedición a través de la SEDE electrónica de la UMH de los certificados de investigación que actualmente se expiden manualmente.',
   'DevelopmentOutsideSprint','Medium',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Transferencia de Conocimiento',
   '5','InvestigacionEconomico','1','76','2026-01-31',
   'https://drive.google.com/open?id=19qvq3S2Uets36cv5PnwfiRM6pgm_1RuJ',NULL),
  -- 56
  ('Simplificación de procedimiento de prestaciones de servicio',
   'El objeto de este proyecto es simplificar el procedimiento de prestaciones de servicio, tanto periódicas como no-periódicas. Para ello, se solicita una nueva aplicación llamada Prestaciones de Servicio, parecida al frontal de la aplicación Documentos de pago, donde se pueda ver dónde se encuentra el trámite.',
   'DevelopmentOutsideSprint','Large',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Transferencia de Conocimiento',
   '5','InvestigacionEconomico','2','69','2026-03-31',
   'https://drive.google.com/open?id=1iT85DEIpY5yeeeSCi_wkVI3t6a2zHoZr',NULL),
  -- 57
  ('Mejoras aplicación de preinscripción',
   'El objetivo de este proyecto es mejorar la aplicación de preinscripción, teniendo en cuenta las necesidades que se han detectado en el último curso académico.',
   'WaitingForDevelopers','Large',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '5','Academico','17','37','2026-03-15',
   'https://drive.google.com/open?id=1Nq0B5llzp9d0lOBh_21KgkG1w3ZyN58n',
   'https://cau-old.umh.es/browse/ACCESOP-441'),
  -- 58
  ('Revisión de la integración UXXI-SIGITT2',
   'El objeto de este proyecto es abordar algunos problemas con la integración UXXI-SIGITT2 que dificultan el día a día con SIGITT.',
   'Stopped','Small',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Transferencia de Conocimiento',
   '3','InvestigacionEconomico','4','74','2026-06-01',
   'https://drive.google.com/open?id=1RK06IPrFS0NXCjUVZLg9mGQ4l5vJnsXM',NULL),
  -- 59
  ('Mejoras aplicación certificados TFM',
   'El objetivo de este proyecto es mejorar la aplicación de Certificado TFM, adecuándola a las necesidades que se han detectado una vez se ha puesto en funcionamiento.',
   'Stopped','Medium',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '4','Academico','22',NULL,'2026-07-30',
   'https://drive.google.com/open?id=12GoWqQ0ztS3CkFl1fUyXe2UuuLw49Bcn',NULL),
  -- 60
  ('Mejoras de rendimiento en SIGITT2',
   'El objeto de este proyecto es mejorar el rendimiento de la aplicación SIGITT2 puesto que los gestores de STC y SGI pierden mucho tiempo mientras se cargan o almacenan los datos.',
   'DevelopmentOutsideSprint','Small',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Transferencia de Conocimiento',
   '5','InvestigacionEconomico','3','78','2026-02-01',
   'https://drive.google.com/open?id=1Uhc1BFGFRwbjUjVeUSodgvraK1CHg6uL',NULL),
  -- 61
  ('Revisión de la integración GestorCV-SIGITT2',
   'El objeto de este proyecto es abordar algunos problemas con la integración GestorCV-SIGITT2. Corrige errores de criterios y extractores en el módulo de IPR.',
   'Stopped','Small',
   'Vicerrectorado de Investigación y Transferencia','Servicio de Transferencia de Conocimiento',
   '3','InvestigacionEconomico','6','70','2026-06-01',
   'https://drive.google.com/open?id=1x-FBvytF5frArc8IPR7dV9jNl5h',NULL),
  -- 62
  ('Mejora aplicación Retribuciones Adicionales',
   'Mejora de la aplicación de méritos autonómicos, introduciendo comprobación de los límites y ampliando la posibilidad a personal contratado.',
   'Completed','Medium',
   'Vicerrectorado de Profesorado','Servicio de Profesorado, Nómina y Seguridad Social',
   '5','RRHH','3',NULL,'2026-02-28',
   'https://drive.google.com/open?id=1YTTsPBm1yo_Jarngx_kRQGmyFLidCZVv',
   'https://cau-old.umh.es/browse/SUELDO-1061'),
  -- 63
  ('MEJORAS EN GISBAP',
   'Mejorar la aplicación de gestión de subvenciones (GISBAP) incluyendo nuevas funcionalidades que permitan la simplificación administrativa y que agilicen la realización del trabajo. Se requiere disponer integraciones con servicios de intermediación de datos y con el sistema económico UXXI.',
   'Stopped','Medium',
   'Secretaría General','Servicio de Modernización y Coordinación Administrativa',
   '5','Sede','3',NULL,'2026-03-31',
   'https://drive.google.com/open?id=1PWJ97JusHmUlWdwgYL7dVEsQFYhlkkAn',NULL),
  -- 64
  ('MEJORAS GESTIÓN DE CONVENIOS UMH',
   'Mejorar la aplicación Gestión de convenios UMH con las solicitudes de mejora que se han detectado sobre la versión inicial tras la puesta en producción.',
   'Stopped','Medium',
   'Secretaría General','Servicio de Modernización y Coordinación Administrativa',
   '5','Sede','4',NULL,'2026-05-31',
   'https://drive.google.com/open?id=1xNDiVC7pI8g5775r4D0H5nXtonu4myk6',NULL),
  -- 65
  ('Acceso a la Sede electrónica para extranjeros y usuarios externos',
   'El sistema de Autenticación de Ciudadano UE (eIDAS) permite a los ciudadanos de un país de la Unión Europea usar su identificación electrónica nacional para acceder a servicios públicos en otros países miembros. Se solicita la integración con el nodo eIDAS español a través del sistema Cl@ve y el Registro de Usuarios Externos (RUE).',
   'PlanningWithClient','Large',
   'Secretaría General','Servicio de Modernización y Coordinación Administrativa',
   '5','WebTransversal','2','26','2026-03-31',
   'https://drive.google.com/open?id=1TIKLtd68eewoCnJ2f0sjVBC_AdtSt2fM',NULL),
  -- 66
  ('HISTÓRICO INFORMACIÓN DE REGISTRO GENERAL (MASTIN)',
   'La aplicación Mastin lleva años sin soporte, al igual que toda la infraestructura necesaria para que siga funcionando. Se propone el diseño de una aplicación o conector ODBC para acceso a registros históricos de Mastín no migrados a Geiser.',
   'Completed','Small',
   'Secretaría General','Servicio de Modernización y Coordinación Administrativa',
   '4','Sede','10',NULL,'2025-09-30',
   'https://drive.google.com/open?id=1R1JWvNyXB5UidT5YkZvpSU75P3CdFMNk',NULL),
  -- 67
  ('Web de reserva de actividades para centros de educación secundaria y bachillerato',
   'El objeto de este proyecto es contar con una web de gestión de reserva, para que los centros de educación secundaria puedan reservar las actividades que la UMH les ofrece, dentro de los programas de difusión y captación, tales como charlas informativas, visitas a los campus, talleres divulgativos y jornadas de conferencias.',
   'Stopped','Medium',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Comunicación, Márketing y Atención al Estudiantado',
   '5','WebTransversal','8','68','2026-07-01',
   'https://drive.google.com/open?id=18gsoOaBrH0kZkeLFW7wqkeVjJ6jKobqJ',NULL),
  -- 68
  ('MEJORAS GESTOR DE EXPEDIENTES Y PORTAFIRMAS UMH',
   'Mejorar la aplicación Gestor de Expedientes UMH y la aplicación Portafirmas UMH con las solicitudes de mejora recibidas y que han sido calificadas con prioridad crítica.',
   'Stopped','VeryLarge',
   'Secretaría General','Servicio de Modernización y Coordinación Administrativa',
   '5','Sede','2',NULL,'2026-09-30',
   'https://drive.google.com/open?id=1rllo5vbtdk8B9m0AjckHkn_ilsZNmMeM',NULL),
  -- 69
  ('ERASMUS WITHOUT PAPER (Acuerdo Académicos conectar con la EWP)',
   'Una vez finalizada la fase 2 de los modelos de cambios y la firma de los acuerdos académicos, la parte de la aplicación destinada a la gestión de acuerdos académicos deberá estar conectada a la red EWP. Esto permitirá gestionar en línea los acuerdos de aprendizaje entre los países miembros de la UE y los terceros países asociados al Programa Erasmus+.',
   'DevelopmentOutsideSprint','Medium',
   'Vicerrectorado de Internacionalización y Cooperación','Servicio de Relaciones Internacionales y Cooperación',
   '5','Academico','2',NULL,'2026-05-31',
   'https://drive.google.com/open?id=1-y5z9wOx_cTIIVlXeUhauvzyteUK2Yxi',NULL),
  -- 70
  ('ERASMUS WITHOUT PAPER (TRANSCRIPT OF RECORDS- Conectar con la EWP)',
   'Una vez desarrollada esta parte de la aplicación de acuerdos académicos deberemos estar conectados a la red EWP para gestionar los Transcript of Records entre los países miembros de la UE y terceros países asociados al Programa Erasmus+.',
   'Stopped','Medium',
   'Vicerrectorado de Internacionalización y Cooperación','Servicio de Relaciones Internacionales y Cooperación',
   '5','Academico','5',NULL,'2025-06-30',
   'https://drive.google.com/open?id=1yr70v-r5MClnDlDqL-bd0R1IS6zN5KtC',NULL),
  -- 71
  ('MOVILIDAD- Propia Estudiante Visitante PARA ESTUDIOS UMH',
   'Procedimiento de admisión estudiante visitante en la UMH: procedimiento para la admisión de estudiantes visitantes que deseen cursar alguna asignatura y/o realizar un periodo de actividades tuteladas en la UMH, fuera de los programas oficiales de movilidad, acuerdos o convenios de intercambio interuniversitario.',
   'Stopped','Medium',
   'Vicerrectorado de Internacionalización y Cooperación','Servicio de Relaciones Internacionales y Cooperación',
   '4','Academico','12',NULL,'2026-09-30',
   'https://drive.google.com/open?id=1QWM5uVvu307hxOSDUtjPQNh0SYnQt1XP',NULL),
  -- 72
  ('MOVILIDAD-REPOSITORIO DE DOCUMENTACIÓN PARA EL ESTUDIANTADO QUE PARTICIPA EN PROGRAMAS DE MOVILIDAD',
   'La creación de un repositorio de documentación para el estudiantado que tenga una plaza de movilidad aceptada dentro de un programa de movilidad.',
   'Stopped','Medium',
   'Vicerrectorado de Internacionalización y Cooperación','Servicio de Relaciones Internacionales y Cooperación',
   '5','Academico','16','50','2026-05-30',
   'https://drive.google.com/open?id=1IFDytu2QJP1tBNEy5vovKf1Ozy8UxlNi',NULL),
  -- 73
  ('MOVILIDAD- REPOSITORIO DE DOCUMENTACIÓN PARA CONVOCATORIAS DE MOVILIDAD DE PDI Y PTGAS',
   'Utilizar los documentos subidos en el espacio opositor para las convocatorias de movilidad internacional del PDI y PTGAS.',
   'Stopped','Medium',
   'Vicerrectorado de Internacionalización y Cooperación','Servicio de Relaciones Internacionales y Cooperación',
   '4','Academico','18','51','2026-09-30',
   'https://drive.google.com/open?id=1vh5tlDUkzq1Ai88qDje7im8qnB6_1a95',NULL),
  -- 74
  ('APLICACIÓN PARA PROYECTOS INTERNACIONALES Y COOPERACIÓN AL DESARROLLO',
   'Necesitamos que se nos adapte el módulo de SIGITT2.0 para la gestión de proyectos internacionales y de cooperación al desarrollo (CID) que se gestionan en el Servicio de Relaciones Internacionales y Cooperación.',
   'Stopped','Large',
   'Vicerrectorado de Internacionalización y Cooperación','Servicio de Relaciones Internacionales y Cooperación',
   '5','InvestigacionEconomico','12',NULL,'2026-06-30',
   'https://drive.google.com/open?id=1wvc4j48lZm6rKitiQAWLgNIYHanSBQ9F',NULL),
  -- 75
  ('Plataforma de inscripciones para Vida UMH',
   'Se trata de diseñar una plataforma de gestión de las inscripciones en las actividades de Vida UMH, tanto las gratuitas como las de pago.',
   'Stopped','Small',
   'Vicerrectorado de Estudiantes y Coordinación','Servicio de Comunicación, Márketing y Atención al Estudiantado',
   '5','WebTransversal','7',NULL,'2026-06-01',
   'https://drive.google.com/open?id=1JfxPnyVblECV3xI0Ojhego5XDo2DZX9z',NULL),
  -- 76
  ('Mejoras SII Aplicación Compras Menores para solicitud a Cartera Proyectos TI 2026 de la UMH',
   'Solicitud de mejoras y adaptación de la aplicación de Compras menores para agilizar el flujo de comunicaciones entre el Autorizador y Peticionario.',
   'Completed','Small',
   'Gerencia','Servicio de Infraestructura Informática',
   '4','Sede','8',NULL,'2026-03-19',
   'https://drive.google.com/open?id=1PRBVwPdxbVqW22_lgG98W7WpjS8f28HI',NULL),
  -- 77
  ('Mejoras SII Aplicación Pedidos para solicitud a Cartera Proyectos TI 2026 de la UMH',
   'Mejoras y adaptación de la Aplicación de Pedidos para mejorar la comunicación con los proveedores del nuevo acuerdo marco de Servicios TIC que se iniciará a principios del 2026, permitiendo agilizar el flujo de comunicaciones entre el Peticionario y las empresas proveedoras.',
   'InTesting','Medium',
   'Gerencia','Servicio de Infraestructura Informática',
   '5','Sede','7',NULL,'2026-02-14',
   'https://drive.google.com/open?id=1mRUMBK7qpbrDHrtFNbrawKkesPpAd-fc',NULL),
  -- 78
  ('Mejoras Applicación de Seguimiento de Doctorado y Trámites Depósito de Tesis',
   'El objetivo de este proyecto es continuar con la mejora de la aplicación de Seguimiento de Doctorados iniciada en la Cartera de Proyectos de 2025 y que no se ha podido terminar de implementar por la complejidad de su realización. Persigue la internacionalización de la Escuela de Doctorado con el acceso de evaluadores, tribunales y estudiantes extranjeros.',
   'DevelopmentOutsideSprint','Large',
   'Vicerrectorado de Investigación y Transferencia','Escuela de Doctorado',
   '5','Academico','8','81','2025-03-01',
   'https://drive.google.com/open?id=1dTh1UwC_DVUJ5-xdqAj9eZBMXShuUwp8',NULL),
  -- 79
  ('Mejoras aplicación gestión TFG/M',
   'El objetivo general del proyecto es la mejora de la aplicación de gestión del TFG/M. Se busca centralizar toda la información relativa a los TFG/M de cara a procesos de acreditación y mejorar la usabilidad para reducir las dudas e incidencias que surgen sobre el manejo de la aplicación.',
   'Stopped','Small',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   '5','Academico','30',NULL,'2026-09-01',
   'https://drive.google.com/open?id=1bRiToVJ53pOLlfOWH1-9lGb4pHJSUwp8',NULL),
  -- 80
  ('REGISTRO DE APODERAMIENTOS Y REGISTRO DE FUNCIONARIOS HABILITADOS UMH',
   'El Registro Electrónico de Apoderamiento de la Administración General del Estado permitirá hacer constar las representaciones que los ciudadanos otorguen a terceros para actuar en su nombre de forma electrónica ante la UMH, la AGE y sus organismos públicos.',
   'Stopped','Medium',
   'Secretaría General','Servicio de Modernización y Coordinación Administrativa',
   '5','Sede','9','14','2026-02-28',
   'https://drive.google.com/open?id=1LaeeIogRcebCPx7rha9bD4ArxfliQtmw',NULL),
  -- 81
  ('Herramienta gestión expedientes SDA',
   'Se precisa de una herramienta para la correcta tramitación de los expedientes de contratación por sistema dinámico de adquisición.',
   'DevelopmentOutsideSprint','VeryLarge',
   'Gerencia','Servicio de Planificación y Racionalización de la Contratación',
   '5','Sede','1',NULL,'2026-12-31',
   'https://drive.google.com/open?id=1pvhtYi4YP6LMDENwU0qHapA6a0WnM84z',NULL),
  -- 82
  ('Automatización de las inscripciones de SABIEX',
   'Migrar SABIEX a Fórmate y configurar este tipo de enseñanza como un título propio. Se busca actualizar y homogeneizar la forma de matrícula en las enseñanzas no regladas.',
   'DevelopmentOutsideSprint','Small',
   'Vicerrectorado de Cultura, Igualdad y Diversidad','Oficina de Cultura e Igualdad',
   '5','Academico','1',NULL,'2026-09-15',
   NULL,NULL),
  -- 83
  ('Elecciones Delegado General de Estudiantes 2026',
   'Elecciones Delegado General de Estudiantes 2026.',
   'Completed','VerySmall',
   'Secretaría General','Junta Electoral',
   NULL,'RRHH','3',NULL,'2026-05-08',
   NULL,'https://cau-old.umh.es/browse/ELECCIONES-335'),
  -- 84
  ('Recibo reserva de plaza preinscripción máster',
   'Se debe abonar un recibo reserva de plaza preinscripción máster para poder matricularse en el máster.',
   'WaitingForDevelopers','Small',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   NULL,'Academico','3',NULL,'2026-07-01',
   NULL,'https://cau-old.umh.es/browse/ACCESOP-476'),
  -- 85
  ('Asignaturas equivalentes en Campus Virtual',
   'El proyecto tiene como objetivo facilitar la gestión conjunta de asignaturas equivalentes en el campus virtual. Para ello, se definirá por curso académico qué asignaturas y bloques se consideran equivalentes, de manera que la asignatura principal agrupe y gestione el conjunto de asignaturas asociadas.',
   'DevelopmentOutsideSprint','Medium',
   'Vicerrectorado de Estudios','Servicio de Gestión de Estudios',
   NULL,'Academico','1',NULL,'2026-06-01',
   'https://drive.google.com/drive/folders/13_4NVtjkVof6x-qdeUuV0p-l7x3hlVzt',
   'https://cau-old.umh.es/browse/DOCENCIAV-632')

) AS v(title, description, status, complexity, promoter_name, ou_name,
       gp, sg, uor, prev, ddate, specs, epic)
JOIN "Promoters"    pr ON pr."Name" = v.promoter_name
JOIN "OrganicUnits" ou ON ou."Name" = v.ou_name
WHERE NOT EXISTS (
  SELECT 1 FROM "Projects" p
  WHERE p."Title" = v.title AND p."PortfolioYear" = 2026
);

-- ─── 7. ASIGNACIONES PROYECTO → EQUIPO ───────────────────────────────────────

INSERT INTO "ProjectTeamAssignments" ("ProjectId", "TeamId", "IsPrimary")
SELECT p."Id", t."Id", true
FROM (VALUES
  ('PERMISOS 2.0: Nueva interfaz web aplicación de Permisos',                                                                    'WEB/TRANSVERSAL'),
  ('Automatización de la generación de los censos para centros e institutos de investigación',                                   'RRHH'),
  ('Propuesta mejora de la aplicación de Reserva de Estancias',                                                                  'WEB/TRANSVERSAL'),
  ('Adecuación parcial de la aplicación gestión-red',                                                                            'WEB/TRANSVERSAL'),
  ('MIGRACIÓN DE TIPOS DE ENSEÑANZA NO REGLADA DE CULTURA A LA PLATAFORMA FÓRMATE',                                             'ACADEMICO'),
  ('Mejoras SATDI Aplicación de Pedidos',                                                                                        'SEDE'),
  ('MEJORAS EN LA APLICACIÓN DE PLAN DIRECTOR',                                                                                  'RRHH'),
  ('Migración Sistema de Encuestas Calidad de Moodle a LimeSurvey',                                                              'WEB/TRANSVERSAL'),
  ('CPTI26-00- Plantillas (II)',                                                                                                  'OBSERVATORIO'),
  ('CPTI26-OO-01- Gestor de Anexos II',                                                                                          'OBSERVATORIO'),
  ('CPTI26-OO-03 - Prácticas Internas',                                                                                          'OBSERVATORIO'),
  ('CPTI26-OO-05 Gestión de Ofertas de Empleo para Titulados Oficiales',                                                         'OBSERVATORIO'),
  ('CPTI26-OO-04 - Cuestionarios',                                                                                               'OBSERVATORIO'),
  ('CPTI26-OO-02 - Iteración del proyecto',                                                                                      'OBSERVATORIO'),
  ('Formulario específico en Sede electrónica para la presentación de documentación de Becas del Ministerio',                    'ACADEMICO'),
  ('Módulo aplicación Becas Conselleria',                                                                                        'ACADEMICO'),
  ('Módulo aplicación Becas y Ayudas UMH',                                                                                      'ACADEMICO'),
  ('CPTI26-OO-06 - Sistema de KPI y Cuadro de Mando Personalizable',                                                            'OBSERVATORIO'),
  ('Módulo aplicación para la compensación de tasas por precios públicos',                                                       'ACADEMICO'),
  ('CPTI26-OO-08 - Gestión de Afiliación a la Seguridad Social',                                                                'OBSERVATORIO'),
  ('Emisión de certificados de docencia y dirección de enseñanzas de formación permanente a través de la sede electrónica de la UMH.', 'ACADEMICO'),
  ('Reconocimiento de créditos entre los grados que conforman un doble grado.',                                                  'ACADEMICO'),
  ('Formulario específico en Sede electrónica para la solicitud de expedición de duplicados de títulos oficiales',               'ACADEMICO'),
  ('Nueva aplicación para la gestión de los Títulos Oficiales',                                                                  'ACADEMICO'),
  ('Transferencia de los expedientes de solicitud de expedición de títulos',                                                     'ACADEMICO'),
  ('Interoperabilidad de datos por recubrimiento',                                                                               'ACADEMICO'),
  ('Modificación de matrícula',                                                                                                  'ACADEMICO'),
  ('Traslado de expediente',                                                                                                     'ACADEMICO'),
  ('PROTOCOLO GESTIÓN COBRO RECIBOS',                                                                                           'ACADEMICO'),
  ('Anulación de matrícula',                                                                                                     'ACADEMICO'),
  ('Aplicación matrícula selectividad',                                                                                          'ACADEMICO'),
  ('Informe Subvenciones CNEA',                                                                                                  'ACADEMICO'),
  ('Gestión eficiente de aulas',                                                                                                 'WEB/TRANSVERSAL'),
  ('Modificaciones en Sexenios AVAP',                                                                                           'RRHH'),
  ('Gestor de Equipos de Investigación Científica (GEIC)',                                                                       'WEB/TRANSVERSAL'),
  ('Aplicación de registro de dedicación a proyectos',                                                                          'INVESTIGACIÓN/ECONOMICO'),
  ('MEJORA - SIGITT 2.0 JUSTIFICACIÓN ECONÓMICA DE PROYECTOS DE INVESTIGACIÓN',                                                  'INVESTIGACIÓN/ECONOMICO'),
  ('Mejoras en la aplicación del espacio opositor',                                                                              'SEDE'),
  ('Migración de datos de UXXI Investigación a SIGITT2.0',                                                                       'INVESTIGACIÓN/ECONOMICO'),
  ('Gestión Telefonía UMH',                                                                                                     'WEB/TRANSVERSAL'),
  ('Certificados de traslado de la PAU Selectividad',                                                                           'ACADEMICO'),
  ('Compensaciones exceso horario',                                                                                              'RRHH'),
  ('Ausencias',                                                                                                                  'RRHH'),
  ('CERTIFICADO HISTÓRICO FORMACIÓN PTGAS',                                                                                     'ACADEMICO'),
  ('Continuación de estudios',                                                                                                   'ACADEMICO'),
  ('Gestor de bolsas de trabajo',                                                                                                'RRHH'),
  ('Reserva de Instalaciones Deportivas UMH - Fase 2',                                                                          'WEB/TRANSVERSAL'),
  ('Mejoras de Escuela de Verano y Aula Junior',                                                                                 'WEB/TRANSVERSAL'),
  ('Ticketing UMH',                                                                                                             'WEB/TRANSVERSAL'),
  ('Modificaciones programa DOCENTIA_UMH',                                                                                      'ACADEMICO'),
  ('Gestor de currículo',                                                                                                       'INVESTIGACIÓN/ECONOMICO'),
  ('Comprobación Documental',                                                                                                   'INVESTIGACIÓN/ECONOMICO'),
  ('Grupos de investigación',                                                                                                   'INVESTIGACIÓN/ECONOMICO'),
  ('Reingeniería Gestor de CV',                                                                                                 'INVESTIGACIÓN/ECONOMICO'),
  ('Certificados de Investigación',                                                                                             'INVESTIGACIÓN/ECONOMICO'),
  ('Simplificación de procedimiento de prestaciones de servicio',                                                               'INVESTIGACIÓN/ECONOMICO'),
  ('Mejoras aplicación de preinscripción',                                                                                      'ACADEMICO'),
  ('Revisión de la integración UXXI-SIGITT2',                                                                                   'INVESTIGACIÓN/ECONOMICO'),
  ('Mejoras aplicación certificados TFM',                                                                                       'ACADEMICO'),
  ('Mejoras de rendimiento en SIGITT2',                                                                                         'INVESTIGACIÓN/ECONOMICO'),
  ('Revisión de la integración GestorCV-SIGITT2',                                                                               'INVESTIGACIÓN/ECONOMICO'),
  ('Mejora aplicación Retribuciones Adicionales',                                                                               'RRHH'),
  ('MEJORAS EN GISBAP',                                                                                                        'SEDE'),
  ('MEJORAS GESTIÓN DE CONVENIOS UMH',                                                                                         'SEDE'),
  ('Acceso a la Sede electrónica para extranjeros y usuarios externos',                                                         'WEB/TRANSVERSAL'),
  ('HISTÓRICO INFORMACIÓN DE REGISTRO GENERAL (MASTIN)',                                                                        'SEDE'),
  ('Web de reserva de actividades para centros de educación secundaria y bachillerato',                                         'WEB/TRANSVERSAL'),
  ('MEJORAS GESTOR DE EXPEDIENTES Y PORTAFIRMAS UMH',                                                                          'SEDE'),
  ('ERASMUS WITHOUT PAPER (Acuerdo Académicos conectar con la EWP)',                                                            'ACADEMICO'),
  ('ERASMUS WITHOUT PAPER (TRANSCRIPT OF RECORDS- Conectar con la EWP)',                                                        'ACADEMICO'),
  ('MOVILIDAD- Propia Estudiante Visitante PARA ESTUDIOS UMH',                                                                  'ACADEMICO'),
  ('MOVILIDAD-REPOSITORIO DE DOCUMENTACIÓN PARA EL ESTUDIANTADO QUE PARTICIPA EN PROGRAMAS DE MOVILIDAD',                      'ACADEMICO'),
  ('MOVILIDAD- REPOSITORIO DE DOCUMENTACIÓN PARA CONVOCATORIAS DE MOVILIDAD DE PDI Y PTGAS',                                   'ACADEMICO'),
  ('APLICACIÓN PARA PROYECTOS INTERNACIONALES Y COOPERACIÓN AL DESARROLLO',                                                    'INVESTIGACIÓN/ECONOMICO'),
  ('Plataforma de inscripciones para Vida UMH',                                                                                'WEB/TRANSVERSAL'),
  ('Mejoras SII Aplicación Compras Menores para solicitud a Cartera Proyectos TI 2026 de la UMH',                               'SEDE'),
  ('Mejoras SII Aplicación Pedidos para solicitud a Cartera Proyectos TI 2026 de la UMH',                                       'SEDE'),
  ('Mejoras Applicación de Seguimiento de Doctorado y Trámites Depósito de Tesis',                                             'ACADEMICO'),
  ('Mejoras aplicación gestión TFG/M',                                                                                         'ACADEMICO'),
  ('REGISTRO DE APODERAMIENTOS Y REGISTRO DE FUNCIONARIOS HABILITADOS UMH',                                                    'SEDE'),
  ('Herramienta gestión expedientes SDA',                                                                                      'SEDE'),
  ('Automatización de las inscripciones de SABIEX',                                                                            'ACADEMICO'),
  ('Elecciones Delegado General de Estudiantes 2026',                                                                          'RRHH'),
  ('Recibo reserva de plaza preinscripción máster',                                                                             'ACADEMICO'),
  ('Asignaturas equivalentes en Campus Virtual',                                                                               'ACADEMICO')
) AS a(project_title, team_name)
JOIN "Projects" p ON p."Title" = a.project_title AND p."PortfolioYear" = 2026
JOIN "Teams"    t ON t."Name"  = a.team_name
ON CONFLICT ("ProjectId", "TeamId") DO NOTHING;

COMMIT;

-- ─── RESUMEN ─────────────────────────────────────────────────────────────────

SELECT
  (SELECT COUNT(*) FROM "Persons")               AS personas,
  (SELECT COUNT(*) FROM "Teams")                 AS equipos,
  (SELECT COUNT(*) FROM "PersonTeamMemberships") AS membresias,
  (SELECT COUNT(*) FROM "Promoters")             AS promotores,
  (SELECT COUNT(*) FROM "OrganicUnits")          AS unidades,
  (SELECT COUNT(*) FROM "Projects")              AS proyectos,
  (SELECT COUNT(*) FROM "ProjectTeamAssignments") AS asignaciones;
