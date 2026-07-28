@AGENTS.md

## Modelo de orquestación agéntica

**Claude Code = cerebro** (planifica, genera specs, revisa output, toma decisiones de arquitectura)
**kiro-cli = manos** (implementa código, ejecuta builds y tests)

### Modo coordinador (comportamiento por defecto)

Cuando el usuario habla en lenguaje natural sin invocar un comando concreto (`/specifier`, `/backend-dev`, `/opsx:*`, ...), Claude Code actúa como **coordinador**: es el único punto de contacto con el usuario, decide qué agentes levantar en cada fase y reporta el resultado. El usuario no necesita saber qué slash command toca — eso lo decide el coordinador.

**Mensaje de bienvenida:** en el primer turno de una conversación nueva en este repo, si el usuario no invocó ya un comando explícito, abre con una presentación breve en este estilo: *"Soy el coordinador de Cartera de Proyectos TIC. Cuéntame qué necesitas desarrollar, o pregúntame lo que quieras saber del proyecto."* Sáltatela si el primer mensaje ya trae una petición concreta — resuélvela directamente, la bienvenida no debe interponerse.

**Flujo cuando el usuario pide construir algo:**

1. **Especificar** — si la petición ya es lo bastante precisa (criterios de aceptación claros, alcance acotado), ve directo a `openspec propose` (lógica de `/opsx:propose`) para dejarla como artefactos versionados (`proposal.md`/`design.md`/`tasks.md`). Si es ambigua, pregunta primero lo necesario (**AskUserQuestion** para decisiones que no puedas inferir del código o la conversación) — no crees el change hasta tener claridad.
2. **Implementar** — con el change listo (`openspec status` sin bloqueos), aplica la lógica completa de `/opsx:apply-kiro`: agrupa las tasks por capa, elige modelo por capa, delega en kiro-cli (pane Herdr si `HERDR_ENV=1`), y **verifica build/tests/diff de cada capa antes de marcarla como hecha** — nunca te fíes de que kiro diga que terminó.
3. **Avisar** — al completar todas las capas, resume qué se implementó, el estado de `tasks.md` y si procede archivar (`/opsx:archive`). Si una capa se bloquea, detente ahí y reporta el problema concreto — no encadenes capas sobre una base rota.

Los comandos `/specifier`, `/backend-dev`, `/frontend-dev`, `/architect`, `/tester` y `/opsx:*` siguen existiendo tal cual — son atajos manuales para cuando el usuario (o el propio coordinador) quiere entrar directo a una fase concreta sin pasar por el flujo guiado. Invocar uno de ellos explícitamente tiene prioridad sobre el modo coordinador.

Este modo por defecto es específico de Claude Code (usa `kiro-cli`/Herdr, documentados solo en este fichero); no aplica a sesiones de opencode, que siguen usando sus comandos `/opsx-*` y roles de forma manual.

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

⚠️ **Falso positivo conocido de `herdr wait agent-status --status idle` con kiro-cli**: en pruebas reales, `agent_status` ha reportado `idle` mientras kiro seguía escribiendo/editando ficheros (comprobado con `git status`/`git diff` mostrando cambios llegando minutos después de que el `wait` ya hubiera devuelto). No te fíes de un solo `idle` como señal de "terminó". Antes de leer el pane o revisar el diff, confirma con uno de estos dos métodos:

```bash
# Método A (recomendado): sondear que el pane deja de crecer
prev=-1
for i in $(seq 1 20); do
  sleep 15
  cur=$(herdr pane get <pane_id> 2>&1 | grep -o '"max_offset_from_bottom":[0-9]*' | grep -o '[0-9]*')
  [ "$cur" = "$prev" ] && break
  prev=$cur
done
# Cuando --no-interactive termina de verdad, kiro cierra el proceso y la pane
# desaparece: `herdr pane get <pane_id>` devuelve pane_not_found. Esa es la señal más fiable.

# Método B: reintentar herdr wait agent-status una segunda vez tras una pausa,
# y solo confiar si además `git status`/`git diff` dejaron de cambiar entre dos lecturas.
```

Notas:
- Si `HERDR_ENV` no está definida, usar el patrón de invocación directo por Bash de arriba (sin cambios).
- Para builds/tests largos (docker compose, Playwright) aplica el mismo patrón: `herdr pane split` + `herdr pane run` + `herdr wait output`.
- Las specs largas con comillas conflictivas pueden escribirse a un fichero temporal y pasarse con `--resume` o interpolación, según convenga.
- `herdr pane read` solo admite `--source visible|recent|recent-unwrapped` (no `all`); con transcripts largos, `recent-unwrapped --lines <N alto>` es el que más contexto retiene, pero aun así puede no cubrir toda la sesión — el diff real (`git diff`) es siempre la fuente de verdad, no el texto del pane.

### Alternativa opt-in: opencode con Sonnet 5 para frontend

kiro-cli solo tiene acceso a modelos de AWS Bedrock (`claude-haiku-4.5`, `claude-sonnet-4.6`, `claude-opus-4.6`). Si el usuario pide explícitamente probar otro agente o usar **Sonnet 5** para una tarea de frontend, usa `opencode` (vía GitHub Copilot, ya autenticado — `opencode providers list` lo confirma) en lugar de kiro-cli para esa capa. No sustituye a kiro por defecto en el modo coordinador ni en `/opsx:apply-kiro` — es una opción manual cuando el usuario la pide.

No hace falta crear un agente opencode nuevo: `opencode run --command frontend-dev` ya reutiliza `.opencode/commands/frontend-dev.md`, que importa el mismo `.ai/roles/frontend-dev.md` que usa kiro — misma persona, mismas convenciones, sin duplicar nada.

```bash
# Invocación directa (equivalente a --no-interactive --trust-all-tools de kiro)
opencode run --command frontend-dev "<spec técnica detallada>" \
  --model github-copilot/claude-sonnet-5 \
  --auto --format json
```

⚠️ IDs de modelo con formato distinto al de kiro: opencode usa `<provider>/<modelo>` (p. ej. `github-copilot/claude-sonnet-5`), no el ID plano de Bedrock. `opencode models` lista todo lo disponible; `opencode providers list` confirma qué proveedores están autenticados.

Dentro de Herdr, mismo patrón de pane que con kiro:

```bash
herdr agent start opencode --cwd "$(pwd)" --split right --no-focus -- \
  opencode run --command frontend-dev "<spec>" --model github-copilot/claude-sonnet-5 --auto --format json
# → anota el pane_id

herdr wait agent-status <pane_id> --status idle --timeout 900000
herdr pane read <pane_id> --source recent-unwrapped --lines 500
```

A favor de opencode aquí: Herdr instala un plugin propio (`~/.config/opencode/plugins/herdr-agent-state.js`) que escucha los eventos `session.status` (idle/busy/retry) de opencode directamente — es una integración basada en eventos, no en heurísticas sobre el texto del pane, así que en teoría no debería sufrir el mismo falso positivo documentado arriba para kiro-cli. Aun así, verifica siempre con `git status`/`git diff` antes de dar una capa por terminada — la disciplina de "no te fíes, verifica" aplica igual sea cual sea el agente.

Mismo guardrail que con kiro: nunca marques una task como hecha solo porque opencode dijo que terminó — build/tests/diff siempre antes de marcar el checkbox.

### Selección de modelo para kiro-cli

Todos los agentes kiro (`backend-dev`, `frontend-dev`, `tester`) traen `claude-sonnet-4.6` como modelo por defecto en su `.kiro/agents/*.json` — es el suelo mínimo, no se baja a Haiku. Al final de cada spec generada se incluye la línea:
> **Modelo recomendado:** `<modelo>` — `<razón en una frase>`

Sube a `claude-opus-4.6` solo para tareas excepcionalmente complejas (arquitectura nueva, refactors muy transversales que tocan muchos archivos a la vez) — se decide caso a caso, no hay tabla de señales automática todavía.

Opus es sensiblemente más caro que Sonnet — resérvalo para los casos excepcionales de arriba, no lo pidas por defecto.

IDs de modelo exactos para kiro-cli: `claude-haiku-4.5`, `claude-sonnet-4.6`, `claude-opus-4.6` (punto separador, no guión).

### Slash commands y asignación de roles

| Comando | Claude Code hace | kiro-cli hace | Modelo por defecto |
|---------|-----------------|---------------|-------------------|
| `/specifier` | Genera spec directamente | — | Sonnet (cerebro) |
| `/backend-dev` | Lee contexto, genera spec .NET detallada, elige modelo, llama a kiro, revisa output | Implementa handlers, endpoints, tests, migración | Sonnet (ajustable) |
| `/frontend-dev` | Lee API y contexto, genera spec Angular detallada, elige modelo, llama a kiro, revisa output | Implementa componentes, servicios, rutas | Sonnet (ajustable) |
| `/architect` | Revisa código directamente | — | Sonnet (cerebro) |
| `/tester` | Define casos de prueba, elige modelo, llama a kiro, revisa cobertura | Escribe tests unitarios, integración y E2E | Sonnet (ajustable) |
| `/opsx:apply-kiro` | Agrupa tasks de un change OpenSpec por capa, elige modelo, llama a kiro (pane Herdr si aplica), revisa output | Implementa las tasks de la capa asignada | Sonnet (ajustable, por capa) |

### Flujo típico por feature

Esta secuencia es la que ejecuta el modo coordinador automáticamente (ver arriba); se documenta aquí para invocación manual fase a fase.

```
1. /specifier  → Claude Code genera spec técnica (criterios de aceptación, endpoints, DTOs)
2. /backend-dev → Claude Code lee dominio → genera spec detallada → elige modelo → kiro implementa
3. /frontend-dev → Claude Code lee spec API → genera spec Angular → elige modelo → kiro implementa
4. /tester (opcional) → kiro amplía cobertura de tests
5. /architect (opcional) → Claude Code revisa coherencia de lo implementado
```

Los agentes kiro están definidos en `.kiro/agents/`. Claude Code los invoca desde el directorio raíz del proyecto para que kiro detecte los agentes del workspace.

### OpenSpec como complemento (specs versionadas + delegación a kiro/Herdr)

[OpenSpec](https://github.com/Fission-AI/OpenSpec) (`openspec/`, comandos `/opsx:*` en `.claude/commands/opsx/`) es el mecanismo que usa el modo coordinador por debajo (ver arriba) para dejar cada feature como artefactos versionados (`proposal.md` / `design.md` / `tasks.md` por change en `openspec/changes/<nombre>/`), en vez de specs efímeras en el chat. También puedes invocarlo a mano: `/specifier` para pensar la spec y `/opsx:propose` para dejarla como artefactos versionados, o ir directo a `/opsx:propose`.

```
1. /opsx:propose "<idea>" → Claude Code crea el change y genera proposal/design/tasks
2. /opsx:apply-kiro <change> → Claude Code agrupa las tasks por capa (Backend/Frontend/Tests),
                                 elige modelo por capa y delega en kiro-cli (pane Herdr si HERDR_ENV=1),
                                 revisa build/tests/diff antes de marcar cada task como hecha
3. /opsx:archive <change>  → una vez todas las tasks están completas y verificadas
```

`/opsx:apply` (generado por OpenSpec, sin sufijo `-kiro`) sigue disponible tal cual: hace que Claude Code implemente directamente, útil para changes pequeños o fuera de Herdr. `/opsx:apply-kiro` es la variante que respeta el modelo cerebro/manos de esta sección — úsala por defecto cuando el change vaya a tocar código real.
