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
import {
  PROJECT_COMPLEXITY_LABELS,
  PROJECT_COMPLEXITY_ORDER,
  PROJECT_STATUS_LABELS,
  PROJECT_STATUS_PILL_COLORS,
  ProjectComplexity,
  ProjectStatus,
} from '../projects/project.model';
import { ComplexityIndicatorComponent } from '../projects/complexity-indicator/complexity-indicator.component';
import { ProjectStatusBadgeComponent } from '../projects/project-status-badge/project-status-badge.component';

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

interface PortfolioProjectDto {
  id: number; title: string; status: ProjectStatus; requestingUnit: string; complexity: ProjectComplexity;
  portfolioYear?: number;
}
interface PortfolioStatsDto {
  total: number;
  stopped: number;
  planningWithClient: number;
  waitingForDevelopers: number;
  planningSprint: number;
  inSprint: number;
  developmentOutsideSprint: number;
  inTesting: number;
  completed: number;
  postponedByClient: number;
}
interface PortfolioDto {
  projects: PortfolioProjectDto[];
  stats: PortfolioStatsDto;
  availableYears: number[];
}

const STATUS_ORDER: ProjectStatus[] = [
  'InSprint', 'DevelopmentOutsideSprint', 'InTesting', 'WaitingForDevelopers',
  'PlanningSprint', 'PlanningWithClient', 'Stopped', 'Completed', 'PostponedByClient',
];

const ROLE_LABELS: Record<string, string> = {
  Gestor:      'Gestor de cartera',
  JefeEquipo:  'Jefe de equipo',
  Desarrollador: 'Desarrollador',
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    NzCardModule, NzAvatarModule, NzProgressModule,
    NzTableModule, NzTagModule, NzButtonModule, NzIconModule, NzSpinModule,
    NzGridModule, NzDividerModule, NzEmptyModule, NzBadgeModule, NzTooltipModule,
    ComplexityIndicatorComponent, ProjectStatusBadgeComponent,
  ],
  styles: [`
    .dashboard-wrapper { max-width: 1280px; margin: 0 auto; padding: 0 0 48px; }

    .eyebrow { font-size: 12px; font-weight: 700; letter-spacing: 1.2px; text-transform: uppercase; color: var(--brand-primary); margin: 0 0 4px; }
    h1.page-title { font-size: 28px; font-weight: 800; letter-spacing: -0.4px; margin: 0 0 24px; color: var(--ink); }

    .kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 18px; margin-bottom: 18px; }
    @media (max-width: 900px) { .kpi-grid { grid-template-columns: repeat(2, 1fr); } }
    .kpi-card { background: var(--bg-surface); border: 1px solid var(--border); border-radius: var(--radius-card); padding: 20px 22px; }
    .kpi-label-row { display: flex; align-items: center; gap: 8px; margin-bottom: 10px; }
    .kpi-dot { width: 8px; height: 8px; border-radius: 999px; flex: 0 0 auto; }
    .kpi-label { font-size: 12px; font-weight: 700; letter-spacing: 0.5px; text-transform: uppercase; color: var(--text-muted); }
    .kpi-value { font-size: 38px; font-weight: 800; line-height: 1; font-variant-numeric: tabular-nums; color: var(--ink); }
    .kpi-sub { font-size: 12.5px; color: var(--text-muted); margin-top: 6px; }

    .dist-grid { display: grid; grid-template-columns: 1.3fr 1fr; gap: 18px; margin-bottom: 18px; }
    @media (max-width: 900px) { .dist-grid { grid-template-columns: 1fr; } }

    .section-title { font-size: 15px; font-weight: 700; margin: 0 0 16px; display: flex; align-items: center; gap: 8px; color: var(--ink); }

    .dist-row { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; }
    .dist-label { display: flex; align-items: center; gap: 6px; font-size: 13px; color: var(--text-secondary); flex-shrink: 0; }
    .dist-label-status { width: 172px; }
    .dist-label-complexity { width: 96px; }
    .dist-bar-track { flex: 1; height: 8px; border-radius: 999px; background: var(--bg-track); overflow: hidden; }
    .dist-bar-fill { height: 100%; border-radius: 999px; }
    .dist-count { width: 28px; text-align: right; font-size: 13px; font-variant-numeric: tabular-nums; color: var(--ink); flex-shrink: 0; }

    .attention-row { display: flex; align-items: center; justify-content: space-between; padding: 12px 0; border-top: 1px solid var(--border-faint-alt); }
    .attention-row:first-of-type { border-top: none; }

    .charts-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; margin-bottom: 18px; }
    @media (max-width: 800px) { .charts-grid { grid-template-columns: 1fr; } }

    .sprint-progress-row { display: flex; align-items: center; gap: 12px; margin-bottom: 12px; }
    .sprint-progress-label { width: 140px; flex-shrink: 0; font-size: 13px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .sprint-progress-bar { flex: 1; }
    .sprint-progress-count { font-size: 12px; color: var(--text-muted); width: 60px; text-align: right; flex-shrink: 0; }

    .status-row { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; }
    .status-label { width: 90px; font-size: 13px; flex-shrink: 0; }
    .status-bar   { flex: 1; }
    .status-count { width: 32px; text-align: right; font-size: 13px; font-weight: 600; flex-shrink: 0; }
  `],
  template: `
    @if (!data() || !portfolio()) {
      <div style="display:flex;justify-content:center;padding:80px">
        <nz-spin nzSize="large" />
      </div>
    } @else {
      <div class="dashboard-wrapper">

        <p class="eyebrow">Cartera de Proyectos TIC</p>
        <h1 class="page-title">Dashboard</h1>

        <!-- KPIs de cartera -->
        <div class="kpi-grid">
          <div class="kpi-card">
            <div class="kpi-label-row"><span class="kpi-dot" style="background:#1B1A18"></span><span class="kpi-label">Total proyectos</span></div>
            <div class="kpi-value">{{ portfolio()!.stats.total }}</div>
            <div class="kpi-sub">en la cartera</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-label-row"><span class="kpi-dot" style="background:#2A5BA8"></span><span class="kpi-label">En curso</span></div>
            <div class="kpi-value">{{ inCourseCount() }}</div>
            <div class="kpi-sub">sprint, pruebas o desarrollo</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-label-row"><span class="kpi-dot" style="background:var(--brand-primary)"></span><span class="kpi-label">Parados</span></div>
            <div class="kpi-value" style="color:var(--brand-primary)">{{ portfolio()!.stats.stopped }}</div>
            <div class="kpi-sub">requieren atención</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-label-row"><span class="kpi-dot" style="background:#1F9D5B"></span><span class="kpi-label">Finalizados</span></div>
            <div class="kpi-value">{{ portfolio()!.stats.completed }}</div>
            <div class="kpi-sub">completados</div>
          </div>
        </div>

        <!-- Distribuciones -->
        <div class="dist-grid">
          <nz-card [nzBordered]="true">
            <div class="section-title">Proyectos por estado</div>
            @for (row of statusDistribution(); track row.status) {
              <div class="dist-row">
                <span class="dist-label dist-label-status">
                  <span class="kpi-dot" [style.background]="row.dot"></span>{{ row.label }}
                </span>
                <span class="dist-bar-track">
                  <span class="dist-bar-fill" [style.width.%]="row.pct" [style.background]="row.dot"></span>
                </span>
                <span class="dist-count">{{ row.count }}</span>
              </div>
            }
          </nz-card>

          <nz-card [nzBordered]="true">
            <div class="section-title">Por complejidad</div>
            @for (row of complexityDistribution(); track row.complexity) {
              <div class="dist-row">
                <span class="dist-label dist-label-complexity">{{ row.label }}</span>
                <span class="dist-bar-track">
                  <span class="dist-bar-fill" [style.width.%]="row.pct" style="background:var(--complexity-filled)"></span>
                </span>
                <span class="dist-count">{{ row.count }}</span>
              </div>
            }
          </nz-card>
        </div>

        <!-- Proyectos parados -->
        <nz-card [nzBordered]="true" style="margin-bottom:24px">
          <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px">
            <div class="section-title" style="margin-bottom:0">
              <span class="kpi-dot" style="background:var(--brand-primary)"></span> Proyectos parados — requieren atención
            </div>
            <a routerLink="/portfolio" style="font-size:13px;color:var(--brand-primary);font-weight:600">Ver toda la cartera →</a>
          </div>
          @if (stoppedProjects().length === 0) {
            <nz-empty nzNotFoundContent="Ningún proyecto parado" />
          } @else {
            @for (p of stoppedProjects(); track p.id) {
              <div class="attention-row">
                <div>
                  <a [routerLink]="['/projects', p.id]" style="font-weight:600;font-size:14.5px">{{ p.title }}</a>
                  <div style="font-size:12.5px;color:var(--text-muted)">{{ p.requestingUnit }}</div>
                </div>
                <app-complexity-indicator [complexity]="p.complexity" />
              </div>
            }
          }
        </nz-card>

        <!-- Resumen personal -->
        <div class="charts-grid">

          <nz-card [nzBordered]="true">
            <div class="section-title"><span nz-icon nzType="pie-chart"></span> Mis tareas por estado</div>

            @if (data()!.myWorkItems.total === 0) {
              <nz-empty nzNotFoundContent="Sin tareas asignadas" />
            } @else {
              <div style="display:flex;align-items:center;gap:24px;margin-bottom:20px">
                <nz-progress
                  nzType="circle"
                  [nzPercent]="donePercent()"
                  [nzWidth]="100"
                  nzStrokeColor="#1F9D5B"
                  [nzFormat]="circleFmt">
                </nz-progress>
                <div>
                  <p style="margin:0 0 4px;font-size:13px;color:var(--text-muted)">Tareas completadas</p>
                  <p style="margin:0;font-size:20px;font-weight:700;color:#1F9D5B">
                    {{ data()!.myWorkItems.done }} / {{ data()!.myWorkItems.total }}
                  </p>
                </div>
              </div>

              <div class="status-row">
                <span class="status-label" style="color:var(--text-muted)">Backlog</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.backlog)" nzStrokeColor="#d9d9d9" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.backlog }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#2A5BA8">Por hacer</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.toDo)" nzStrokeColor="#2A5BA8" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.toDo }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#C9A11A">En progreso</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.inProgress)" nzStrokeColor="#C9A11A" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.inProgress }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:var(--brand-primary)">Bloqueadas</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.blocked)" [nzStrokeColor]="'var(--brand-primary)'" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.blocked }}</span>
              </div>
              <div class="status-row">
                <span class="status-label" style="color:#1F9D5B">Hecho</span>
                <nz-progress class="status-bar" [nzPercent]="pct(data()!.myWorkItems.done)" nzStrokeColor="#1F9D5B" [nzShowInfo]="false" nzSize="small"></nz-progress>
                <span class="status-count">{{ data()!.myWorkItems.done }}</span>
              </div>
            }
          </nz-card>

          <nz-card [nzBordered]="true">
            <div class="section-title"><span nz-icon nzType="thunderbolt"></span> Progreso de sprints activos</div>

            @if (data()!.activeSprints.length === 0) {
              <nz-empty nzNotFoundContent="Sin sprints activos" />
            } @else {
              @for (sprint of data()!.activeSprints; track sprint.id) {
                <div class="sprint-progress-row">
                  <span class="sprint-progress-label" [nzTooltipTitle]="sprint.projectTitle + ' — ' + sprint.name" nz-tooltip>
                    <span style="font-weight:600">{{ sprint.name }}</span>
                    <span style="display:block;font-size:11px;color:var(--text-muted)">{{ sprint.projectTitle }}</span>
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
                    <span style="font-size:11px;color:var(--text-muted)">
                      Fin: {{ sprint.endDate }}
                      @if (daysLeft(sprint.endDate) !== null) {
                        · <span [style.color]="daysLeft(sprint.endDate)! <= 3 ? 'var(--brand-primary)' : 'var(--text-muted)'">
                          {{ daysLeft(sprint.endDate) }}d restantes
                        </span>
                      }
                    </span>
                  </div>
                }
              }

              @if (data()!.myWorkItems.total > 0) {
                <nz-divider style="margin:16px 0 12px"></nz-divider>
                <div style="font-size:13px;color:var(--text-muted);margin-bottom:8px">Distribución por prioridad</div>
                <div style="display:flex;gap:8px;flex-wrap:wrap">
                  <nz-tag nzColor="error">🔴 Crítica: {{ data()!.myWorkItems.critical }}</nz-tag>
                  <nz-tag nzColor="warning">🟠 Alta: {{ data()!.myWorkItems.high }}</nz-tag>
                  <nz-tag nzColor="processing">🔵 Media: {{ data()!.myWorkItems.medium }}</nz-tag>
                  <nz-tag>⚪ Baja: {{ data()!.myWorkItems.low }}</nz-tag>
                </div>
              }
            }
          </nz-card>

        </div>

        <!-- Mis proyectos -->
        <nz-card [nzBordered]="true" style="margin-bottom:16px">
          <div class="section-title"><span nz-icon nzType="project"></span> Mis proyectos</div>
          <nz-table [nzData]="data()!.myProjects" nzBordered nzSize="small" [nzShowPagination]="false">
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
                  <td><a [routerLink]="['/projects', p.id]" style="font-weight:600">{{ p.title }}</a></td>
                  <td style="color:var(--text-secondary);font-size:13px">{{ p.requestingUnit }}</td>
                  <td>
                    <app-project-status-badge [status]="p.status" />
                  </td>
                  <td>
                    @if (p.totalWorkItems > 0) {
                      <div style="display:flex;align-items:center;gap:8px">
                        <nz-progress style="flex:1" [nzPercent]="projectPct(p)" nzSize="small" [nzShowInfo]="false" nzStrokeColor="#1F9D5B"></nz-progress>
                        <span style="font-size:12px;color:var(--text-muted);white-space:nowrap">{{ p.doneWorkItems }}/{{ p.totalWorkItems }}</span>
                      </div>
                    } @else {
                      <span style="color:var(--text-faint);font-size:13px">Sin tareas</span>
                    }
                  </td>
                  <td>
                    <a nz-button nzSize="small" [routerLink]="['/projects', p.id, 'kanban']">
                      <span nz-icon nzType="project"></span> Kanban
                    </a>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="5" style="text-align:center;color:var(--text-faint)">No tienes proyectos asignados</td></tr>
              }
            </tbody>
          </nz-table>
        </nz-card>

        <!-- Sprints activos -->
        <nz-card [nzBordered]="true">
          <div class="section-title">
            <span nz-icon nzType="thunderbolt" nzTheme="fill" style="color:var(--brand-primary)"></span>
            Sprints activos
          </div>
          <nz-table [nzData]="data()!.activeSprints" nzBordered nzSize="small" [nzShowPagination]="false">
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
                  <td><a [routerLink]="['/projects', s.projectId]" style="font-size:13px">{{ s.projectTitle }}</a></td>
                  <td style="font-size:13px;color:var(--text-secondary);max-width:200px">{{ s.goal ?? '—' }}</td>
                  <td style="font-size:12px;white-space:nowrap">
                    {{ s.startDate ?? '?' }} → {{ s.endDate ?? '?' }}
                    @if (daysLeft(s.endDate) !== null) {
                      <span [style.color]="daysLeft(s.endDate)! <= 3 ? 'var(--brand-primary)' : 'var(--text-muted)'" style="display:block">
                        {{ daysLeft(s.endDate) }}d restantes
                      </span>
                    }
                  </td>
                  <td>
                    <div style="display:flex;align-items:center;gap:8px">
                      <nz-progress style="flex:1;min-width:80px" [nzPercent]="sprintPct(s)" nzSize="small" [nzShowInfo]="false" [nzStrokeColor]="sprintColor(s)"></nz-progress>
                      <span style="font-size:12px;color:var(--text-muted);white-space:nowrap">{{ s.doneWorkItems }}/{{ s.workItemCount }}</span>
                    </div>
                  </td>
                  <td>
                    <a nz-button nzSize="small" nzType="primary" [routerLink]="['/projects', s.projectId, 'sprints', s.id, 'kanban']">
                      <span nz-icon nzType="layout"></span> Abrir
                    </a>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6" style="text-align:center;color:var(--text-faint)">Sin sprints activos en tus proyectos</td></tr>
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
  readonly portfolio = toSignal(this.http.get<PortfolioDto>('/api/portfolio'));

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

  readonly inCourseCount = computed(() => {
    const s = this.portfolio()?.stats;
    if (!s) return 0;
    return s.inSprint + s.developmentOutsideSprint + s.inTesting + s.waitingForDevelopers;
  });

  private statKey(status: ProjectStatus): keyof PortfolioStatsDto {
    const map: Record<ProjectStatus, keyof PortfolioStatsDto> = {
      Stopped: 'stopped',
      PlanningWithClient: 'planningWithClient',
      WaitingForDevelopers: 'waitingForDevelopers',
      PlanningSprint: 'planningSprint',
      InSprint: 'inSprint',
      DevelopmentOutsideSprint: 'developmentOutsideSprint',
      InTesting: 'inTesting',
      Completed: 'completed',
      PostponedByClient: 'postponedByClient',
    };
    return map[status];
  }

  readonly statusDistribution = computed(() => {
    const stats = this.portfolio()?.stats;
    if (!stats) return [];
    const counts = STATUS_ORDER.map(status => ({ status, count: stats[this.statKey(status)] }));
    const max = Math.max(...counts.map(c => c.count), 1);
    return counts.map(c => ({
      status: c.status,
      label: PROJECT_STATUS_LABELS[c.status],
      dot: PROJECT_STATUS_PILL_COLORS[c.status].dot,
      count: c.count,
      pct: Math.round((c.count / max) * 100),
    }));
  });

  readonly complexityDistribution = computed(() => {
    const projects = this.portfolio()?.projects ?? [];
    const order: ProjectComplexity[] = ['VerySmall', 'Small', 'Medium', 'Large', 'VeryLarge'];
    const counts = order.map(complexity => ({
      complexity,
      count: projects.filter(p => p.complexity === complexity).length,
    }));
    const max = Math.max(...counts.map(c => c.count), 1);
    return counts.map(c => ({
      complexity: c.complexity,
      label: PROJECT_COMPLEXITY_LABELS[c.complexity],
      count: c.count,
      pct: Math.round((c.count / max) * 100),
    }));
  });

  readonly stoppedProjects = computed(() =>
    (this.portfolio()?.projects ?? []).filter(p => p.status === 'Stopped'),
  );

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
    if (pct >= 80) return '#1F9D5B';
    if (pct >= 40) return '#C9A11A';
    return '#2A5BA8';
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

  logout(): void {
    this.oidc.logoff().subscribe();
  }
}
