@AGENTS.md

## Modelo de orquestación agéntica

**Claude Code = cerebro** (planifica, genera specs, revisa output, toma decisiones de arquitectura)
**kiro-cli = manos** (implementa código, ejecuta builds y tests)

### Uso de herramientas — regla de oro

Preferir siempre la herramienta más barata que resuelva el problema:

| Tarea | Herramienta correcta | ❌ No usar |
|-------|---------------------|-----------|
| Buscar símbolo o archivo | `Grep` / `Glob` directamente | Agent:Explore |
| Leer archivo concreto | `Read` directamente | Agent |
| Implementar código | `kiro-cli` via Bash | Agent con Edit/Write |
| Exploración open-ended (>5 ficheros desconocidos) | `Agent:Explore` — solo si proteger contexto es crítico | — |
| Revisión independiente de código | `Agent:code-reviewer` — solo si se necesita perspectiva externa | — |

**El `Agent` tool crea una instancia Sonnet completa que arranca en frío y re-deriva contexto ya conocido — es la ruta cara. Usarlo solo cuando sea imprescindible.**

### Patrón de invocación

```bash
# Desde el directorio del proyecto (para que kiro detecte los agentes locales)
kiro-cli chat "<spec técnica detallada>" \
  --agent <agent-name> \
  --no-interactive \
  --trust-all-tools \
  --model <modelo-elegido>
```

### Ejecución dentro de Herdr: kiro-cli en panes

Si la sesión corre dentro de [Herdr](https://herdr.dev) (detectable por la variable de entorno `HERDR_ENV=1`; `HERDR_PANE_ID` identifica la pane actual), **lanzar kiro-cli en una pane paralela en lugar de bloquear el Bash de Claude Code**. Así el usuario ve el progreso de kiro en directo y Claude Code queda libre mientras espera.

```bash
# 1. Lanzar kiro en una pane nueva (split a la derecha de la pane actual)
herdr agent start kiro --cwd "C:\Angel\git\cartera-proyectos" --split right --no-focus -- \
  kiro-cli chat "<spec técnica detallada>" --agent <agent-name> --no-interactive --trust-all-tools --model <modelo>
# → devuelve el pane_id de la pane creada

# 2. Esperar a que termine (bloqueante, con timeout generoso)
herdr wait agent-status <pane_id> --status idle --timeout 600000
#    (si herdr no detecta kiro como agente, usar: herdr wait output <pane_id> --match "<texto final>" --timeout 600000)

# 3. Leer el resultado y revisar el output como siempre
herdr pane read <pane_id> --source recent --lines 300

# 4. Cerrar la pane cuando ya no haga falta
herdr pane close <pane_id>
```

Notas:
- Si `HERDR_ENV` no está definida, usar el patrón de invocación directo por Bash de arriba (sin cambios).
- Para builds/tests largos (docker compose, Playwright) aplica el mismo patrón: `herdr pane split` + `herdr pane run` + `herdr wait output`.
- Las specs largas con comillas conflictivas pueden escribirse a un fichero temporal y pasarse con `--resume` o interpolación, según convenga.

### Selección de modelo para kiro-cli

Claude Code elige el modelo en función de la complejidad de la tarea. Al final de cada spec generada se incluye la línea:
> **Modelo recomendado:** `<modelo>` — `<razón en una frase>`

| Señal de complejidad | Modelo |
|----------------------|--------|
| Backend CQRS puro (handler + endpoint siguiendo patrón existente) | `claude-sonnet-4.6` |
| Tests unitarios sobre código existente | `claude-sonnet-4.6` |
| Frontend con un único componente aislado | `claude-sonnet-4.6` |
| Frontend con múltiples archivos y contratos TypeScript entre ellos | `claude-sonnet-4.6` |
| Refactor transversal (cambia interfaces que impactan varios archivos) | `claude-sonnet-4.6` |
| Lógica de negocio compleja o máquina de estados nueva | `claude-sonnet-4.6` |

Referencia de coste relativo: Haiku ≈ 0.4×, Sonnet ≈ 1.3× (escala sobre base 1.0).

IDs de modelo exactos para kiro-cli: `claude-haiku-4.5`, `claude-sonnet-4.6`, `claude-opus-4.6` (punto separador, no guión).

### Slash commands y asignación de roles

| Comando | Claude Code hace | kiro-cli hace | Modelo por defecto |
|---------|-----------------|---------------|-------------------|
| `/specifier` | Genera spec directamente | — | Sonnet (cerebro) |
| `/backend-dev` | Lee contexto, genera spec .NET detallada, elige modelo, llama a kiro, revisa output | Implementa handlers, endpoints, tests, migración | Haiku (ajustable) |
| `/frontend-dev` | Lee API y contexto, genera spec Angular detallada, elige modelo, llama a kiro, revisa output | Implementa componentes, servicios, rutas | Sonnet (ajustable) |
| `/architect` | Revisa código directamente | — | Sonnet (cerebro) |
| `/tester` | Define casos de prueba, elige modelo, llama a kiro, revisa cobertura | Escribe tests unitarios, integración y E2E | Haiku (ajustable) |
| `/opsx:apply-kiro` | Agrupa tasks de un change OpenSpec por capa, elige modelo, llama a kiro (pane Herdr si aplica), revisa output | Implementa las tasks de la capa asignada | Haiku (ajustable, por capa) |

### Flujo típico por feature

```
1. /specifier  → Claude Code genera spec técnica (criterios de aceptación, endpoints, DTOs)
2. /backend-dev → Claude Code lee dominio → genera spec detallada → elige modelo → kiro implementa
3. /frontend-dev → Claude Code lee spec API → genera spec Angular → elige modelo → kiro implementa
4. /tester (opcional) → kiro amplía cobertura de tests
5. /architect (opcional) → Claude Code revisa coherencia de lo implementado
```

Los agentes kiro están definidos en `.kiro/agents/`. Claude Code los invoca desde el directorio raíz del proyecto para que kiro detecte los agentes del workspace.

### OpenSpec como complemento (specs versionadas + delegación a kiro/Herdr)

[OpenSpec](https://github.com/Fission-AI/OpenSpec) (`openspec/`, comandos `/opsx:*` en `.claude/commands/opsx/`) es un complemento opcional al flujo de arriba para features que conviene versionar como artefactos (`proposal.md` / `design.md` / `tasks.md` por change en `openspec/changes/<nombre>/`), en vez de specs efímeras en el chat. No sustituye a `/specifier` — puedes usar `/specifier` para pensar la spec y `/opsx:propose` para dejarla como artefactos versionados, o ir directo a `/opsx:propose`.

```
1. /opsx:propose "<idea>" → Claude Code crea el change y genera proposal/design/tasks
2. /opsx:apply-kiro <change> → Claude Code agrupa las tasks por capa (Backend/Frontend/Tests),
                                 elige modelo por capa y delega en kiro-cli (pane Herdr si HERDR_ENV=1),
                                 revisa build/tests/diff antes de marcar cada task como hecha
3. /opsx:archive <change>  → una vez todas las tasks están completas y verificadas
```

`/opsx:apply` (generado por OpenSpec, sin sufijo `-kiro`) sigue disponible tal cual: hace que Claude Code implemente directamente, útil para changes pequeños o fuera de Herdr. `/opsx:apply-kiro` es la variante que respeta el modelo cerebro/manos de esta sección — úsala por defecto cuando el change vaya a tocar código real.
