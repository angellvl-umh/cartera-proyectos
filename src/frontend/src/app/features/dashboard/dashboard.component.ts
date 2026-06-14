import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';

interface DashboardMeDto {
  id: number; name: string; email: string; role: string;
}

interface DashboardProjectDto {
  id: number; title: string; status: string; requestingUnit: string;
  startDate?: string; endDate?: string;
  totalWorkItems: number; doneWorkItems: number;
}

interface DashboardSprintDto {
  id: number; projectId: number; projectTitle: string;
  name: string; goal?: string; startDate?: string; endDate?: string;
  workItemCount: number; doneWorkItems: number; totalEstimationPoints: number;
}

interface WorkItemStatsDto {
  total: number; backlog: number; toDo: number; inProgress: number;
  blocked: number; done: number;
  critical: number; high: number; medium: number; low: number;
}

interface DashboardDto {
  me: DashboardMeDto;
  myProjects: DashboardProjectDto[];
  activeSprints: DashboardSprintDto[];
  myWorkItems: WorkItemStatsDto;
}

const PROJECT_STATUS_COLORS: Record<string, string> = {
  Proposed:   'default',
  Approved:   'processing',
  InProgress: 'success',
  Paused:     'warning',
  Completed:  'success',
  Cancelled:  'error',
};

const PROJECT_STATUS_LABELS: Record<string, string> = {
  Proposed:   'Propuesto',
  Approved:   'Aprobado',
  InProgress: 'En ejecución',
  Paused:     'Pausado',
  Completed:  'Completado',
  Cancelled:  'Cancelado',
};

const ROLE_LABELS: Record<string, string> = {
  Gestor:      'Gestor de cartera',
  JefeEquipo:  'Jefe de equipo',
  Desarrollador: 'Desarrollador',
};

const ROLE_COLORS: Record<string, string> = {
  Gestor:       'purple',
  JefeEquipo:   'blue',
  Desarrollador: 'green',
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    NzCardModule, NzAvatarModule, NzStatisticModule, NzProgressModule,
    NzTableModule, NzTagModule, NzButtonModule, NzIconModule, NzSpinModule,
    NzGridModule, NzDividerModule, NzEmptyModule, NzBadgeModule, NzTooltipModule,
  ],
  styles: [`
    .dashboard-wrapper { max-width: 1200px; margin: 0 auto; padding: 0 0 48px; }

    .user-card {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 24px;
      background: linear-gradient(135deg, #1890ff 0%, #096dd9 100%);
      border-radius: 8px;
      margin-bottom: 24px;
      color: #fff;
    }
    .user-info h2 { margin: 0 0 4px; color: #fff; font-size: 22px; }
    .user-info p  { margin: 0; color: rgba(255,255,255,0.85); font-size: 14px; }

    .stat-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      margin-bottom: 24px;
    }
    @media (max-width: 800px) { .stat-grid { grid-template-columns: repeat(2, 1fr); } }

    .stat-card {
      background: #fff;
      border-radius: 8px;
      padding: 20px 24px;
      box-shadow: 0 1px 4px rgba(0,0,0,.07);
      border: 1px solid #f0f0f0;
    }
    .stat-label { font-size: 13px; color: #8c8c8c; margin-bottom: 8px; }
    .stat-value { font-size: 28px; font-weight: 700; line-height: 1; }
    .stat-sub   { font-size: 12px; color: #8c8c8c; margin-top: 4px; }

    .charts-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      margin-bottom: 24px;
    }
    @media (max-width: 800px) { .charts-grid { grid-template-columns: 1fr; } }

    .section-title {
      font-size: 16px;
      font-weight: 600;
      margin: 0 0 12px;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .sprint-progress-row {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 12px;
    }
    .sprint-progress-label {
      width: 140px;
      flex-shrink: 0;
      font-size: 13px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .sprint-progress-bar { flex: 1; }
    .sprint-progress-count { font-size: 12px; color: #8c8c8c; width: 60px; text-align: right; flex-shrink: 0; }

    .status-row {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 10px;
    }
    .status-label { width: 90px; font-size: 13px; flex-shrink: 0; }
    .status-bar   { flex: 1; }
    .status-count { width: 32px; text-align: right; font-size: 13px; font-weight: 600; flex-shrink: 0; }
  `],
  template: `
    @if (!data()) {
      <div style="display:flex;justify-content:center;padding:80px">
        <nz-spin nzSize="large" />
      </div>
    } @else {
      <div class="dashboard-wrapper">

        <!-- Usuario -->
        <div class="user-card">
          <nz-avatar [nzSize]="64" [nzText]="initials()" style="background:#fff;color:#1890ff;font-size:24px;font-weight:700;flex-shrink:0"></nz-avatar>
          <div class="user-info" style="flex:1">
            <h2>{{ data()!.me.name }}</h2>
            <p>{{ data()!.me.email }}</p>
            <nz-tag [nzColor]="roleColor(data()!.me.role)" style="margin-top:8px">
              {{ roleLabel(data()!.me.role) }}
            </nz-tag>
          </div>
          <button nz-button nzGhost (click)="logout()" style="color:#fff;border-color:rgba(255,255,255,.5)">
            <span nz-icon nzType="logout"></span> Cerrar sesión
          </button>
        </div>

        <!-- Stat cards -->
        <div class="stat-grid">
          <div class="stat-card">
            <div class="stat-label">Mis tareas</div>
            <div class="stat-value" style="color:#1890ff">{{ data()!.myWorkItems.total }}</div>
            <div class="stat-sub">en mis proyectos</div>
          </div>
          <div class="stat-card">
            <div class="stat-label">En progreso</div>
            <div class="stat-value" style="color:#faad14">{{ data()!.myWorkItems.inProgress }}</div>
            <div class="stat-sub">tareas activas ahora</div>
          </div>
          <div class="stat-card">
            <div class="stat-label">Completadas</div>
            <div class="stat-value" style="color:#52c41a">{{ data()!.myWorkItems.done }}</div>
            <div class="stat-sub">
              {{ donePercent() }}% del total
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-label">Sprints activos</div>
            <div class="stat-value" style="color:#722ed1">{{ data()!.activeSprints.length }}</div>
            <div class="stat-sub">en mis proyectos</div>
          </div>
        </div>

        <!-- Charts -->
        <div class="charts-grid">

          <!-- Gráfico donut: % completado + distribución -->
          <nz-card [nzBordered]="true">
            <div class="section-title">
              <span nz-icon nzType="pie-chart"></span> Mis tareas por estado
            </div>

            @if (data()!.myWorkItems.total === 0) {
              <nz-empty nzNotFoundContent="Sin tareas asignadas" />
            } @else {
              <div style="display:flex;align-items:center;gap:24px;margin-bottom:20px">
                <nz-progress
                  nzType="circle"
                  [nzPercent]="donePercent()"
                  [nzWidth]="100"
                  nzStrokeColor="#52c41a"
                  [nzFormat]="circleFmt">
                </nz-progress>
                <div>
                  <p style="margin:0 0 4px;font-size:13px;color:#8c8c8c">Tareas completadas</p>
                  <p style="margin:0;font-size:20px;font-weight:700;color:#52c41a">
                    {{ data()!.myWorkItems.done }} / {{ data()!.myWorkItems.total }}
                  </p>
                </div>
              </div>

              <div class="status-row">
                <span class="status-label" style="color:#8c8c8c">Backlog</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.backlog)" nzStrokeColor="#d9d9d9" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.backlog }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#1890ff">Por hacer</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.toDo)" nzStrokeColor="#1890ff" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.toDo }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#faad14">En progreso</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.inProgress)" nzStrokeColor="#faad14" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.inProgress }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#ff4d4f">Bloqueadas</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.blocked)" nzStrokeColor="#ff4d4f" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.blocked }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#52c41a">Hecho</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.done)" nzStrokeColor="#52c41a" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.done }}</span>
              </div>
            }
          </nz-card>

          <!-- Gráfico sprints: progreso de cada sprint activo -->
          <nz-card [nzBordered]="true">
            <div class="section-title">
              <span nz-icon nzType="thunderbolt"></span> Progreso de sprints activos
            </div>

            @if (data()!.activeSprints.length === 0) {
              <nz-empty nzNotFoundContent="Sin sprints activos" />
            } @else {
              @for (sprint of data()!.activeSprints; track sprint.id) {
                <div class="sprint-progress-row">
                  <span class="sprint-progress-label" [nzTooltipTitle]="sprint.projectTitle + ' — ' + sprint.name" nz-tooltip>
                    <span style="font-weight:600">{{ sprint.name }}</span>
                    <span style="display:block;font-size:11px;color:#8c8c8c">{{ sprint.projectTitle }}</span>
                  </span>
                  <nz-progress
                    class="sprint-progress-bar"
                    [nzPercent]="sprintPct(sprint)"
                    [nzStrokeColor]="sprintColor(sprint)"
                    nzSize="small"
                    [nzShowInfo]="false">
                  </nz-progress>
                  <span class="sprint-progress-count">{{ sprint.doneWorkItems }}/{{ sprint.workItemCount }}</span>
                </div>
                @if (sprint.endDate) {
                  <div style="margin-top:-6px;margin-bottom:12px;padding-left:152px">
                    <span style="font-size:11px;color:#8c8c8c">
                      Fin: {{ sprint.endDate }}
                      @if (daysLeft(sprint.endDate) !== null) {
                        · <span [style.color]="daysLeft(sprint.endDate)! <= 3 ? '#ff4d4f' : '#8c8c8c'">
                          {{ daysLeft(sprint.endDate) }}d restantes
                        </span>
                      }
                    </span>
                  </div>
                }
              }

              @if (data()!.myWorkItems.total > 0) {
                <nz-divider style="margin:16px 0 12px"></nz-divider>
                <div style="font-size:13px;color:#8c8c8c;margin-bottom:8px">Distribución por prioridad</div>
                <div style="display:flex;gap:8px;flex-wrap:wrap">
                  <nz-tag nzColor="error">
                    🔴 Crítica: {{ data()!.myWorkItems.critical }}
                  </nz-tag>
                  <nz-tag nzColor="warning">
                    🟠 Alta: {{ data()!.myWorkItems.high }}
                  </nz-tag>
                  <nz-tag nzColor="processing">
                    🔵 Media: {{ data()!.myWorkItems.medium }}
                  </nz-tag>
                  <nz-tag>
                    ⚪ Baja: {{ data()!.myWorkItems.low }}
                  </nz-tag>
                </div>
              }
            }
          </nz-card>

        </div>

        <!-- Mis proyectos -->
        <nz-card [nzBordered]="true" style="margin-bottom:16px">
          <div class="section-title">
            <span nz-icon nzType="project"></span> Mis proyectos
          </div>
          <nz-table
            [nzData]="data()!.myProjects"
            nzBordered
            nzSize="small"
            [nzShowPagination]="false">
            <thead>
              <tr>
                <th>Proyecto</th>
                <th>Unidad</th>
                <th>Estado</th>
                <th>Progreso tareas</th>
                <th style="width:120px">Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (p of data()!.myProjects; track p.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/projects', p.id]" style="font-weight:600">{{ p.title }}</a>
                  </td>
                  <td style="color:#595959;font-size:13px">{{ p.requestingUnit }}</td>
                  <td>
                    <nz-tag [nzColor]="projectStatusColor(p.status)">
                      {{ projectStatusLabel(p.status) }}
                    </nz-tag>
                  </td>
                  <td>
                    @if (p.totalWorkItems > 0) {
                      <div style="display:flex;align-items:center;gap:8px">
                        <nz-progress
                          style="flex:1"
                          [nzPercent]="projectPct(p)"
                          nzSize="small"
                          [nzShowInfo]="false"
                          nzStrokeColor="#52c41a">
                        </nz-progress>
                        <span style="font-size:12px;color:#8c8c8c;white-space:nowrap">
                          {{ p.doneWorkItems }}/{{ p.totalWorkItems }}
                        </span>
                      </div>
                    } @else {
                      <span style="color:#bfbfbf;font-size:13px">Sin tareas</span>
                    }
                  </td>
                  <td>
                    <a nz-button nzSize="small" [routerLink]="['/projects', p.id, 'kanban']">
                      <span nz-icon nzType="project"></span> Kanban
                    </a>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="5" style="text-align:center;color:#bfbfbf">No tienes proyectos asignados</td></tr>
              }
            </tbody>
          </nz-table>
        </nz-card>

        <!-- Sprints activos -->
        <nz-card [nzBordered]="true">
          <div class="section-title">
            <span nz-icon nzType="thunderbolt" nzTheme="fill" style="color:#722ed1"></span>
            Sprints activos
          </div>
          <nz-table
            [nzData]="data()!.activeSprints"
            nzBordered
            nzSize="small"
            [nzShowPagination]="false">
            <thead>
              <tr>
                <th>Sprint</th>
                <th>Proyecto</th>
                <th>Objetivo</th>
                <th>Fechas</th>
                <th>Progreso</th>
                <th style="width:110px">Kanban</th>
              </tr>
            </thead>
            <tbody>
              @for (s of data()!.activeSprints; track s.id) {
                <tr>
                  <td style="font-weight:600">{{ s.name }}</td>
                  <td>
                    <a [routerLink]="['/projects', s.projectId]" style="font-size:13px">{{ s.projectTitle }}</a>
                  </td>
                  <td style="font-size:13px;color:#595959;max-width:200px">
                    {{ s.goal ?? '—' }}
                  </td>
                  <td style="font-size:12px;white-space:nowrap">
                    {{ s.startDate ?? '?' }} → {{ s.endDate ?? '?' }}
                    @if (daysLeft(s.endDate) !== null) {
                      <span [style.color]="daysLeft(s.endDate)! <= 3 ? '#ff4d4f' : '#8c8c8c'" style="display:block">
                        {{ daysLeft(s.endDate) }}d restantes
                      </span>
                    }
                  </td>
                  <td>
                    <div style="display:flex;align-items:center;gap:8px">
                      <nz-progress
                        style="flex:1;min-width:80px"
                        [nzPercent]="sprintPct(s)"
                        nzSize="small"
                        [nzShowInfo]="false"
                        [nzStrokeColor]="sprintColor(s)">
                      </nz-progress>
                      <span style="font-size:12px;color:#8c8c8c;white-space:nowrap">
                        {{ s.doneWorkItems }}/{{ s.workItemCount }}
                      </span>
                    </div>
                  </td>
                  <td>
                    <a nz-button nzSize="small" nzType="primary"
                      [routerLink]="['/projects', s.projectId, 'sprints', s.id, 'kanban']">
                      <span nz-icon nzType="layout"></span> Abrir
                    </a>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6" style="text-align:center;color:#bfbfbf">Sin sprints activos en tus proyectos</td></tr>
              }
            </tbody>
          </nz-table>
        </nz-card>

      </div>
    }
  `,
})
export class DashboardComponent {
  private readonly http = inject(HttpClient);
  private readonly oidc = inject(OidcSecurityService);

  readonly data = toSignal(this.http.get<DashboardDto>('/api/dashboard'));

  readonly initials = computed(() => {
    const name = this.data()?.me.name ?? '';
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase() || '?';
  });

  readonly donePercent = computed(() => {
    const s = this.data()?.myWorkItems;
    if (!s || s.total === 0) return 0;
    return Math.round((s.done / s.total) * 100);
  });

  readonly circleFmt = (p: number) => `${p}%`;

  pct(count: number): number {
    const total = this.data()?.myWorkItems.total ?? 0;
    if (total === 0) return 0;
    return Math.round((count / total) * 100);
  }

  sprintPct(sprint: DashboardSprintDto): number {
    if (sprint.workItemCount === 0) return 0;
    return Math.round((sprint.doneWorkItems / sprint.workItemCount) * 100);
  }

  sprintColor(sprint: DashboardSprintDto): string {
    const pct = this.sprintPct(sprint);
    if (pct >= 80) return '#52c41a';
    if (pct >= 40) return '#faad14';
    return '#1890ff';
  }

  projectPct(p: DashboardProjectDto): number {
    if (p.totalWorkItems === 0) return 0;
    return Math.round((p.doneWorkItems / p.totalWorkItems) * 100);
  }

  daysLeft(endDate?: string): number | null {
    if (!endDate) return null;
    const diff = new Date(endDate).getTime() - Date.now();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  }

  roleLabel(role: string): string {
    return ROLE_LABELS[role] ?? role;
  }

  roleColor(role: string): string {
    return ROLE_COLORS[role] ?? 'default';
  }

  projectStatusLabel(status: string): string {
    return PROJECT_STATUS_LABELS[status] ?? status;
  }

  projectStatusColor(status: string): string {
    return PROJECT_STATUS_COLORS[status] ?? 'default';
  }

  logout(): void {
    this.oidc.logoff().subscribe();
  }
}
