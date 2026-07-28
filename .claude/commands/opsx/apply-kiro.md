---
name: "OPSX: Apply (kiro/Herdr)"
description: Implement tasks from an OpenSpec change by delegating to kiro-cli agents, following the cerebro/manos orchestration model in CLAUDE.md
allowed-tools: Bash(openspec:*), Bash(herdr:*), Bash(kiro-cli:*), Bash(git:*), Bash(dotnet:*), Bash(npx:*), Bash(pnpm:*)
category: Workflow
tags: [workflow, artifacts, kiro, herdr]
---

Implementa las tasks de un change de OpenSpec delegando el trabajo en kiro-cli (las "manos"), en lugar de que Claude Code edite código directamente. Este comando es la versión puente entre `/opsx:*` y el modelo de orquestación agéntica ya documentado en `CLAUDE.md` (sección "Modelo de orquestación agéntica"). `/opsx:apply` sigue existiendo tal cual para cuando quieras que Claude Code implemente él mismo (cambios pequeños, o fuera de Herdr sin ganas de levantar kiro).

**Store selection:** si el usuario nombra un store, usa `openspec store list --json` para resolver el id y añade `--store <id>` a los comandos que leen/escriben el change (`status`, `instructions`, `show`, `validate`, `archive`). Sin store, opera sobre el `openspec/` local del repo.

**Input**: opcionalmente el nombre del change (p. ej. `/opsx:apply-kiro add-project-tags`). Si se omite, infiérelo del contexto de la conversación o, si hay varios changes activos, pregunta con `openspec list --json` + **AskUserQuestion**. Anuncia siempre: "Using change: <name>".

## Paso 1 — Resolver el change y comprobar que está listo para implementar

```bash
openspec status --change "<name>" --json
openspec instructions apply --change "<name>" --json
```

Igual que en `/opsx:apply`: si `state: "blocked"`, indica qué artefacto falta y sugiere `/opsx:update` o `/opsx:propose`. Si `state: "all_done"`, felicita y sugiere `/opsx:archive`. En otro caso, lee todos los `contextFiles` (proposal, design, specs, tasks) antes de continuar.

## Paso 2 — Agrupar las tasks por capa

`tasks.md` usa el formato `## N. <Nombre de grupo>` con checkboxes `- [ ] N.M <descripción>` debajo. Clasifica cada grupo en una capa según el nombre del heading y, si es ambiguo, según las rutas de fichero mencionadas en las descripciones de sus tasks:

| Capa | Señales en el heading/tasks | Agente kiro | `allowedPaths` del agente |
|------|------------------------------|--------------|---------------------------|
| Backend | "backend", "api", "core", "infrastructure", ".net", rutas `Core/`, `Infrastructure/`, `Api/`, `tests/CarteraProyectos.*` | `backend-dev` | `src/CarteraProyectos.{Core,Infrastructure,Api}/**`, `tests/CarteraProyectos.UnitTests/**` |
| Frontend | "frontend", "angular", "ui", rutas `src/frontend/` | `frontend-dev` | `src/frontend/src/**` |
| Tests | "test", "e2e", "cobertura", rutas `tests/**`, `*.spec.ts`, `e2e/**` | `tester` | `tests/**`, `src/frontend/src/**/*.spec.ts` |

Si un grupo no encaja claramente en ninguna fila, **no lo adivines**: usa **AskUserQuestion** para que el usuario lo asigne a una capa. Enrutar mal una task a un agente sin permisos de escritura sobre esos ficheros hace que kiro falle o, peor, escriba fuera de su ámbito.

Muestra el resultado del agrupamiento antes de lanzar nada:
```
## Capas detectadas en <change-name>
- Backend: 4 tasks (grupo "1. Backend")
- Frontend: 3 tasks (grupo "2. Frontend")
- Tests: 1 task (grupo "3. Tests")
```

## Paso 3 — Orden de ejecución

Secuencial, no paralelo: **Backend → Frontend → Tests**, salvo que el usuario pida explícitamente paralelizar y las tasks sean demostrablemente independientes (el frontend normalmente necesita el contrato de API que deja el backend). Salta las capas sin tasks pendientes.

## Paso 4 — Elegir modelo por capa

Los tres agentes `.kiro/agents/{backend-dev,frontend-dev,tester}.json` ya traen `claude-sonnet-4.6` por defecto — es el suelo mínimo, no bajes a `claude-haiku-4.5`. Sube a `claude-opus-4.6` solo si el grupo de tasks de esa capa es excepcionalmente complejo (arquitectura nueva, refactor muy transversal que toca muchos archivos a la vez) — decídelo caso a caso. Documenta la elección en una línea antes de lanzar la capa: `Modelo: claude-sonnet-4.6 — default del agente.`

## Paso 5 — Construir el spec para kiro

Para cada capa, compón el texto de spec (no dupliques el prompt de persona: ya vive en el agente `.kiro/agents/*.json` vía `--agent`). Incluye:
- Resumen del `proposal.md` (qué y por qué, 2-3 líneas)
- Extracto relevante de `design.md` (solo lo que toque a esta capa)
- El bloque exacto de tasks pendientes de esta capa (`- [ ] N.M ...`), tal cual aparecen en `tasks.md`
- Instrucción explícita: *"No marques checkboxes en tasks.md — eso lo hace Claude Code después de revisar tu trabajo."*

## Paso 6 — Lanzar kiro

Detecta el entorno con `echo "$HERDR_ENV"`.

**Dentro de Herdr (`HERDR_ENV=1`)** — pane paralela, no bloquea a Claude Code:
```bash
herdr agent start kiro --cwd "$(pwd)" --split right --no-focus -- \
  kiro-cli chat "<spec-de-la-capa>" --agent <backend-dev|frontend-dev|tester> \
  --no-interactive --trust-all-tools --model <modelo>
# → anota el pane_id devuelto

herdr wait agent-status <pane_id> --status idle --timeout 900000
# fallback si herdr no detecta el estado de kiro como agente:
# herdr wait output <pane_id> --match "<texto final esperado>" --timeout 900000

herdr pane read <pane_id> --source recent --lines 300
```

**Fuera de Herdr** (fallback directo, bloqueante):
```bash
kiro-cli chat "<spec-de-la-capa>" --agent <backend-dev|frontend-dev|tester> \
  --no-interactive --trust-all-tools --model <modelo>
```

## Paso 7 — Revisar el resultado (esto es lo que hace Claude Code de verdad — NO es opcional)

kiro afirmar que ha terminado no es prueba de nada. Antes de marcar ninguna task como hecha:
1. `git status` / `git diff` — confirma qué tocó kiro y que cae dentro de `allowedPaths` de su agente
2. Verifica que compila/pasa tests de esa capa:
   - Backend: `dotnet build src/` y `dotnet test`
   - Frontend: `cd src/frontend && npx ng build` (+ `npx vitest run` si hay specs nuevos)
   - Tests: ejecuta la suite añadida/modificada
3. Compara el diff contra la descripción de cada task del grupo — si no la cubre o el build/tests fallan, **no marques el checkbox**

Solo si build+tests pasan y el diff cubre la task: edita `tasks.md` cambiando `- [ ]` → `- [x]` para esa task concreta.

Si Herdr: `herdr pane close <pane_id>` cuando la capa termina y queda verificada.

## Paso 8 — Repetir para la siguiente capa, luego mostrar estado final

Mismo formato de salida que `/opsx:apply`:
```
## Implementación completada: <change-name> (vía kiro/Herdr)

**Progreso:** N/M tasks completas

### Por capa
- Backend (backend-dev, claude-sonnet-4.6): 4/4 ✓ — pane cerrada
- Frontend (frontend-dev, claude-sonnet-4.6): 3/3 ✓ — pane cerrada
- Tests (tester, claude-sonnet-4.6): 1/1 ✓ — pane cerrada

Todas las tasks completas. Puedes archivar con `/opsx:archive`.
```

Si una capa queda bloqueada (build roto, task ambigua, kiro no responde), **detente ahí** — no sigas con la siguiente capa arrastrando una base rota. Reporta el error concreto, no lo que kiro dijo que hizo, y espera instrucciones.

## Guardrails

- Nunca marques una task como hecha solo porque kiro dijo que la terminó — verifica build/tests/diff siempre
- Nunca inventes un `pane_id` — solo usa el que devuelva `herdr agent start`
- Si el agrupamiento backend/frontend/tests de una task es ambiguo, pregunta — no enrutes a ciegas (el agente de kiro tiene `allowedPaths` restringidos y fallará o hará algo indebido si la task no encaja)
- Respeta el orden Backend → Frontend → Tests salvo petición explícita de paralelizar
- Fuera de Herdr, usa el fallback directo por Bash sin bloquear el flujo — no falles solo porque `HERDR_ENV` no está definida
- No dupliques el prompt de persona de los agentes kiro en el spec — ya está en `.kiro/agents/*.json`; el spec solo aporta el contexto específico del change
- Actualiza `tasks.md` inmediatamente tras verificar cada capa, no esperes a que termine todo el change
