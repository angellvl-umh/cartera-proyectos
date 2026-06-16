"""
title: Cartera de Proyectos TIC
description: Herramientas para consultar y gestionar la cartera de proyectos TIC universitaria. Permite al agente IA consultar tareas, proyectos, capacidad de equipos, realizar acciones como cambiar estados o crear tareas, y generar gráficos visuales.
author: Cartera TIC
version: 1.1.0
required_open_webui_version: 0.5.0
requirements: matplotlib
"""

import io
import json
import requests

import matplotlib
matplotlib.use("Agg")  # backend sin display, obligatorio en Docker
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from pydantic import BaseModel
from typing import Optional


class Tools:
    class Valves(BaseModel):
        base_url: str = "http://backend:8080/api/agent"
        api_key: str = "cartera-agent-key"
        external_url: str = "http://localhost:5000"  # URL pública del backend (accesible desde el navegador)

    def __init__(self):
        self.valves = self.Valves()

    def _headers(self, email: str) -> dict:
        return {
            "Authorization": f"Bearer {self.valves.api_key}",
            "X-Open-WebUI-User-Email": email,
            "Content-Type": "application/json",
        }

    def _get(self, path: str, email: str, params: dict = None) -> dict:
        try:
            r = requests.get(
                f"{self.valves.base_url}{path}",
                headers=self._headers(email),
                params=params,
                timeout=10,
            )
            return r.json() if r.ok else {"error": r.status_code, "detail": r.text}
        except Exception as e:
            return {"error": str(e)}

    def _post(self, path: str, email: str, body: dict = None) -> dict:
        try:
            r = requests.post(
                f"{self.valves.base_url}{path}",
                headers=self._headers(email),
                json=body or {},
                timeout=10,
            )
            return r.json() if r.ok else {"error": r.status_code, "detail": r.text}
        except Exception as e:
            return {"error": str(e)}

    def get_my_tasks(self, __user__: dict) -> str:
        """
        Obtener mis tareas pendientes.
        Devuelve las tareas activas, en backlog y completadas del usuario.
        Úsalo cuando el usuario pregunte '¿qué tengo pendiente?', '¿en qué estoy trabajando?', '¿cuáles son mis tareas?'
        """
        result = self._get("/me", __user__.get("email", ""))
        return json.dumps(result, ensure_ascii=False)

    def get_projects(
        self,
        __user__: dict,
        sipt_group: Optional[str] = None,
        status: Optional[str] = None,
    ) -> str:
        """
        Listar proyectos del usuario, con filtros opcionales.
        Devuelve los proyectos del usuario con su estado y progreso de tareas.
        Úsalo cuando el usuario pregunte por sus proyectos o el estado general de la cartera.
        :param sipt_group: Filtrar por grupo SIPT. Valores válidos: WebTransversal, RRHH, Academico, Sede, Observatorio, InvestigacionEconomico
        :param status: Filtrar por estado. Valores válidos: Stopped, PlanningWithClient, WaitingForDevelopers, PlanningSprint, InSprint, DevelopmentOutsideSprint, InTesting, Completed, PostponedByClient
        """
        params: dict = {}
        if sipt_group:
            params["siptGroup"] = sipt_group
        if status:
            params["status"] = status
        result = self._get("/projects", __user__.get("email", ""), params=params or None)
        return json.dumps(result, ensure_ascii=False)

    def get_project_detail(self, project_id: int, __user__: dict) -> str:
        """
        Obtener detalle de un proyecto.
        Devuelve información detallada: estado, sprints activos, tareas pendientes y progreso.
        Úsalo cuando el usuario pregunte '¿cómo va el proyecto X?' o pida info sobre un proyecto concreto.
        :param project_id: ID numérico del proyecto
        """
        result = self._get(f"/projects/{project_id}", __user__.get("email", ""))
        return json.dumps(result, ensure_ascii=False)

    def get_capacity(self, __user__: dict) -> str:
        """
        Consultar carga y disponibilidad de equipos.
        Devuelve la carga de trabajo de todos los equipos y sus miembros.
        Green = disponible (≤3 tareas activas), Yellow = cargado (4-6), Red = saturado (≥7).
        Úsalo cuando el usuario pregunte qué equipo tiene disponibilidad para un nuevo proyecto.
        """
        result = self._get("/capacity", __user__.get("email", ""))
        return json.dumps(result, ensure_ascii=False)

    def search_tasks(self, query: str, __user__: dict) -> str:
        """
        Buscar tarea por descripción usando búsqueda semántica.
        Devuelve las 5 tareas más similares al texto proporcionado.
        Úsalo para identificar una tarea concreta cuando el usuario la mencione de forma natural,
        por ejemplo 'la tarea del proxy SSL' o 'lo del certificado'.
        :param query: Texto a buscar de forma natural
        """
        result = self._get("/tasks/search", __user__.get("email", ""), params={"q": query})
        return json.dumps(result, ensure_ascii=False)

    def update_task_status(self, task_id: int, status: str, __user__: dict) -> str:
        """
        Cambiar el estado de una tarea.
        Estados válidos: Backlog, ToDo, InProgress, Blocked, Done. Done es terminal y no puede revertirse.
        IMPORTANTE: confirma siempre con el usuario antes de ejecutar este cambio.
        Úsalo cuando el usuario diga que terminó una tarea, que está bloqueado o que empieza a trabajar en algo.
        :param task_id: ID numérico de la tarea
        :param status: Nuevo estado — uno de: Backlog, ToDo, InProgress, Blocked, Done
        """
        result = self._post(
            f"/tasks/{task_id}/status",
            __user__.get("email", ""),
            {"status": status},
        )
        return json.dumps(result, ensure_ascii=False)

    def create_task(
        self,
        project_id: int,
        title: str,
        __user__: dict,
        description: Optional[str] = None,
        priority: Optional[str] = "Medium",
        epic_id: Optional[int] = None,
        sprint_id: Optional[int] = None,
        assign_to_self: Optional[bool] = True,
    ) -> str:
        """
        Crear una nueva tarea en un proyecto.
        Úsalo cuando el usuario quiera registrar trabajo pendiente.
        :param project_id: ID numérico del proyecto
        :param title: Título de la tarea
        :param description: Descripción opcional
        :param priority: Prioridad — Low, Medium, High o Critical. Por defecto Medium.
        :param epic_id: ID de la épica (opcional)
        :param sprint_id: ID del sprint al que asignar la tarea (opcional)
        :param assign_to_self: Si true (por defecto), asigna la tarea al usuario que la crea
        """
        body = {
            "projectId": project_id,
            "title": title,
            "description": description,
            "priority": priority,
            "epicId": epic_id,
            "sprintId": sprint_id,
            "assignToSelf": assign_to_self,
        }
        result = self._post("/tasks", __user__.get("email", ""), body)
        return json.dumps(result, ensure_ascii=False)

    def add_task_comment(self, task_id: int, text: str, __user__: dict) -> str:
        """
        Añadir un comentario o nota de seguimiento a una tarea existente.
        El comentario queda registrado con el autor y la fecha actual.
        Úsalo cuando el usuario quiera documentar avances, bloqueos o novedades sobre una tarea.
        :param task_id: ID numérico de la tarea
        :param text: Texto del comentario
        """
        result = self._post(
            f"/tasks/{task_id}/comment",
            __user__.get("email", ""),
            {"text": text},
        )
        return json.dumps(result, ensure_ascii=False)

    def add_project_note(self, project_id: int, text: str, __user__: dict) -> str:
        """
        Añadir una nota de seguimiento a un proyecto.
        La nota queda registrada con el autor y la fecha actual.
        Úsalo cuando el usuario quiera documentar decisiones, hitos, bloqueos o novedades sobre un proyecto concreto.
        :param project_id: ID numérico del proyecto
        :param text: Texto de la nota de seguimiento
        """
        result = self._post(
            f"/projects/{project_id}/notes",
            __user__.get("email", ""),
            {"text": text},
        )
        return json.dumps(result, ensure_ascii=False)

    def reindex(self, __user__: dict) -> str:
        """
        Regenerar el índice de embeddings para búsqueda semántica.
        Genera o actualiza los vectores de todas las tareas. Ejecutar después de crear o modificar tareas masivamente.
        Puede tardar varios segundos según el número de tareas.
        """
        result = self._post("/reindex", __user__.get("email", ""))
        return json.dumps(result, ensure_ascii=False)

    # ── Charts ────────────────────────────────────────────────────────────────

    def _fig_to_md(self, fig) -> str:
        buf = io.BytesIO()
        fig.savefig(buf, format="png", bbox_inches="tight", dpi=96)
        plt.close(fig)
        buf.seek(0)
        # Subir el PNG al backend y devolver solo una URL corta (~80 chars)
        # para no inflar el contexto del LLM con miles de tokens de base64
        r = requests.post(
            f"{self.valves.base_url}/charts",
            headers={"Authorization": f"Bearer {self.valves.api_key}",
                     "Content-Type": "application/octet-stream"},
            data=buf.getvalue(),
            timeout=15,
        )
        if not r.ok:
            return f"Error guardando gráfico: {r.status_code}"
        chart_id = r.json()["id"]
        url = f"{self.valves.external_url}/api/agent/charts/{chart_id}"
        return f"![gráfico]({url})"

    def chart_team_capacity(self, __user__: dict) -> str:
        """
        Generar gráfico de carga de trabajo por equipo.
        Muestra las tareas activas de cada miembro con código de color:
        verde (≤3 tareas, disponible), amarillo (4-6, cargado), rojo (≥7, saturado).
        Úsalo cuando el usuario pida ver la capacidad de los equipos de forma visual.
        """
        data = self._get("/capacity", __user__.get("email", ""))
        if "error" in data:
            return json.dumps(data, ensure_ascii=False)

        COLOR_MAP = {"Green": "#4caf50", "Yellow": "#ff9800", "Red": "#f44336"}
        rows = []

        for team in data.get("teams", []):
            for m in team.get("members", []):
                rows.append({
                    "label": f"{m['name']} ({team['teamName']})",
                    "value": m["activeTasks"],
                    "color": COLOR_MAP.get(m["loadLevel"], "#9e9e9e"),
                })

        if not rows:
            return "No hay datos de capacidad disponibles."

        # Ordenar por carga descendente y limitar a 15 para no exceder el contexto
        rows.sort(key=lambda r: r["value"], reverse=True)
        rows = rows[:15]
        names  = [r["label"] for r in rows]
        values = [r["value"] for r in rows]
        colors = [r["color"] for r in rows]

        fig, ax = plt.subplots(figsize=(7, max(3, len(names) * 0.4)))
        bars = ax.barh(names, values, color=colors, edgecolor="white", height=0.6)
        ax.bar_label(bars, padding=4, fontsize=9)
        ax.set_xlabel("Tareas activas")
        ax.set_title("Carga de trabajo por persona", fontweight="bold", pad=12)
        ax.invert_yaxis()
        ax.set_xlim(0, max(values or [1]) + 2)
        ax.spines[["top", "right"]].set_visible(False)

        legend = [
            mpatches.Patch(color="#4caf50", label="Disponible (≤3)"),
            mpatches.Patch(color="#ff9800", label="Cargado (4-6)"),
            mpatches.Patch(color="#f44336", label="Saturado (≥7)"),
        ]
        ax.legend(handles=legend, loc="lower right", fontsize=8)
        fig.tight_layout()
        return self._fig_to_md(fig)

    def chart_project_progress(self, __user__: dict) -> str:
        """
        Generar gráfico de progreso de proyectos.
        Muestra tareas completadas vs pendientes para cada proyecto del usuario.
        Úsalo cuando el usuario quiera ver visualmente el avance de los proyectos.
        """
        data = self._get("/projects", __user__.get("email", ""))
        if "error" in data or not isinstance(data, list) or not data:
            return "No hay proyectos disponibles para mostrar."

        titles = [p["title"][:28] + "…" if len(p["title"]) > 28 else p["title"] for p in data]
        done    = [p["doneTasks"] for p in data]
        pending = [p["totalTasks"] - p["doneTasks"] for p in data]

        x = range(len(titles))
        fig, ax = plt.subplots(figsize=(max(6, len(titles) * 1.1), 4))
        w = 0.38
        b1 = ax.bar([i - w / 2 for i in x], done,    width=w, label="Completadas", color="#4caf50", edgecolor="white")
        b2 = ax.bar([i + w / 2 for i in x], pending, width=w, label="Pendientes",  color="#90a4ae", edgecolor="white")
        ax.bar_label(b1, padding=3, fontsize=8)
        ax.bar_label(b2, padding=3, fontsize=8)
        ax.set_xticks(list(x))
        ax.set_xticklabels(titles, rotation=22, ha="right", fontsize=9)
        ax.set_ylabel("Tareas")
        ax.set_title("Progreso de proyectos", fontweight="bold", pad=12)
        ax.legend(fontsize=9)
        ax.spines[["top", "right"]].set_visible(False)
        fig.tight_layout()
        return self._fig_to_md(fig)

    def chart_my_tasks_by_status(self, __user__: dict) -> str:
        """
        Generar gráfico de distribución de mis tareas por estado.
        Muestra un gráfico de donut con las tareas del usuario agrupadas por estado.
        Úsalo cuando el usuario quiera ver un resumen visual de su carga de trabajo actual.
        """
        data = self._get("/me", __user__.get("email", ""))
        if "error" in data:
            return json.dumps(data, ensure_ascii=False)

        STATUS_COLORS = {
            "InProgress": "#2196f3",
            "ToDo":       "#ff9800",
            "Backlog":    "#9e9e9e",
            "Blocked":    "#f44336",
            "Done":       "#4caf50",
        }
        STATUS_LABELS = {
            "InProgress": "En progreso",
            "ToDo":       "Por hacer",
            "Backlog":    "Backlog",
            "Blocked":    "Bloqueada",
            "Done":       "Hecha",
        }

        counts: dict[str, int] = {}
        for task in data.get("activeTasks", []):
            counts[task["status"]] = counts.get(task["status"], 0) + 1
        for task in data.get("backlogTasks", []):
            counts["Backlog"] = counts.get("Backlog", 0) + 1
        done_count = data.get("doneTasks", 0)
        if done_count:
            counts["Done"] = done_count

        if not counts:
            return "No tienes tareas registradas."

        labels  = [STATUS_LABELS.get(k, k) for k in counts]
        sizes   = list(counts.values())
        colors  = [STATUS_COLORS.get(k, "#bdbdbd") for k in counts]
        total   = sum(sizes)

        fig, ax = plt.subplots(figsize=(5, 4))
        wedges, _, autotexts = ax.pie(
            sizes, labels=None, colors=colors,
            autopct=lambda p: f"{p:.0f}%\n({int(round(p * total / 100))})",
            pctdistance=0.72, startangle=90,
            wedgeprops={"edgecolor": "white", "linewidth": 2},
        )
        for at in autotexts:
            at.set_fontsize(8)

        centre = plt.Circle((0, 0), 0.48, color="white")
        ax.add_patch(centre)
        ax.text(0, 0, f"{total}\ntareas", ha="center", va="center", fontsize=11, fontweight="bold")
        ax.legend(wedges, labels, loc="lower center", bbox_to_anchor=(0.5, -0.08),
                  ncol=3, fontsize=8, frameon=False)
        ax.set_title(f"Mis tareas — {data.get('userName', '')}", fontweight="bold", pad=14)
        fig.tight_layout()
        return self._fig_to_md(fig)
