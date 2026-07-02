import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzDividerModule } from 'ng-zorro-antd/divider';

// ── Tipos ─────────────────────────────────────────────────────────────────────

export interface ActiveTaskDto {
  workItemId: number;
  title: string;
  status: string;       // 'InProgress' | 'Blocked'
  priority: string;     // 'Low' | 'Medium' | 'High' | 'Critical'
  type: string;         // 'Task' | 'UserStory'
  projectId: number;
  projectTitle: string;
  sprintName: string | null;
  dueDate: string | null;
  isHito: boolean;
}

export interface PersonActivityDto {
  personId: number;
  name: string;
  role: string;
  activeTasks: ActiveTaskDto[];
}

export interface TeamActivityDto {
  teamId: number;
  teamName: string;
  leadName: string | null;
  members: PersonActivityDto[];
}

// ── Etiquetas y colores ────────────────────────────────────────────────────────

const ROLE_LABELS: Record<string, string> = {
  Gestor: 'Gestor',
  JefeEquipo: 'Jefe equipo',
  Desarrollador: 'Desarrollador',
};

const STATUS_CONFIG: Record<string, { label: string; color: string; bg: string }> = {
  Blocked:    { label: 'Bloqueada',    color: '#ff4d4f', bg: '#fff1f0' },
  InProgress: { label: 'En progreso',  color: '#fa8c16', bg: '#fff7e6' },
};

const PRIORITY_COLORS: Record<string, string> = {
  Low:      '#52c41a',
  Medium:   '#1890ff',
  High:     '#fa8c16',
  Critical: '#ff4d4f',
};

const AVATAR_COLORS = [
  '#1890ff', '#13c2c2', '#52c41a', '#722ed1',
  '#eb2f96', '#fa8c16', '#2f54eb', '#08979c',
];

function avatarColor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
  return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
}

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-team-activity',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    NzCardModule, NzTagModule, NzIconModule, NzSpinModule, NzEmptyModule,
    NzTooltipModule, NzButtonModule, NzAvatarModule, NzDividerModule,
  ],
  styles: [`
    .page-header { margin-bottom: 28px; }
    .page-header h2 { margin: 0 0 4px; font-size: 22px; }
    .page-header p  { margin: 0; color: #8c8c8c; font-size: 13px; }

    /* Grid de equipos */
    .teams-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(460px, 1fr));
      gap: 20px;
    }

    /* Cabecera del equipo */
    .team-header { margin-bottom: 16px; }
    .team-name { font-size: 16px; font-weight: 700; color: #262626; margin: 0 0 2px; }
    .team-meta { font-size: 12px; color: #8c8c8c; }

    /* Parrilla de personas */
    .persons-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 12px;
    }

    /* Tarjeta persona activa */
    .person-card {
      background: #fafafa;
      border: 1px solid #f0f0f0;
      border-radius: 10px;
      padding: 12px;
    }
    .person-card-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 10px;
    }
    .person-name { font-weight: 700; font-size: 13px; color: #262626; }
    .person-role { font-size: 11px; color: #8c8c8c; }

    /* Tarjeta persona disponible (compacta) */
    .person-card-available {
      background: #f6ffed;
      border: 1px solid #b7eb8f;
    }

    /* Tarjeta de tarea individual */
    .task-item {
      display: flex;
      flex-direction: column;
      gap: 3px;
      padding: 8px 10px;
      background: #fff;
      border-radius: 8px;
      border: 1px solid #f0f0f0;
      margin-bottom: 6px;
    }
    .task-item:last-child { margin-bottom: 0; }
    .task-title {
      font-size: 12.5px;
      font-weight: 600;
      color: #262626;
      text-decoration: none;
      line-height: 1.4;
    }
    .task-title:hover { text-decoration: underline; color: #1890ff; }
    .task-meta { font-size: 11px; color: #8c8c8c; }
    .task-duedate { font-size: 11px; }
    .overdue { color: #ff4d4f; font-weight: 600; }

    /* Sección disponibles */
    .available-section { margin-top: 16px; }
    .available-label {
      font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
      text-transform: uppercase; color: #8c8c8c;
      margin-bottom: 8px;
    }
    .available-chips {
      display: flex; flex-wrap: wrap; gap: 8px;
    }
    .available-chip {
      display: flex; align-items: center; gap: 8px;
      background: #f6ffed; border: 1px solid #b7eb8f;
      border-radius: 20px; padding: 4px 12px 4px 6px;
    }
    .available-chip-name { font-size: 12px; font-weight: 600; color: #389e0d; }
  `],
  template: `
    <!-- Cabecera -->
    <div class="page-header" style="display:flex;align-items:flex-start;justify-content:space-between;flex-wrap:wrap;gap:12px">
      <div>
        <h2>
          <span nz-icon nzType="eye" style="color:#1890ff;margin-right:8px"></span>
          Trabajo en curso
        </h2>
        <p>¿En qué tarea está cada persona ahora mismo? Tareas activas (En progreso + Bloqueadas) por equipo.</p>
      </div>
      <div style="display:flex;gap:8px;flex-wrap:wrap">
        <a routerLink="/capacity" nz-button>
          <span nz-icon nzType="team"></span> Capacidad
        </a>
        <a routerLink="/capacity/forecast" nz-button>
          <span nz-icon nzType="line-chart"></span> Previsión anual
        </a>
      </div>
    </div>

    <!-- Estado de carga -->
    @if (data() === undefined) {
      <div style="display:flex;justify-content:center;padding:80px">
        <nz-spin nzSize="large" />
      </div>
    } @else if (data()!.length === 0) {
      <nz-empty nzNotFoundContent="No hay equipos configurados" />
    } @else {
      <div class="teams-grid">
        @for (team of data()!; track team.teamId) {
          <nz-card [nzBordered]="true">
            <!-- Cabecera equipo -->
            <div class="team-header">
              <div class="team-name">
                <span nz-icon nzType="team" style="color:#1890ff;margin-right:6px"></span>
                {{ team.teamName }}
              </div>
              <div class="team-meta">
                @if (team.leadName) { 👤 {{ team.leadName }} · }
                {{ activeCount(team) }} activ{{ activeCount(team) !== 1 ? 'as' : 'a' }} ·
                {{ availableMembers(team).length }} disponible{{ availableMembers(team).length !== 1 ? 's' : '' }}
              </div>
            </div>

            @if (team.members.length === 0) {
              <nz-empty nzNotFoundContent="Sin miembros" />
            } @else {

              <!-- Personas con tareas activas -->
              @if (busyMembers(team).length > 0) {
                <div class="persons-grid">
                  @for (member of busyMembers(team); track member.personId) {
                    <div class="person-card">
                      <!-- Cabecera persona -->
                      <div class="person-card-header">
                        <nz-avatar
                          [nzText]="member.name[0]"
                          [style.background-color]="memberColor(member.name)"
                          style="flex-shrink:0;font-size:13px;font-weight:700">
                        </nz-avatar>
                        <div style="min-width:0">
                          <div class="person-name">{{ member.name }}</div>
                          <div class="person-role">{{ roleLabel(member.role) }}</div>
                        </div>
                        <!-- Contador badge -->
                        <span style="margin-left:auto;background:#fff2e8;color:#d4380d;border-radius:10px;padding:1px 8px;font-size:11px;font-weight:700;white-space:nowrap;flex-shrink:0">
                          {{ member.activeTasks.length }}
                        </span>
                      </div>

                      <!-- Tareas activas -->
                      @for (task of member.activeTasks; track task.workItemId) {
                        <div class="task-item">
                          <!-- Fila superior: status badge + priority dot -->
                          <div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap">
                            <span
                              style="display:inline-flex;align-items:center;gap:4px;border-radius:4px;padding:1px 6px;font-size:11px;font-weight:600"
                              [style.color]="statusCfg(task.status).color"
                              [style.background]="statusCfg(task.status).bg">
                              @if (task.status === 'Blocked') {
                                <span nz-icon nzType="stop" style="font-size:10px"></span>
                              } @else {
                                <span nz-icon nzType="sync" style="font-size:10px"></span>
                              }
                              {{ statusCfg(task.status).label }}
                            </span>
                            <!-- Priority dot -->
                            <span
                              style="width:8px;height:8px;border-radius:50%;flex-shrink:0"
                              [style.background]="priorityColor(task.priority)"
                              nz-tooltip [nzTooltipTitle]="task.priority">
                            </span>
                            @if (task.isHito) {
                              <span nz-icon nzType="flag" style="color:#eb2f96;font-size:12px" nz-tooltip nzTooltipTitle="Hito"></span>
                            }
                            @if (task.type === 'UserStory') {
                              <span nz-icon nzType="read" style="color:#8c8c8c;font-size:11px" nz-tooltip nzTooltipTitle="Historia de usuario"></span>
                            }
                          </div>

                          <!-- Título clicable -->
                          <a class="task-title" [routerLink]="['/projects', task.projectId]"
                            nz-tooltip [nzTooltipTitle]="task.title">
                            {{ task.title }}
                          </a>

                          <!-- Meta: proyecto · sprint -->
                          <div class="task-meta">
                            {{ task.projectTitle }}
                            @if (task.sprintName) { · {{ task.sprintName }} }
                          </div>

                          <!-- Fecha límite -->
                          @if (task.dueDate) {
                            <div class="task-duedate" [class.overdue]="isOverdue(task.dueDate)">
                              📅 {{ task.dueDate }}
                              @if (isOverdue(task.dueDate)) { ⚠ }
                            </div>
                          }
                        </div>
                      }
                    </div>
                  }
                </div>
              }

              <!-- Divider si hay ambos grupos -->
              @if (busyMembers(team).length > 0 && availableMembers(team).length > 0) {
                <nz-divider style="margin:12px 0"></nz-divider>
              }

              <!-- Personas disponibles (compactas) -->
              @if (availableMembers(team).length > 0) {
                <div class="available-section">
                  <div class="available-label">Disponibles</div>
                  <div class="available-chips">
                    @for (member of availableMembers(team); track member.personId) {
                      <div class="available-chip">
                        <nz-avatar
                          [nzText]="member.name[0]"
                          nzSize="small"
                          [style.background-color]="memberColor(member.name)"
                          style="font-size:11px;font-weight:700">
                        </nz-avatar>
                        <span class="available-chip-name">{{ member.name }}</span>
                      </div>
                    }
                  </div>
                </div>
              }

            }
          </nz-card>
        }
      </div>
    }
  `,
})
export class TeamActivityComponent {
  private readonly http = inject(HttpClient);

  readonly data = toSignal(
    this.http.get<TeamActivityDto[]>('/api/teams/activity')
  );

  // ── Helpers ────────────────────────────────────────────────────────────────

  busyMembers(team: TeamActivityDto): PersonActivityDto[] {
    return team.members.filter(m => m.activeTasks.length > 0);
  }

  availableMembers(team: TeamActivityDto): PersonActivityDto[] {
    return team.members.filter(m => m.activeTasks.length === 0);
  }

  activeCount(team: TeamActivityDto): number {
    return team.members.reduce((sum, m) => sum + m.activeTasks.length, 0);
  }

  roleLabel(role: string): string {
    return ROLE_LABELS[role] ?? role;
  }

  memberColor(name: string): string {
    return avatarColor(name);
  }

  statusCfg(status: string): { label: string; color: string; bg: string } {
    return STATUS_CONFIG[status] ?? { label: status, color: '#595959', bg: '#f5f5f5' };
  }

  priorityColor(priority: string): string {
    return PRIORITY_COLORS[priority] ?? '#d9d9d9';
  }

  isOverdue(dueDate: string): boolean {
    return new Date(dueDate) < new Date();
  }
}
