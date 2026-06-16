using CarteraProyectos.Core.Domain;
using Microsoft.EntityFrameworkCore;
using static CarteraProyectos.Core.Domain.ProjectStatus;
using static CarteraProyectos.Core.Domain.ProjectComplexity;
using static CarteraProyectos.Core.Domain.SiptGroup;

namespace CarteraProyectos.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Projects.AnyAsync()) return;

        // --- Promotores ---
        var prom = new Dictionary<string, Promoter>();
        foreach (var n in new[]
        {
            "Vicerrectorado de Infraestructuras",
            "Vicerrectorado de Investigación y Transferencia",
            "Vicerrectorado de Cultura, Igualdad y Diversidad",
            "Gerencia",
            "Vicerrectorado de Estudiantes y Coordinación",
            "Vicerrectorado de Estudios",
            "Rectorado",
            "Secretaría General",
            "Vicerrectorado de Internacionalización y Cooperación",
            "Vicerrectorado de Profesorado"
        })
        { var e = Promoter.Create(n); db.Promoters.Add(e); prom[n] = e; }

        // --- Unidades Orgánicas ---
        var unit = new Dictionary<string, OrganicUnit>();
        foreach (var n in new[]
        {
            "Servicio de Innovación y Planificación Tecnológica",
            "Junta Electoral",
            "Servicio de Infraestructura Informática",
            "Oficina de Cultura e Igualdad",
            "Servicio de Personal Técnico, de Administración e Investigación",
            "Servicio de Calidad",
            "Observatorio Ocupacional",
            "Servicio de Gestión de Estudios",
            "Vicerrectorado de Investigación y Transferencia",
            "Servicio de Gestión de la Investigación",
            "Servicio de Apoyo Técnico a la Docencia y a la Investigación",
            "Servicio de Transferencia de Conocimiento",
            "Servicio de Modernización y Coordinación Administrativa",
            "Servicio de Relaciones Internacionales y Cooperación",
            "Servicio de Comunicación, Márketing y Atención al Estudiantado",
            "Oficina de Deportes",
            "Servicio de Profesorado, Nómina y Seguridad Social",
            "CEGECA-San Juan",
            "Escuela de Doctorado",
            "Servicio de Planificación y Racionalización de la Contratación"
        })
        { var e = OrganicUnit.Create(n); db.OrganicUnits.Add(e); unit[n] = e; }

        // --- Tags ---
        foreach (var (name, color) in new[] {
            ("Prioritario", "#f5222d"), ("Migración", "#fa8c16"), ("Nueva funcionalidad", "#52c41a"),
            ("Mantenimiento", "#1890ff"), ("Seguridad", "#722ed1"), ("Accesibilidad", "#13c2c2"),
            ("Deuda técnica", "#eb2f96"), ("Integración", "#faad14") })
        { db.Tags.Add(Tag.Create(name, color)); }

        await db.SaveChangesAsync();

        static DateOnly? D(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().Replace("/0226", "/2026");
            return DateOnly.TryParseExact(s,
                ["d/MM/yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy"],
                null, System.Globalization.DateTimeStyles.None, out var d) ? d : null;
        }

        void Add(
            string title, string? desc, ProjectStatus st, ProjectComplexity cx,
            string pr, string ou, int? gp, SiptGroup? sg, int? uor, int? prev,
            string? date, string? specs, string? epic)
        {
            var p = Project.Create(
                title.Length > 150 ? title[..150] : title,
                desc?.Length > 2000 ? desc[..2000] : desc,
                ou, cx, 2026, null, null,
                prev, null,
                prom[pr].Id, unit[ou].Id, uor,
                gp, sg, D(date), specs, epic);
            if (st != Stopped) p.TransitionTo(st);
            db.Projects.Add(p);
        }

        // Aliases para promotores
        const string VI   = "Vicerrectorado de Infraestructuras";
        const string VIT  = "Vicerrectorado de Investigación y Transferencia";
        const string VCID = "Vicerrectorado de Cultura, Igualdad y Diversidad";
        const string G    = "Gerencia";
        const string VEC  = "Vicerrectorado de Estudiantes y Coordinación";
        const string VE   = "Vicerrectorado de Estudios";
        const string R    = "Rectorado";
        const string SG   = "Secretaría General";
        const string VIC  = "Vicerrectorado de Internacionalización y Cooperación";
        const string VP   = "Vicerrectorado de Profesorado";

        // Aliases para unidades orgánicas
        const string SIPT   = "Servicio de Innovación y Planificación Tecnológica";
        const string JE     = "Junta Electoral";
        const string SII    = "Servicio de Infraestructura Informática";
        const string OCI    = "Oficina de Cultura e Igualdad";
        const string SPTI   = "Servicio de Personal Técnico, de Administración e Investigación";
        const string SCA    = "Servicio de Calidad";
        const string OO     = "Observatorio Ocupacional";
        const string SGE    = "Servicio de Gestión de Estudios";
        const string VITOU  = "Vicerrectorado de Investigación y Transferencia";
        const string SGI    = "Servicio de Gestión de la Investigación";
        const string SATDI  = "Servicio de Apoyo Técnico a la Docencia y a la Investigación";
        const string STC    = "Servicio de Transferencia de Conocimiento";
        const string SMCA   = "Servicio de Modernización y Coordinación Administrativa";
        const string SRIC   = "Servicio de Relaciones Internacionales y Cooperación";
        const string SCMAE  = "Servicio de Comunicación, Márketing y Atención al Estudiantado";
        const string OD     = "Oficina de Deportes";
        const string SPNS   = "Servicio de Profesorado, Nómina y Seguridad Social";
        const string CEGECA = "CEGECA-San Juan";
        const string ED     = "Escuela de Doctorado";
        const string SPRC   = "Servicio de Planificación y Racionalización de la Contratación";

        // ===== PROYECTOS CPTI-2026 =====

        // 1
        Add("PERMISOS 2.0: Nueva interfaz web aplicación de Permisos",
            "La aplicación de Permisos es una aplicación web Fullstack desarrollada hace muchos años con tecnologías desactualizadas, y a día de hoy sólo es posible ejecutar la aplicación en el navegador Edge con Compatibilidad de Internet Explorer habilitada. Se necesita rehacer el frontal para que funcione en todos los navegadores.",
            Stopped, Small, VI, SIPT, 5, WebTransversal, 13, null, "30/06/2026",
            "https://drive.google.com/open?id=16RrRoj-3BWGSEYWIpt6A4idgi2yGcowl", null);

        // 2
        Add("Automatización de la generación de los censos para centros e institutos de investigación",
            "Es necesario desarrollar un procedimiento para la automatización de la generación de los censos dado un centro o instituto de investigación, tal y como se realiza actualmente con el resto de elecciones.",
            Stopped, Small, VIT, JE, 5, RRHH, 5, null, "1/01/2026",
            "https://drive.google.com/open?id=16WRDmzJnl3owgX-GMRNURzPtUrwXNKSl", null);

        // 3
        Add("Propuesta mejora de la aplicación de Reserva de Estancias",
            "Propuestas de mejora de la aplicación de Reservas de Estancias. Se proponen cambios para mejorar la aplicación tanto en la parte de perfil Usuario como perfil Administrador.",
            InSprint, Medium, VI, SII, 3, WebTransversal, 9, 27, "1/05/2026",
            "https://drive.google.com/open?id=1b-KWDbjYzriEVm8sTkGK7ofYl792vvw2", null);

        // 4
        Add("Adecuación parcial de la aplicación gestión-red",
            "El proyecto consiste en la adecuación de la aplicación gestión-red que actualmente sólo se puede usar con el navegador Internet Explorer o Microsoft Edge (en modo de compatibilidad) para que se pueda usar con otros navegadores del tipo Chrome o Firefox.",
            Completed, Medium, VI, SII, 5, WebTransversal, 12, 40, "31/07/2026",
            "https://drive.google.com/open?id=1WYLLDdPcDxZ9qJDZ1JDeLw-cZ0pIbm1j", null);

        // 5
        Add("MIGRACIÓN DE TIPOS DE ENSEÑANZA NO REGLADA DE CULTURA A LA PLATAFORMA FÓRMATE",
            "Se trata de migrar el sistema de matriculación de nuestras actividades a la nueva plataforma FÓRMATE.",
            PlanningWithClient, Medium, VCID, OCI, 4, Academico, 6, null, "30/11/2026",
            "https://drive.google.com/open?id=1vqOd3x4frKamtfXNEyBPBJlhY_1tBWYG", null);

        // 6
        Add("Mejoras SATDI Aplicación de Pedidos",
            "Con el seguimiento de los expedientes 2024_SDA_01 y 2025_AM_02, cuyos trámites se realizan mediante la Aplicación de Pedidos, así como tras atender a demandas de los usuarios, se ha detectado la necesidad de la implantación de mejoras y actualizaciones que automaticen y simplifiquen procedimientos actualmente realizados fuera de ella.",
            DevelopmentOutsideSprint, Medium, VIT, SPTI, 5, Sede, 6, null, "1/06/2026",
            "https://drive.google.com/open?id=1v-vPwsfvLb27ZGU_RW9PWAYf6MRI4m9z", null);

        // 7
        Add("MEJORAS EN LA APLICACIÓN DE PLAN DIRECTOR",
            "Se trata de incluir en la aplicación de Plan Director una serie de mejoras que optimicen su funcionamiento y eviten tener que realizar cálculos manuales de los incentivos de calidad.",
            Completed, Small, G, SCA, 4, RRHH, 2, null, "31/12/2025",
            "https://drive.google.com/open?id=1ACO0Jf8vF98_C75dqzLdKfjnNHlkKPWQ",
            "https://cau-old.umh.es/browse/CALIDAD-425");

        // 8
        Add("Migración Sistema de Encuestas Calidad de Moodle a LimeSurvey",
            "El objetivo principal de este proyecto es el de migrar el actual sistema de encuestas de calidad basado en Moodle a la plataforma especializada de gestión de Encuestas LimeSurvey con el fin de proporcionar herramientas más específicas para la generación y gestión de encuestas, así como dotar de independencia en la creación y distribución de las mismas.",
            PostponedByClient, Medium, VE, SCA, 5, WebTransversal, 3, 57, "1/07/2027",
            "https://drive.google.com/open?id=1VgqW50DOaSzlPFXnfEBZZAymHKkp4NLx", null);

        // 9
        Add("CPTI26-00- Plantillas (II)",
            "El proyecto pretende permitir un sistema de plantillas, en el que dependiendo de las condiciones específicas de un convenio se generen la documentación de prácticas adaptada al caso sin necesidad de que el usuario que haga el envío deba revisarlo. Además, plantillas automatizadas y envíos editables para poder personalizar las comunicaciones y evitar pérdidas o malos entendidos.",
            Completed, Medium, VEC, OO, 5, Observatorio, 1, null, "28/02/2026",
            "https://drive.google.com/open?id=1Quy7tCZlUyWdsrjetifQgkpxwrMel_N3", null);

        // 10
        Add("CPTI26-OO-01- Gestor de Anexos II",
            "CPTI2601 - Gestor de Anexos II - RPAE e integración en el gestor, tiene por objetivo finalizar de integrar en la aplicación Observatorio el proceso de gestión documental de prácticas con 5 funcionalidades básicas: Gestión de remesas de documentación, Gestión de firmas agrupadas de anexos (RPAE), Generación de Expedientes en SEDE, Actualización automatizada de representantes.",
            PlanningSprint, Medium, VEC, OO, 5, Observatorio, 2, 86, "30/05/2026",
            "https://drive.google.com/open?id=1QXSAzoMRXQ2K50xnmoJeX08WTCrYftDe",
            "https://cau-old.umh.es/browse/EPRACTICAS-2536");

        // 11
        Add("CPTI26-OO-03 - Prácticas Internas",
            "El proyecto tiene por objeto sincronizar la información de las UOR y PDI/PAS existente en las distintas bases de datos, de modo que se encuentre actualizada en el OO para la gestión de prácticas. Además, incluye las actividades de control económico de los anexos a las normas de ejecución del presupuesto para la gestión de prácticas internas.",
            InSprint, Medium, VEC, OO, 5, Observatorio, 4, 90, "31/07/2026",
            "https://drive.google.com/open?id=1_AXcSRdJDjbKQJUN15P12YY9a2uk03C-",
            "https://cau-old.umh.es/browse/EPRACTICAS-2520");

        // 12
        Add("CPTI26-OO-05 Gestión de Ofertas de Empleo para Titulados Oficiales",
            "El proyecto está orientado a sustituir esta funcionalidad de OBSGESTION que actualmente se basa en forms y ASP clásico. El objetivo es que las entidades colaboradoras publiquen ofertas de empleo, valoren CV de titulados y realicen sus procesos de selección de nuestros titulados.",
            Stopped, Medium, VEC, OO, 5, Observatorio, 6, 93, "31/10/2026",
            "https://drive.google.com/open?id=1WnwgwKZr8GIQDQXG8TwJbwNV6Ef83dlt", null);

        // 13
        Add("CPTI26-OO-04 - Cuestionarios",
            "Actualmente para la evaluación y seguimiento de prácticas se usa la antigua aplicación CUESTIONARIOS que no cuenta ya con soporte. Estos cuestionarios son la base para la evaluación obligatoria de prácticas (RD592/14). El sistema quiere dotar a la aplicación Observatorio de un sistema de envío configurable, dirigible y que pueda cumplir las expectativas de los distintos títulos.",
            Stopped, Large, VEC, OO, 5, Observatorio, 5, 89, "31/12/2026",
            "https://drive.google.com/open?id=19IBulrEXEUE-uYeFKf238jGWqZOwQXfG", null);

        // 14
        Add("CPTI26-OO-02 - Iteración del proyecto",
            "Este proyecto incluye varias funcionalidades esenciales para el funcionamiento de las prácticas curriculares (8000 anuales), que afectan a más de 50 titulaciones: recoger las condiciones de los convenios y validarlas en las asignaciones de plazas, dar cabida a estudiantes FPO en el sistema, reforzar la información de las asignaciones en tiempo real e integrar tutores de prácticas clínicas hospitalarias.",
            Stopped, Small, VEC, OO, 5, Observatorio, 3, 87, "30/04/2026",
            "https://drive.google.com/open?id=1_H-riwj7oIzQjIb29Ht_B0lfhCUKnHpK", null);

        // 15
        Add("Formulario específico en Sede electrónica para la presentación de documentación de Becas del Ministerio",
            "El objetivo de este proyecto es implementar en Sede Electrónica un formulario específico para la presentación de documentación vinculada a la convocatoria de Becas del Ministerio de Educación, Formación Profesional y Deportes, evitando así la presentación a través de una instancia general.",
            Completed, Small, VEC, SGE, 4, Academico, 7, null, "30/06/2026",
            "https://drive.google.com/open?id=1deoeyplcmd8fFvpj6UtHl3tbSH2R5vbr", null);

        // 16
        Add("Módulo aplicación Becas Conselleria",
            "El objetivo de este proyecto es potenciar la ya existente aplicación de Becas de Conselleria, incorporando un nuevo módulo que permita integrar en dicha aplicación las nuevas convocatorias (Exención de Tasas, GVA Talent, Premios de Excelencia Académica y cualquier otra convocatoria sobrevenida).",
            DevelopmentOutsideSprint, Medium, VEC, SGE, 4, Academico, 19, 44, "30/09/2026",
            "https://drive.google.com/open?id=1W3-kv_YP30mxTn4Q47DoLFUv1ZM4eByU", null);

        // 17
        Add("Módulo aplicación Becas y Ayudas UMH",
            "El objetivo de este proyecto es potenciar la ya existente aplicación de Becas y Ayudas UMH, incorporando un nuevo módulo que permita integrar en dicha aplicación las nuevas convocatorias (Santander Ayudas al Estudio, Santander Excelencia 360º y cualquier otra convocatoria sobrevenida).",
            Stopped, Medium, VEC, SGE, 4, Academico, 15, 46, "30/06/2026",
            "https://drive.google.com/open?id=1Wzkb86nawtI7RDVyFs-DX8s_kiw37Ou4", null);

        // 18
        Add("CPTI26-OO-06 - Sistema de KPI y Cuadro de Mando Personalizable",
            "El Observatorio genera anualmente una gran cantidad de documentación y situaciones que requieren un control adecuado. Se propone la creación de un cuadro de mandos y el soporte a un sistema de BI que permita la monitorización y generación de informes ágiles.",
            Stopped, Medium, VEC, OO, 3, Observatorio, 7, 92, "31/12/2026",
            "https://drive.google.com/open?id=1JIfOOB6_vteGyfgECoGh10Iqf_MjkSnC", null);

        // 19
        Add("Módulo aplicación para la compensación de tasas por precios públicos",
            "El objetivo de este proyecto es crear una aplicación que permita realizar las distintas compensaciones económicas por precios públicos de las distintas administraciones: Compensación por Familias Numerosas, Compensación tasas Beca Ministerio, Compensación tasa Beca Generalitat Valenciana, Compensación tasas Ministerio parte no compensada.",
            Stopped, Medium, VE, SGE, 4, Academico, 21, null, "30/09/2026",
            "https://drive.google.com/open?id=1eAqNXCbHUZeY6G71ZPPFx2e4rpr31m3V", null);

        // 20
        Add("CPTI26-OO-08 - Gestión de Afiliación a la Seguridad Social",
            "El proyecto busca completar la implantación de la D.A.52 de la LGSS que dispone el alta en prácticas de estudiantes universitarios. Se pretende desarrollar un sistema de control del resultado de estas altas y bajas y poder determinar el impacto económico real de la medida.",
            Stopped, Small, VEC, OO, 3, Observatorio, 8, 94, "31/12/2026",
            "https://drive.google.com/open?id=17anjdAKJekzFC6h81KrDwUxyAVMUzgNo", null);

        // 21
        Add("Emisión de certificados de docencia y dirección de enseñanzas de formación permanente a través de la sede electrónica de la UMH.",
            "El objetivo de este proyecto es implementar los certificados de docencia y de dirección de las enseñanzas de formación permanente en la sede electrónica de la UMH.",
            Stopped, Small, VE, SGE, 4, Academico, 23, 58, "15/10/2026",
            "https://drive.google.com/open?id=1vNhpiQoEf2aDC1WEkvbBb_vEGLIACKyz", null);

        // 22
        Add("Reconocimiento de créditos entre los grados que conforman un doble grado.",
            "El objetivo de este proyecto consiste en adaptar la gestión de los Programas de Estudios Simultáneos (Dobles Grados) para permitir el reconocimiento de una asignatura o grupo de asignaturas de cualquiera de los grados mediante la superación de más de una asignatura del Doble Grado.",
            DevelopmentOutsideSprint, Medium, VE, SGE, 5, Academico, 11, 60, "30/01/2026",
            "https://drive.google.com/open?id=193DQEn7n6o4ywbxWLUZWAcuBkkeBZBH8", null);

        // 23
        Add("Formulario específico en Sede electrónica para la solicitud de expedición de duplicados de títulos oficiales",
            "Formulario específico para la solicitud de expedición de duplicados de títulos oficiales, evitando así la presentación a través de una instancia general.",
            Stopped, Small, VE, SGE, 3, Academico, 24, null, "30/10/2026",
            "https://drive.google.com/open?id=18h58FkYJXIiE9NB3bjqh1utqopnKdb02", null);

        // 24
        Add("Nueva aplicación para la gestión de los Títulos Oficiales",
            "El objetivo de este proyecto es terminar de desarrollar la nueva aplicación para la gestión de títulos oficiales de la UMH de la cual ya hay un proyecto avanzado. Con este proyecto se pretende dar respuesta a las distintas situaciones que en la gestión de los expedientes de títulos oficiales se presentan en el Servicio de Gestión de Estudios.",
            Stopped, Medium, VE, SGE, 5, Academico, 20, 43, "30/04/2026",
            "https://drive.google.com/open?id=1w3g-A60jxPDEhsuu5skIBVQj0D7cuY2o", null);

        // 25
        Add("Transferencia de los expedientes de solicitud de expedición de títulos",
            "El objetivo de este proyecto es transferir los expedientes de solicitud de expedición de títulos oficiales en SEDE desde los Centros de Gestión y Doctorado al SGE.",
            Stopped, Small, VE, SGE, 5, Academico, 9, null, "30/04/2026",
            "https://drive.google.com/open?id=1rEkmZ5qy84TEW6ZzJP2GqoGRc-umOa2b", null);

        // 26
        Add("Interoperabilidad de datos por recubrimiento",
            "El objetivo de este proyecto es que las aplicaciones de la UMH puedan consultar los datos en las correspondientes plataformas públicas disponibles, según el Catálogo de Servicios de Verificación y Consulta de datos SCSP, con el fin de dar cumplimiento al artículo 28.2 de la Ley 39/2015.",
            Stopped, Large, VEC, SGE, 5, Academico, 14, 31, "30/05/2026",
            "https://drive.google.com/open?id=1theY-emMNcxlXzC5GbYReiTJjMXaTkMj", null);

        // 27
        Add("Modificación de matrícula",
            "El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de matrícula parcial y superación de matrícula máxima.",
            Stopped, Small, VEC, SGE, 3, Academico, 26, 34, "30/07/2026",
            "https://drive.google.com/open?id=1ndGhtoBom6H0T1FkdHnsD17vo-PK8FXN", null);

        // 28
        Add("Traslado de expediente",
            "El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de traslado de expediente de grado y de pruebas de acceso a la universidad.",
            Stopped, Medium, VEC, SGE, 5, Academico, 27, 35, "30/06/2026",
            "https://drive.google.com/open?id=1WbxjraIC9fzcOcieu0lDtn7U1s4AN_9P", null);

        // 29
        Add("PROTOCOLO GESTIÓN COBRO RECIBOS",
            "El control de impagos de las tasas de matrícula en los estudios de Grado y Máster Universitario es un proceso bastante manual y sería conveniente una automatización para automatizar el envío de avisos y recordatorios a los estudiantes con pagos pendientes.",
            Stopped, Small, G, CEGECA, 3, Academico, 13, 20, "4/05/2026",
            "https://drive.google.com/open?id=1lyX3WmOshhxz70PcZiE3t6twsEcZzwJI", null);

        // 30
        Add("Anulación de matrícula",
            "El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de anulación de matrícula voluntaria y por causa de fuerza mayor.",
            Stopped, Small, VEC, SGE, 3, Academico, 28, 36, "30/07/2026",
            "https://drive.google.com/open?id=1hUW8kbIJYLBuSKMveQ5dm58wNptUiOHz", null);

        // 31
        Add("Aplicación matrícula selectividad",
            "El objetivo de este proyecto es obtener para cada exención y bonificación previstas en el Decreto 101/2024, de 2 de agosto, del Consell, por el que se regulan los precios públicos de los servicios académicos universitarios, el importe total de la matrícula de la PAU. Este dato es necesario para solicitar las compensaciones CNEA.",
            InSprint, Small, VEC, SGE, 5, Academico, 3, null, "30/04/2026",
            "https://drive.google.com/open?id=1DX9HdgubqqWyQhm8Wwx0uuMB354tB0XN",
            "https://cau-old.umh.es/browse/ACCESOG-1085");

        // 32
        Add("Informe Subvenciones CNEA",
            "El objetivo de este proyecto es obtener un informe en formato Excel para solicitar la compensación por Costes Derivados de la Normativa Estatal y Autonómica. Anualmente la Dirección General de Universidades solicita a la Gerencia certificación firmada acreditativa de la compensación por los citados costes.",
            Stopped, Small, VEC, SGE, 5, Academico, 1, null, "30/04/2026",
            "https://drive.google.com/open?id=19tRcsMEzmDgxwoBVQb4PdL5TGuoEY_Ne", null);

        // 33
        Add("Gestión eficiente de aulas",
            "Implantar un sistema sencillo, digital y verificable que garantice que las aulas reservadas en estancias de la Universidad Miguel Hernández de Elche (UMH) para clases y otras actividades sean efectivamente utilizadas, evitando bloqueos innecesarios y asegurando un uso eficiente de los espacios.",
            Completed, Small, R, SIPT, 5, WebTransversal, 1, null, "8/02/2026",
            "https://drive.google.com/open?id=15LCfTdoAcy93AiWUiTe50RS7jIde3RxF", null);

        // 34
        Add("Modificaciones en Sexenios AVAP",
            "Solicitud de modificaciones en la aplicación de sexenios AVAP acorde con los cambios que presenta la aplicación de ANECA. Se incorporarán los cambios necesarios una vez la ANECA abra su aplicación.",
            Completed, Small, VIT, VITOU, 5, RRHH, 1, null, "22/12/2025",
            "https://drive.google.com/open?id=1Id9h5aTGjJ_CEYx9V1McDVN-hA-9vlLS",
            "https://cau-old.umh.es/browse/EXPEDIENTE-735");

        // 35
        Add("Gestor de Equipos de Investigación Científica (GEIC)",
            "En la CPTI de 2025 se inició el desarrollo de un nuevo gestor de los equipos de investigación (GEIC) respondiendo a nuevas necesidades de seguimiento de utilización de los grandes equipos. En esta convocatoria se solicita ampliar funcionalidades y poner en producción GEIC para que esté a disposición de investigadores en 2026.",
            Completed, Medium, VIT, SATDI, 5, WebTransversal, 6, 45, "1/03/2025",
            "https://drive.google.com/open?id=1D7ALIUuXBNj0LOtndzKd7r4RKmDuvwmF", null);

        // 36
        Add("Aplicación de registro de dedicación a proyectos",
            "Desarrollo de una aplicación para registro de dedicación a proyectos, con objeto de recoger las dedicaciones horarias del personal investigador necesarias para la gestión y justificación de los costes de personal de sus proyectos.",
            Stopped, Medium, VIT, SGI, 5, InvestigacionEconomico, 8, null, "1/02/2026",
            "https://drive.google.com/open?id=1c4dKcHkkIIt3IdZtltbwpTbIZOXk", null);

        // 37
        Add("MEJORA - SIGITT 2.0 JUSTIFICACIÓN ECONÓMICA DE PROYECTOS DE INVESTIGACIÓN",
            "Dotar a SIGITT 2.0 de los complementos y mejoras necesarios para su plena utilización e implementación definitiva. Se busca compendiar la información y simplificar las pantallas, completando la información para que esté toda disponible en el mismo aplicativo.",
            Stopped, Large, VIT, SGI, 5, InvestigacionEconomico, 5, 91, "30/03/2026",
            "https://drive.google.com/open?id=1l5EAy2H9C3Xh7FLEKfJOz8_RgZdv20qq", null);

        // 38
        Add("Mejoras en la aplicación del espacio opositor",
            "Por un lado, tiene como objetivo mejorar algunas características de la aplicación Espacio opositor. Por otro lado, agilizar las gestiones administrativas de las convocatorias eliminando las gestiones que se realizan en Hominis-Universitas XXI. Se pretende que la gestión se realice solo con el Gestor de expedientes y el espacio opositor.",
            Stopped, VeryLarge, G, SPTI, 4, Sede, 5, null, "31/12/2026",
            "https://drive.google.com/open?id=19HRrLHtPB82PnKmhL-Yd0qiY0iGWDTwP", null);

        // 39
        Add("Migración de datos de UXXI Investigación a SIGITT2.0",
            "La aplicación UXXI Investigación de OCU, lleva años sin soporte, al igual que toda la infraestructura necesaria para que siga funcionando. El objetivo es la realización de la migración de datos de UXXI Investigación a SIGITT2.0, de forma que sea posible apagar definitivamente UXXI Investigación.",
            Stopped, Medium, VI, SII, 4, InvestigacionEconomico, 7, null, "31/12/2026",
            "https://drive.google.com/open?id=1KbeNs0EW6ASas4HFguKE53cjMDCpeAab", null);

        // 40
        Add("Gestión Telefonía UMH",
            "Mejoras para la aplicación Gestión Telefonía UMH para una mejora en las solicitudes y visualización de contenidos.",
            Completed, Small, VI, SII, 4, WebTransversal, 11, 41, "12/01/2026",
            "https://drive.google.com/open?id=1xsZdmrnEqRpF0BJVj9R7wVf8SwDCLh35", null);

        // 41
        Add("Certificados de traslado de la PAU Selectividad",
            "El objetivo de este proyecto es modificar la emisión de certificados de traslado de PAU, que se realiza desde la aplicación de Selectividad, de tal forma que se realice a través de la sede electrónica y obtener los certificados con sello de órgano.",
            Stopped, Small, VEC, SGE, 5, Academico, 25, 29, "30/05/2026",
            "https://drive.google.com/open?id=1p7RPbUph9Czvm9yzFsAeH44Ilf2WmKE6", null);

        // 42
        Add("Compensaciones exceso horario",
            "El objetivo principal es desarrollar una nueva aplicación que permita el registro y cálculo automatizado de las horas extraordinarias realizadas y previamente autorizadas por los empleados, reemplazando el proceso manual actual.",
            Stopped, Medium, G, SPTI, 3, RRHH, 7, 8, "31/12/2026",
            "https://drive.google.com/open?id=1hXswTq24FC3Uf8LyegI7vbQqcx1Et68G", null);

        // 43
        Add("Ausencias",
            "Desarrollo de una nueva aplicación de gestión de ausencias. La solicitud se basa en la necesidad de una nueva herramienta debido a la obsolescencia del sistema actual y al aumento de usuarios, tipos de permisos y autorizadores.",
            Stopped, Large, G, SPTI, 4, RRHH, 6, null, "31/12/2026",
            "https://drive.google.com/open?id=1EDBIoo39vsvDay2bkBxWplyfB1a_K4D3", null);

        // 44
        Add("CERTIFICADO HISTÓRICO FORMACIÓN PTGAS",
            "Proyecto para la creación de un Certificado Histórico de las actividades formativas del Personal Técnico, de Gestión y de Administración y Servicios (PTGAS), accesible a través de la Sede Electrónica. Esta funcionalidad es una demanda histórica de los sindicatos.",
            Stopped, VerySmall, G, SPTI, 2, Academico, 10, null, "31/12/2026",
            "https://drive.google.com/open?id=1TzCNpHhmVg9eJ7IdPpR6TiIdHygTK7mZ", null);

        // 45
        Add("Continuación de estudios",
            "El objetivo de este proyecto es que el estudiantado pueda solicitar desde su acceso identificado los procedimientos de continuación de estudios por distintos motivos.",
            Stopped, Small, VEC, SGE, 4, Academico, 29, 33, "30/05/2026",
            "https://drive.google.com/open?id=1MlcK1laeW64tWhz2BXE63lWyGu8ue9Ot", null);

        // 46
        Add("Gestor de bolsas de trabajo",
            "Proyecto de desarrollo de un Gestor de bolsas de trabajo destinado al PTGAS para centralizar la gestión de las bolsas de empleo en una única aplicación automatizada, eliminando el riesgo de errores que conllevan los actuales procesos manuales.",
            Stopped, Large, G, SPTI, 4, RRHH, 4, 6, "31/12/2026",
            "https://drive.google.com/open?id=1WHtksUZOj77o-SStQmWf_EyF8itYGzkv", null);

        // 47
        Add("Reserva de Instalaciones Deportivas UMH - Fase 2",
            "Concluida la Fase 1 del proyecto, se ha identificado un conjunto de funcionalidades complementarias necesarias para satisfacer plenamente los requerimientos de la Oficina de Campus Saludables y Deportes. Esta Fase 2 se orienta a la incorporación, mejora y finalización de estas funcionalidades pendientes.",
            InSprint, Small, R, OD, 4, WebTransversal, 5, null, "15/01/2026",
            "https://drive.google.com/open?id=1g171D8LWkDlhDjqRjWrFk1eT0h3rco7i", null);

        // 48
        Add("Mejoras de Escuela de Verano y Aula Junior",
            "El proyecto busca mejorar y agilizar la gestión de la Escuela de Verano y Aula Junior, reduciendo incidencias para usuarios internos y externos. Se optimizará el proceso de validación de la documentación requerida, actualmente gestionado por correo electrónico.",
            PlanningWithClient, Medium, R, OD, 4, WebTransversal, 4, null, "15/03/2026",
            "https://drive.google.com/open?id=15G5c4R-krVCMDDMm8_G8x10ZbYGsRh0R", null);

        // 49
        Add("Ticketing UMH",
            "Implantar mejoras en Ticketing UMH para beneficio de todos los técnicos de SII y SIPT.",
            InSprint, Small, VI, SII, 5, WebTransversal, 10, null, "31/03/2026",
            "https://drive.google.com/open?id=1RWGRRiOKmBrByoePvB6S9QWuuYQ-wF9i", null);

        // 50
        Add("Modificaciones programa DOCENTIA_UMH",
            "El objetivo del proyecto es solicitar mejoras y nuevas funcionalidades en la aplicación del DOCENTIA. Estas modificaciones están motivadas para incorporar las recomendaciones del informe final de la comisión de implantación del diseño de la ANECA, así como por la adaptación del modelo al Programa de Apoyo para la Evaluación de la Calidad de la Actividad Docente.",
            InSprint, Medium, VP, SPNS, 5, Academico, 4, null, "20/03/2026",
            "https://drive.google.com/open?id=1jjk63sB0EgS7PEar9P-tpXWbuHQJVabI",
            "https://cau-old.umh.es/browse/CALIDEVAL-606");

        // 51
        Add("Gestor de currículo",
            "Mejoras a desarrollar en la aplicación Gestor de Currículo, incluyendo la revisión y actualización de la normativa del Procedimiento de Evaluación de la Actividad Investigadora, Transferencia Tecnológica y Difusión de la Ciencia de la Universidad Miguel Hernández.",
            Stopped, Medium, VIT, VITOU, 5, InvestigacionEconomico, 9, null, "30/09/2026",
            "https://drive.google.com/open?id=1j6Wjjoh_WkLS68B-uM4PqiL4gQUkx-zy", null);

        // 52
        Add("Comprobación Documental",
            "Mejoras a desarrollar en la aplicación Comprobación Documental, con la finalidad de facilitar el proceso de validación de méritos que se realiza a los investigadores de la universidad.",
            Stopped, Small, VIT, VITOU, 3, InvestigacionEconomico, 10, null, "30/09/2026",
            "https://drive.google.com/open?id=11q47pHy21EOHGf4UvFtnO-VLnVmTodm5", null);

        // 53
        Add("Grupos de investigación",
            "Mejoras a desarrollar en la aplicación Grupos Investigación, incluyendo la revisión y actualización de la normativa del Reglamento de Grupos de Investigación de la Universidad Miguel Hernández, cuya última actualización es de noviembre de 2014.",
            PlanningSprint, Medium, VIT, VITOU, 5, InvestigacionEconomico, 11, 84, "30/09/2025",
            "https://drive.google.com/open?id=1oulH-G3k_7H2JBEZ9YlXLK4EuVzGO9gA",
            "https://cau-old.umh.es/browse/INVESTIGA-3424");

        // 54
        Add("Reingeniería Gestor de CV",
            "Desarrollar una nueva interfaz respetando el flujo actual de la aplicación. La prioridad será generar una interfaz modular para poder cambiarla en un futuro y reutilizar los módulos creados en esta reingeniería. Se creará un frontend y un backend para desacoplar funcionalidades.",
            Stopped, Large, VIT, VITOU, 4, InvestigacionEconomico, 13, null, "30/09/2026",
            "https://drive.google.com/open?id=1uFwyjndFxzn50ki8RmEuKa1qOQ-SIP7s", null);

        // 55
        Add("Certificados de Investigación",
            "El objeto de este proyecto es la expedición a través de la SEDE electrónica de la UMH de los certificados de investigación que actualmente se expiden manualmente.",
            DevelopmentOutsideSprint, Medium, VIT, STC, 5, InvestigacionEconomico, 1, 76, "31/01/2026",
            "https://drive.google.com/open?id=19qvq3S2Uets36cv5PnwfiRM6pgm_1RuJ", null);

        // 56
        Add("Simplificación de procedimiento de prestaciones de servicio",
            "El objeto de este proyecto es simplificar el procedimiento de prestaciones de servicio, tanto periódicas como no-periódicas. Para ello, se solicita una nueva aplicación llamada Prestaciones de Servicio, parecida al frontal de la aplicación Documentos de pago, donde se pueda ver dónde se encuentra el trámite.",
            DevelopmentOutsideSprint, Large, VIT, STC, 5, InvestigacionEconomico, 2, 69, "31/03/2026",
            "https://drive.google.com/open?id=1iT85DEIpY5yeeeSCi_wkVI3t6a2zHoZr", null);

        // 57
        Add("Mejoras aplicación de preinscripción",
            "El objetivo de este proyecto es mejorar la aplicación de preinscripción, teniendo en cuenta las necesidades que se han detectado en el último curso académico.",
            WaitingForDevelopers, Large, VE, SGE, 5, Academico, 17, 37, "15/03/2026",
            "https://drive.google.com/open?id=1Nq0B5llzp9d0lOBh_21KgkG1w3ZyN58n",
            "https://cau-old.umh.es/browse/ACCESOP-441");

        // 58
        Add("Revisión de la integración UXXI-SIGITT2",
            "El objeto de este proyecto es abordar algunos problemas con la integración UXXI-SIGITT2 que dificultan el día a día con SIGITT.",
            Stopped, Small, VIT, STC, 3, InvestigacionEconomico, 4, 74, "1/06/2026",
            "https://drive.google.com/open?id=1RK06IPrFS0NXCjUVZLg9mGQ4l5vJnsXM", null);

        // 59
        Add("Mejoras aplicación certificados TFM",
            "El objetivo de este proyecto es mejorar la aplicación de Certificado TFM, adecuándola a las necesidades que se han detectado una vez se ha puesto en funcionamiento.",
            Stopped, Medium, VE, SGE, 4, Academico, 22, null, "30/07/2026",
            "https://drive.google.com/open?id=12GoWqQ0ztS3CkFl1fUyXe2UuuLw49Bcn", null);

        // 60
        Add("Mejoras de rendimiento en SIGITT2",
            "El objeto de este proyecto es mejorar el rendimiento de la aplicación SIGITT2 puesto que los gestores de STC y SGI pierden mucho tiempo mientras se cargan o almacenan los datos.",
            DevelopmentOutsideSprint, Small, VIT, STC, 5, InvestigacionEconomico, 3, 78, "1/02/2026",
            "https://drive.google.com/open?id=1Uhc1BFGFRwbjUjVeUSodgvraK1CHg6uL", null);

        // 61
        Add("Revisión de la integración GestorCV-SIGITT2",
            "El objeto de este proyecto es abordar algunos problemas con la integración GestorCV-SIGITT2. Corrige errores de criterios y extractores en el módulo de IPR.",
            Stopped, Small, VIT, STC, 3, InvestigacionEconomico, 6, 70, "1/06/2026",
            "https://drive.google.com/open?id=1x-FBvytF5frArc8IPR7dV9jNl5h", null);

        // 62
        Add("Mejora aplicación Retribuciones Adicionales",
            "Mejora de la aplicación de méritos autonómicos, introduciendo comprobación de los límites y ampliando la posibilidad a personal contratado.",
            Completed, Medium, VP, SPNS, 5, RRHH, 3, null, "28/02/2026",
            "https://drive.google.com/open?id=1YTTsPBm1yo_Jarngx_kRQGmyFLidCZVv",
            "https://cau-old.umh.es/browse/SUELDO-1061");

        // 63
        Add("MEJORAS EN GISBAP",
            "Mejorar la aplicación de gestión de subvenciones (GISBAP) incluyendo nuevas funcionalidades que permitan la simplificación administrativa y que agilicen la realización del trabajo. Se requiere disponer integraciones con servicios de intermediación de datos y con el sistema económico UXXI.",
            Stopped, Medium, SG, SMCA, 5, Sede, 3, null, "31/03/2026",
            "https://drive.google.com/open?id=1PWJ97JusHmUlWdwgYL7dVEsQFYhlkkAn", null);

        // 64
        Add("MEJORAS GESTIÓN DE CONVENIOS UMH",
            "Mejorar la aplicación Gestión de convenios UMH con las solicitudes de mejora que se han detectado sobre la versión inicial tras la puesta en producción.",
            Stopped, Medium, SG, SMCA, 5, Sede, 4, null, "31/05/2026",
            "https://drive.google.com/open?id=1xNDiVC7pI8g5775r4D0H5nXtonu4myk6", null);

        // 65
        Add("Acceso a la Sede electrónica para extranjeros y usuarios externos",
            "El sistema de Autenticación de Ciudadano UE (eIDAS) permite a los ciudadanos de un país de la Unión Europea usar su identificación electrónica nacional para acceder a servicios públicos en otros países miembros. Se solicita la integración con el nodo eIDAS español a través del sistema Cl@ve y el Registro de Usuarios Externos (RUE).",
            PlanningWithClient, Large, SG, SMCA, 5, WebTransversal, 2, 26, "31/03/2026",
            "https://drive.google.com/open?id=1TIKLtd68eewoCnJ2f0sjVBC_AdtSt2fM", null);

        // 66
        Add("HISTÓRICO INFORMACIÓN DE REGISTRO GENERAL (MASTIN)",
            "La aplicación Mastin lleva años sin soporte, al igual que toda la infraestructura necesaria para que siga funcionando. Se propone el diseño de una aplicación o conector ODBC para acceso a registros históricos de Mastín no migrados a Geiser.",
            Completed, Small, SG, SMCA, 4, Sede, 10, null, "30/09/2025",
            "https://drive.google.com/open?id=1R1JWvNyXB5UidT5YkZvpSU75P3CdFMNk", null);

        // 67
        Add("Web de reserva de actividades para centros de educación secundaria y bachillerato",
            "El objeto de este proyecto es contar con una web de gestión de reserva, para que los centros de educación secundaria puedan reservar las actividades que la UMH les ofrece, dentro de los programas de difusión y captación, tales como charlas informativas, visitas a los campus, talleres divulgativos y jornadas de conferencias.",
            Stopped, Medium, VEC, SCMAE, 5, WebTransversal, 8, 68, "1/07/2026",
            "https://drive.google.com/open?id=18gsoOaBrH0kZkeLFW7wqkeVjJ6jKobqJ", null);

        // 68
        Add("MEJORAS GESTOR DE EXPEDIENTES Y PORTAFIRMAS UMH",
            "Mejorar la aplicación Gestor de Expedientes UMH y la aplicación Portafirmas UMH con las solicitudes de mejora recibidas y que han sido calificadas con prioridad crítica.",
            Stopped, VeryLarge, SG, SMCA, 5, Sede, 2, null, "30/09/2026",
            "https://drive.google.com/open?id=1rllo5vbtdk8B9m0AjckHkn_ilsZNmMeM", null);

        // 69
        Add("ERASMUS WITHOUT PAPER (Acuerdo Académicos conectar con la EWP)",
            "Una vez finalizada la fase 2 de los modelos de cambios y la firma de los acuerdos académicos, la parte de la aplicación destinada a la gestión de acuerdos académicos deberá estar conectada a la red EWP. Esto permitirá gestionar en línea los acuerdos de aprendizaje entre los países miembros de la UE y los terceros países asociados al Programa Erasmus+.",
            DevelopmentOutsideSprint, Medium, VIC, SRIC, 5, Academico, 2, null, "31/05/2026",
            "https://drive.google.com/open?id=1-y5z9wOx_cTIIVlXeUhauvzyteUK2Yxi", null);

        // 70
        Add("ERASMUS WITHOUT PAPER (TRANSCRIPT OF RECORDS- Conectar con la EWP)",
            "Una vez desarrollada esta parte de la aplicación de acuerdos académicos deberemos estar conectados a la red EWP para gestionar los Transcript of Records entre los países miembros de la UE y terceros países asociados al Programa Erasmus+.",
            Stopped, Medium, VIC, SRIC, 5, Academico, 5, null, "30/06/2025",
            "https://drive.google.com/open?id=1yr70v-r5MClnDlDqL-bd0R1IS6zN5KtC", null);

        // 71
        Add("MOVILIDAD- Propia Estudiante Visitante PARA ESTUDIOS UMH",
            "Procedimiento de admisión estudiante visitante en la UMH: procedimiento para la admisión de estudiantes visitantes que deseen cursar alguna asignatura y/o realizar un periodo de actividades tuteladas en la UMH, fuera de los programas oficiales de movilidad, acuerdos o convenios de intercambio interuniversitario.",
            Stopped, Medium, VIC, SRIC, 4, Academico, 12, null, "30/09/2026",
            "https://drive.google.com/open?id=1QWM5uVvu307hxOSDUtjPQNh0SYnQt1XP", null);

        // 72
        Add("MOVILIDAD-REPOSITORIO DE DOCUMENTACIÓN PARA EL ESTUDIANTADO QUE PARTICIPA EN PROGRAMAS DE MOVILIDAD",
            "La creación de un repositorio de documentación para el estudiantado que tenga una plaza de movilidad aceptada dentro de un programa de movilidad.",
            Stopped, Medium, VIC, SRIC, 5, Academico, 16, 50, "30/05/2026",
            "https://drive.google.com/open?id=1IFDytu2QJP1tBNEy5vovKf1Ozy8UxlNi", null);

        // 73
        Add("MOVILIDAD- REPOSITORIO DE DOCUMENTACIÓN PARA CONVOCATORIAS DE MOVILIDAD DE PDI Y PTGAS",
            "Utilizar los documentos subidos en el espacio opositor para las convocatorias de movilidad internacional del PDI y PTGAS.",
            Stopped, Medium, VIC, SRIC, 4, Academico, 18, 51, "30/09/2026",
            "https://drive.google.com/open?id=1vh5tlDUkzq1Ai88qDje7im8qnB6_1a95", null);

        // 74
        Add("APLICACIÓN PARA PROYECTOS INTERNACIONALES Y COOPERACIÓN AL DESARROLLO",
            "Necesitamos que se nos adapte el módulo de SIGITT2.0 para la gestión de proyectos internacionales y de cooperación al desarrollo (CID) que se gestionan en el Servicio de Relaciones Internacionales y Cooperación.",
            Stopped, Large, VIC, SRIC, 5, InvestigacionEconomico, 12, null, "30/06/2026",
            "https://drive.google.com/open?id=1wvc4j48lZm6rKitiQAWLgNIYHanSBQ9F", null);

        // 75
        Add("Plataforma de inscripciones para Vida UMH",
            "Se trata de diseñar una plataforma de gestión de las inscripciones en las actividades de Vida UMH, tanto las gratuitas como las de pago.",
            Stopped, Small, VEC, SCMAE, 5, WebTransversal, 7, null, "1/06/2026",
            "https://drive.google.com/open?id=1JfxPnyVblECV3xI0Ojhego5XDo2DZX9z", null);

        // 76
        Add("Mejoras SII Aplicación Compras Menores para solicitud a Cartera Proyectos TI 2026 de la UMH",
            "Solicitud de mejoras y adaptación de la aplicación de Compras menores para agilizar el flujo de comunicaciones entre el Autorizador y Peticionario.",
            Completed, Small, G, SII, 4, Sede, 8, null, "19/03/2026",
            "https://drive.google.com/open?id=1PRBVwPdxbVqW22_lgG98W7WpjS8f28HI", null);

        // 77
        Add("Mejoras SII Aplicación Pedidos para solicitud a Cartera Proyectos TI 2026 de la UMH",
            "Mejoras y adaptación de la Aplicación de Pedidos para mejorar la comunicación con los proveedores del nuevo acuerdo marco de Servicios TIC que se iniciará a principios del 2026, permitiendo agilizar el flujo de comunicaciones entre el Peticionario y las empresas proveedoras.",
            InTesting, Medium, G, SII, 5, Sede, 7, null, "14/02/2026",
            "https://drive.google.com/open?id=1mRUMBK7qpbrDHrtFNbrawKkesPpAd-fc", null);

        // 78
        Add("Mejoras Applicación de Seguimiento de Doctorado y Trámites Depósito de Tesis",
            "El objetivo de este proyecto es continuar con la mejora de la aplicación de Seguimiento de Doctorados iniciada en la Cartera de Proyectos de 2025 y que no se ha podido terminar de implementar por la complejidad de su realización. Persigue la internacionalización de la Escuela de Doctorado con el acceso de evaluadores, tribunales y estudiantes extranjeros.",
            DevelopmentOutsideSprint, Large, VIT, ED, 5, Academico, 8, 81, "1/03/2025",
            "https://drive.google.com/open?id=1dTh1UwC_DVUJ5-xdqAj9eZBMXShuUwp8", null);

        // 79
        Add("Mejoras aplicación gestión TFG/M",
            "El objetivo general del proyecto es la mejora de la aplicación de gestión del TFG/M. Se busca centralizar toda la información relativa a los TFG/M de cara a procesos de acreditación y mejorar la usabilidad para reducir las dudas e incidencias que surgen sobre el manejo de la aplicación.",
            Stopped, Small, VE, SGE, 5, Academico, 30, null, "1/09/2026",
            "https://drive.google.com/open?id=1bRiToVJ53pOLlfOWH1-9lGb4pHJSUwp8", null);

        // 80
        Add("REGISTRO DE APODERAMIENTOS Y REGISTRO DE FUNCIONARIOS HABILITADOS UMH",
            "El Registro Electrónico de Apoderamiento de la Administración General del Estado permitirá hacer constar las representaciones que los ciudadanos otorguen a terceros para actuar en su nombre de forma electrónica ante la UMH, la AGE y sus organismos públicos.",
            Stopped, Medium, SG, SMCA, 5, Sede, 9, 14, "28/02/2026",
            "https://drive.google.com/open?id=1LaeeIogRcebCPx7rha9bD4ArxfliQtmw", null);

        // 81
        Add("Herramienta gestión expedientes SDA",
            "Se precisa de una herramienta para la correcta tramitación de los expedientes de contratación por sistema dinámico de adquisición.",
            DevelopmentOutsideSprint, VeryLarge, G, SPRC, 5, Sede, 1, null, "31/12/2026",
            "https://drive.google.com/open?id=1pvhtYi4YP6LMDENwU0qHapA6a0WnM84z", null);

        // 82
        Add("Automatización de las inscripciones de SABIEX",
            "Migrar SABIEX a Fórmate y configurar este tipo de enseñanza como un título propio. Se busca actualizar y homogeneizar la forma de matrícula en las enseñanzas no regladas.",
            DevelopmentOutsideSprint, Small, VCID, OCI, 5, Academico, 1, null, "15/09/2026",
            null, null);

        // 83
        Add("Elecciones Delegado General de Estudiantes 2026",
            "Elecciones Delegado General de Estudiantes 2026.",
            Completed, VerySmall, SG, JE, null, RRHH, 3, null, "8/05/2026",
            null, "https://cau-old.umh.es/browse/ELECCIONES-335");

        // 84
        Add("Recibo reserva de plaza preinscripción máster",
            "Se debe abonar un recibo reserva de plaza preinscripción máster para poder matricularse en el máster.",
            WaitingForDevelopers, Small, VE, SGE, null, Academico, 3, null, "1/07/2026",
            null, "https://cau-old.umh.es/browse/ACCESOP-476");

        // 85
        Add("Asignaturas equivalentes en Campus Virtual",
            "El proyecto tiene como objetivo facilitar la gestión conjunta de asignaturas equivalentes en el campus virtual. Para ello, se definirá por curso académico qué asignaturas y bloques se consideran equivalentes, de manera que la asignatura principal agrupe y gestione el conjunto de asignaturas asociadas.",
            DevelopmentOutsideSprint, Medium, VE, SGE, null, Academico, 1, null, "1/06/2026",
            "https://drive.google.com/drive/folders/13_4NVtjkVof6x-qdeUuV0p-l7x3hlVzt",
            "https://cau-old.umh.es/browse/DOCENCIAV-632");

        await db.SaveChangesAsync();
    }
}
