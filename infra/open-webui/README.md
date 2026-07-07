# Open WebUI — tool del agente

- `cartera_tool.py` — tool Python del agente (wrappers del Tool Server `/api/agent` + gráficos matplotlib + exports Excel). **Fuente de verdad**: la copia que corre está en la BD de Open WebUI; tras cambiar este fichero hay que volver a subirlo.
- `system-prompt.md` — prompt de sistema del modelo del agente.
- `push_tool.py` — sube (crea o actualiza) la tool a Open WebUI via API, sin pasar por la UI:

```bash
python infra/open-webui/push_tool.py --key <api-key>          # http://localhost:3000
python infra/open-webui/push_tool.py --key <api-key> --url https://openwebui.produccion
# o exportando OPENWEBUI_API_KEY / OPENWEBUI_URL
```

La API key se genera en Open WebUI en *Settings → Account → API Keys* (el admin debe tener
habilitado *Enable API Key* en *Admin → Settings → General*). También vale el JWT de sesión
de un admin. El script hace upsert: si la tool ya existe (mismo id `cartera_proyectos_tic` o
mismo nombre), la actualiza; si no, la crea.

Nota: las tools también están expuestas como **Tool Server OpenAPI** (`http://backend:8080/openapi/agent.json`,
registrable en *Admin → Settings → Tools*). La tool Python es la vía principal porque añade
gráficos y exports que el Tool Server no puede generar; conviene mantener sus wrappers
alineados con los endpoints de `AgentEndpoints.cs`.
