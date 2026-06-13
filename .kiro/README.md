# Sistema de Agentes — Cartera de Proyectos TIC

Sistema de agentes especializados para desarrollo cooperativo con **kiro-cli**, inspirado en [SwarmForge](https://github.com/unclebob/swarm-forge) pero adaptado al modelo de agentes de Kiro.

## Filosofía

En lugar del flujo secuencial de SwarmForge (`specifier → coder → refactorer → architect`), este sistema usa **agentes intercambiables por atajo de teclado** que el desarrollador orquesta manualmente. Cada agente tiene un rol claro, skills compartidos y restricciones que evitan que se salga de su responsabilidad.

## Agentes

| Atajo | Agente | Rol | Modifica código |
|-------|--------|-----|-----------------|
| `Ctrl+Shift+1` | **specifier** | Convierte ideas en specs técnicas con criterios de aceptación | ❌ |
| `Ctrl+Shift+2` | **backend-dev** | Implementa features en .NET 10 (handlers, endpoints, tests) | ✅ |
| `Ctrl+Shift+3` | **frontend-dev** | Implementa features en Angular 21 (componentes, servicios, tests) | ✅ |
| `Ctrl+Shift+4` | **architect** | Revisa código, valida arquitectura, detecta problemas | ❌ |
| `Ctrl+Shift+5` | **tester** | Escribe tests unitarios, integración y E2E | ✅ |

## Skills (conocimiento compartido)

```
.kiro/skills/
├── angular21/SKILL.md    # Patterns Angular 21 (zoneless, signals, standalone, NG-ZORRO)
├── dotnet10/SKILL.md     # Patterns .NET 10 (Clean Architecture, MediatR, Minimal APIs)
└── domain/SKILL.md       # Dominio del proyecto (entidades, estados, roles, reglas)
```

Cada agente carga los skills relevantes a su rol. El skill de dominio es compartido por todos.

## Flujo de trabajo recomendado

```
1. Ctrl+Shift+1 (specifier)
   → "Necesito que los desarrolladores puedan autoasignarse tareas desde el Kanban"
   → El specifier genera la spec completa con criterios de aceptación

2. Ctrl+Shift+2 (backend-dev)
   → "Implementa esta spec: [pegar spec]"
   → Crea handler, validator, endpoint, test unitario

3. Ctrl+Shift+3 (frontend-dev)
   → "Implementa esta spec: [pegar spec]"
   → Crea componente, servicio, test

4. Ctrl+Shift+4 (architect)
   → "Revisa la implementación de autoasignación de tareas"
   → Valida arquitectura, coherencia, permisos

5. Ctrl+Shift+5 (tester)
   → "Escribe tests de integración para autoasignación de tareas"
   → Genera tests con WebApplicationFactory + Testcontainers
```

## Diferencias con SwarmForge

| SwarmForge | Este sistema |
|-----------|-------------|
| Agentes autónomos en tmux | Agentes bajo demanda en kiro-cli |
| Comunicación por handoff files | El usuario es el orquestador |
| Requiere git worktrees | Un solo workspace |
| Flujo fijo (pipeline) | Flujo flexible (tú decides el orden) |
| Backend: codex/claude/grok | Backend: modelo de kiro-cli |

## Uso con subagents (pipelines)

Para tareas que se benefician de paralelismo, puedes usar el sistema de subagentes de kiro-cli:

```
"Implementa la HU-TB-04 (cambiar estado de tarea) completa, backend y frontend"
```

Kiro puede internamente delegar a los agentes backend-dev y frontend-dev en paralelo si configuras los pipelines apropiados.

## Estructura

```
.kiro/
├── agents/
│   ├── specifier.json      # Especificador de requisitos
│   ├── backend-dev.json    # Desarrollador .NET 10
│   ├── frontend-dev.json   # Desarrollador Angular 21
│   ├── architect.json      # Arquitecto / revisor
│   └── tester.json         # Escritor de tests
└── skills/
    ├── angular21/SKILL.md  # Skill Angular 21
    ├── dotnet10/SKILL.md   # Skill .NET 10
    └── domain/SKILL.md     # Skill de dominio
```
