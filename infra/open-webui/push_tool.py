"""
Sube (crea o actualiza) la tool `cartera_tool.py` a Open WebUI via API REST.

Uso:
    python push_tool.py --key <api-key-o-jwt> [--url http://localhost:3000]
    (o con la variable de entorno OPENWEBUI_API_KEY definida)

La API key se genera en Open WebUI: Settings -> Account -> API Keys
(requiere que el admin tenga habilitado "Enable API Key" en Admin -> Settings -> General).
También sirve el JWT de una sesión de admin (token de "Get session").

Solo usa la librería estándar: no requiere pip install.
"""

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request

TOOL_ID = "cartera_proyectos_tic"
TOOL_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "cartera_tool.py")


def read_tool() -> tuple[str, str, str, str]:
    """Devuelve (content, title, description, version) leyendo el frontmatter del docstring."""
    with open(TOOL_FILE, encoding="utf-8") as f:
        content = f.read()

    def frontmatter(key: str) -> str:
        m = re.search(rf"^{key}:\s*(.+)$", content, re.MULTILINE)
        return m.group(1).strip() if m else ""

    return content, frontmatter("title"), frontmatter("description"), frontmatter("version")


def api(url: str, key: str, path: str, body: dict | None = None) -> tuple[int, dict | list | None]:
    req = urllib.request.Request(
        f"{url.rstrip('/')}{path}",
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
        method="POST" if body is not None else "GET",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as r:
            return r.status, json.loads(r.read().decode() or "null")
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read().decode() or "null")


def main() -> int:
    parser = argparse.ArgumentParser(description="Sube cartera_tool.py a Open WebUI")
    parser.add_argument("--url", default=os.environ.get("OPENWEBUI_URL", "http://localhost:3000"))
    parser.add_argument("--key", default=os.environ.get("OPENWEBUI_API_KEY", ""))
    args = parser.parse_args()

    if not args.key:
        print("ERROR: falta la API key (--key o variable OPENWEBUI_API_KEY).", file=sys.stderr)
        return 1

    content, title, description, version = read_tool()
    payload = {
        "id": TOOL_ID,
        "name": title or "Cartera de Proyectos TIC",
        "content": content,
        "meta": {"description": description, "manifest": {}},
    }

    status, tools = api(args.url, args.key, "/api/v1/tools/")
    if status != 200:
        print(f"ERROR consultando tools existentes (HTTP {status}): {tools}", file=sys.stderr)
        print("¿API key válida? ¿'Enable API Key' activado en Admin -> Settings -> General?", file=sys.stderr)
        return 1

    # Si la tool ya existe (por id fijo, o creada a mano con otro id pero mismo nombre), actualizarla
    existing_id = None
    for t in tools or []:
        if t.get("id") == TOOL_ID or t.get("name") == payload["name"]:
            existing_id = t["id"]
            break

    if existing_id:
        payload["id"] = existing_id
        status, result = api(args.url, args.key, f"/api/v1/tools/id/{existing_id}/update", payload)
        action = "actualizada"
    else:
        status, result = api(args.url, args.key, "/api/v1/tools/create", payload)
        action = "creada"

    if status != 200 or result is None:
        print(f"ERROR subiendo la tool (HTTP {status}): {result}", file=sys.stderr)
        return 1

    print(f"Tool '{payload['name']}' {action} correctamente (id={payload['id']}, version={version}).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
