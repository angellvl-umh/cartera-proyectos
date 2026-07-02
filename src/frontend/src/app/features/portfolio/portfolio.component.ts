import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, Subject, startWith } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { ProjectStatusBadgeComponent } from '../projects/project-status-badge/project-status-badge.component';
import { ComplexityIndicatorComponent } from '../projects/complexity-indicator/complexity-indicator.component';
import { ProjectComplexity, ProjectStatus, PROJECT_COMPLEXITY_ORDER } from '../projects/project.model';

interface PortfolioProjectDto {
  id: number; title: string; status: string; requestingUnit: string; complexity: string;
  portfolioYear?: number; startDate?: string; endDate?: string;
  primaryTeamName?: string;
  totalWorkItems: number; doneWorkItems: number;
  totalMilestones: number; reachedMilestones: number;
  activeSprintCount: number;
  businessValue?: number | null;
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

const STATUS_LABELS: Record<string, string> = {
  Stopped: 'Parado', PlanningWithClient: 'Planif. cliente', WaitingForDevelopers: 'Esperando dev.', PlanningSprint: 'Planif. sprint',
  InSprint: 'En sprint', DevelopmentOutsideSprint: 'Desarro. fuera sprint',
  InTesting: 'En pruebas', Completed: 'Finalizado', PostponedByClient: 'Pospuesto cliente',
};

// Business value labels
const BV_LABEL: Record<number, string> = { 1: 'Marginal', 2: 'Bajo', 3: 'Moderado', 4: 'Alto', 5: 'Crítico' };

@Component({
  selector: 'app-portfolio',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule,
    NzCardModule, NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzSpinModule, NzSelectModule, NzProgressModule, NzStatisticModule,
    NzEmptyModule, NzTooltipModule, NzBadgeModule,
    ProjectStatusBadgeComponent, ComplexityIndicatorComponent,
  ],
  styles: [`
    .header { margin-bottom: 24px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px; }
    .header-left h2 { margin: 0 0 4px; font-size: 22px; }
    .header-left p  { margin: 0; color: #8c8c8c; font-size: 13px; }
    .stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 24px; }
    @media (max-width: 900px) { .stats-grid { grid-template-columns: repeat(4, 1fr); } }
    @media (max-width: 500px) { .stats-grid { grid-template-columns: repeat(2, 1fr); } }
    .stat-box { background: #fff; border-radius: 8px; padding: 14px 16px; text-align: center;
                border: 1px solid #f0f0f0; box-shadow: 0 1px 4px rgba(0,0,0,.05); cursor: pointer;
                transition: all .15s; }
    .stat-box:hover { border-color: #1890ff; }
    .stat-box.active { border-color: #1890ff; background: #e6f7ff; }
    .stat-label { font-size: 11px; color: #8c8c8c; margin-bottom: 4px; text-transform: uppercase; letter-spacing: .4px; }
    .stat-value { font-size: 24px; font-weight: 700; line-height: 1; }
    .filters { display: flex; gap: 12px; margin-bottom: 16px; align-items: center; flex-wrap: wrap; }
    .progress-cell { display: flex; align-items: center; gap: 8px; }
    .bv-stars { display: inline-flex; gap: 1px; font-size: 11px; color: #faad14; }
    /* Matriz valor/esfuerzo */
    .matrix-wrap { overflow-x: auto; padding-bottom: 8px; }
    .matrix-grid {
      display: grid;
      grid-template-columns: 40px repeat(5, 1fr);
      grid-template-rows: repeat(5, 1fr) 40px;
      gap: 4px;
      min-width: 560px;
      min-height: 420px;
    }
    .matrix-cell {
      background: #fafafa;
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      padding: 6px;
      min-height: 72px;
      position: relative;
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
      align-content: flex-start;
    }
    .matrix-cell.quadrant-qw  { background: #f6ffed; border-color: #b7eb8f; }
    .matrix-cell.quadrant-str { background: #e6f7ff; border-color: #91d5ff; }
    .matrix-cell.quadrant-fil { background: #fff7e6; border-color: #ffd591; }
    .matrix-cell.quadrant-que { background: #fff1f0; border-color: #ffa39e; }
    .matrix-y-label {
      display: flex; align-items: center; justify-content: center;
      font-size: 11px; font-weight: 700; color: #8c8c8c;
      writing-mode: vertical-lr; transform: rotate(180deg);
      text-transform: uppercase; letter-spacing: .4px;
    }
    .matrix-x-label {
      display: flex; align-items: center; justify-content: center;
      font-size: 11px; font-weight: 700; color: #8c8c8c;
      text-transform: uppercase; letter-spacing: .4px;
    }
    .matrix-corner-label {
      position: absolute; font-size: 9px; font-weight: 700;
      padding: 2px 5px; border-radius: 4px; opacity: .85; white-space: nowrap;
      line-height: 1.2;
    }
    .matrix-corner-label.tl { top: 4px; left: 4px; }
    .matrix-corner-label.tr { top: 4px; right: 4px; }
    .matrix-corner-label.bl { bottom: 4px; left: 4px; }
    .matrix-corner-label.br { bottom: 4px; right: 4px; }
    .project-chip {
      display: inline-block; max-width: 100%; overflow: hidden; text-overflow: ellipsis;
      white-space: nowrap; font-size: 11px; font-weight: 600;
      background: #fff; border: 1px solid #d9d9d9; border-radius: 4px;
      padding: 2px 6px; cursor: pointer; transition: border-color .15s;
      text-decoration: none; color: #262626;
    }
    .project-chip:hover { border-color: #1890ff; color: #1890ff; }
    .view-toggle { display: flex; gap: 3px; background: #EAE6DF; border-radius: 9px; padding: 3px; }
    .view-toggle button {
      border: none; background: transparent; border-radius: 7px; padding: 6px 14px;
      font-size: 13px; font-weight: 600; color: #6B6661; cursor: pointer; white-space: nowrap;
    }
    .view-toggle button.active { background: #262626; color: #fff; }
  `],
  template: `
    <div class="header">
      <div class="header-left">
        <h2><span nz-icon nzType="fund" style="color:#1890ff;margin-right:8px"></span>Cartera de proyectos</h2>
        <p>Vista global de todos los proyectos de la cartera TIC</p>
      </div>
      <div style="display:flex;gap:10px;align-items:center">
        <nz-select
          [(ngModel)]="selectedYear"
          (ngModelChange)="applyFilters()"
          nzAllowClear
          nzPlaceHolder="Todos los años"
          style="min-width:140px">
          @for (y of data()?.availableYears ?? []; track y) {
            <nz-option [nzValue]="y" [nzLabel]="y.toString()"></nz-option>
          }
        </nz-select>
        @if (selectedStatus() || selectedYear) {
          <button nz-button (click)="clearFilters()">
            <span nz-icon nzType="close"></span> Limpiar
          </button>
        }
        <!-- View toggle -->
        <div class="view-toggle">
          <button type="button" [class.active]="viewMode() === 'tabla'" (click)="viewMode.set('tabla')">Tabla</button>
          <button type="button" [class.active]="viewMode() === 'matriz'" (click)="viewMode.set('matriz')">Matriz</button>
        </div>
      </div>
    </div>

    @if (!data()) {
      <div style="display:flex;justify-content:center;padding:80px"><nz-spin nzSize="large" /></div>
    } @else {

      <!-- Stats por estado (clicables para filtrar) -->
      <div class="stats-grid">
        <div class="stat-box" [class.active]="selectedStatus() === 'Stopped'" (click)="toggleStatus('Stopped')">
          <div class="stat-label">Parados</div>
          <div class="stat-value" style="color:#595959">{{ data()!.stats.stopped }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'PlanningWithClient'" (click)="toggleStatus('PlanningWithClient')">
          <div class="stat-label">Planif. cliente</div>
          <div class="stat-value" style="color:#1890ff">{{ data()!.stats.planningWithClient }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'WaitingForDevelopers'" (click)="toggleStatus('WaitingForDevelopers')">
          <div class="stat-label">Esperando dev.</div>
          <div class="stat-value" style="color:#d4b106">{{ data()!.stats.waitingForDevelopers }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'PlanningSprint'" (click)="toggleStatus('PlanningSprint')">
          <div class="stat-label">Planif. sprint</div>
          <div class="stat-value" style="color:#13c2c2">{{ data()!.stats.planningSprint }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'InSprint'" (click)="toggleStatus('InSprint')">
          <div class="stat-label">En sprint</div>
          <div class="stat-value" style="color:#52c41a">{{ data()!.stats.inSprint }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'DevelopmentOutsideSprint'" (click)="toggleStatus('DevelopmentOutsideSprint')">
          <div class="stat-label">Fuera sprint</div>
          <div class="stat-value" style="color:#2f54eb">{{ data()!.stats.developmentOutsideSprint }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'InTesting'" (click)="toggleStatus('InTesting')">
          <div class="stat-label">En pruebas</div>
          <div class="stat-value" style="color:#fa8c16">{{ data()!.stats.inTesting }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'Completed'" (click)="toggleStatus('Completed')">
          <div class="stat-label">Finalizados</div>
          <div class="stat-value" style="color:#722ed1">{{ data()!.stats.completed }}</div>
        </div>
        <div class="stat-box" [class.active]="selectedStatus() === 'PostponedByClient'" (click)="toggleStatus('PostponedByClient')">
          <div class="stat-label">Pospuestos</div>
          <div class="stat-value" style="color:#ff4d4f">{{ data()!.stats.postponedByClient }}</div>
        </div>
      </div>

      @if (viewMode() === 'tabla') {
        <!-- Tabla de proyectos -->
        <nz-card>
          <nz-table
            [nzData]="data()!.projects"
            nzBordered
            nzSize="small"
            [nzShowPagination]="data()!.projects.length > 20">
            <thead>
              <tr>
                <th>Proyecto</th>
                <th>Unidad solicitante</th>
                <th>Complejidad</th>
                <th>Estado</th>
                <th>Año</th>
                <th>Equipo principal</th>
                <th>Valor</th>
                <th>Progreso tareas</th>
                <th>Hitos</th>
                <th style="width:110px">Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (p of data()!.projects; track p.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/projects', p.id]" style="font-weight:600">{{ p.title }}</a>
                    @if (p.activeSprintCount > 0) {
                      <nz-tag nzColor="purple" style="margin-left:6px;font-size:10px">
                        ⚡ {{ p.activeSprintCount }} sprint{{ p.activeSprintCount > 1 ? 's' : '' }} activo{{ p.activeSprintCount > 1 ? 's' : '' }}
                      </nz-tag>
                    }
                  </td>
                  <td style="font-size:13px;color:#595959">{{ p.requestingUnit }}</td>
                  <td><app-complexity-indicator [complexity]="asComplexity(p.complexity)" size="table" /></td>
                  <td><app-project-status-badge [status]="asStatus(p.status)" /></td>
                  <td style="text-align:center">{{ p.portfolioYear ?? '—' }}</td>
                  <td style="font-size:13px">{{ p.primaryTeamName ?? '—' }}</td>
                  <td style="white-space:nowrap">
                    @if (p.businessValue) {
                      <span class="bv-stars" [title]="bvLabel(p.businessValue)">
                        @for (i of starsArray(p.businessValue); track i) { ★ }
                        @for (i of starsArray(5 - p.businessValue); track i) { <span style="opacity:.25">★</span> }
                      </span>
                    } @else {
                      <span style="color:#bfbfbf;font-size:12px">—</span>
                    }
                  </td>
                  <td style="min-width:160px">
                    @if (p.totalWorkItems > 0) {
                      <div class="progress-cell">
                        <nz-progress style="flex:1"
                          [nzPercent]="progressPct(p)"
                          nzSize="small"
                          [nzShowInfo]="false"
                          [nzStrokeColor]="progressColor(p)">
                        </nz-progress>
                        <span style="font-size:12px;color:#8c8c8c;white-space:nowrap">
                          {{ p.doneWorkItems }}/{{ p.totalWorkItems }}
                        </span>
                      </div>
                    } @else {
                      <span style="color:#bfbfbf;font-size:13px">Sin tareas</span>
                    }
                  </td>
                  <td style="text-align:center">
                    @if (p.totalMilestones > 0) {
                      <span nz-tooltip [nzTooltipTitle]="p.reachedMilestones + ' alcanzados de ' + p.totalMilestones">
                        <span nz-icon nzType="flag" style="color:#eb2f96;margin-right:4px"></span>
                        {{ p.reachedMilestones }}/{{ p.totalMilestones }}
                      </span>
                    } @else {
                      <span style="color:#bfbfbf">—</span>
                    }
                  </td>
                  <td>
                    <div style="display:flex;gap:4px">
                      <a nz-button nzSize="small" [routerLink]="['/projects', p.id]">
                        <span nz-icon nzType="eye"></span>
                      </a>
                      <a nz-button nzSize="small" [routerLink]="['/projects', p.id, 'report']">
                        <span nz-icon nzType="bar-chart"></span>
                      </a>
                      <a nz-button nzSize="small" [routerLink]="['/projects', p.id, 'kanban']">
                        <span nz-icon nzType="project"></span>
                      </a>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="10"><nz-empty nzNotFoundContent="Sin proyectos con los filtros aplicados" /></td></tr>
              }
            </tbody>
          </nz-table>
        </nz-card>
      } @else {
        <!-- ── Matriz valor/esfuerzo ── -->
        <nz-card nzTitle="Matriz valor de negocio / esfuerzo (complejidad)" style="margin-bottom:16px">
          <p style="font-size:12px;color:#8c8c8c;margin-bottom:16px">
            Eje Y = valor de negocio (5 arriba = más valor) · Eje X = complejidad (1 izquierda = menos esfuerzo)
          </p>
          <div class="matrix-wrap">
            <div class="matrix-grid">
              <!-- 5 rows of cells + y-labels, then x-labels row -->
              @for (bv of [5,4,3,2,1]; track bv) {
                <!-- Y axis label in col 0 -->
                <div class="matrix-y-label">{{ bv }}</div>
                <!-- 5 complexity columns -->
                @for (cx of [1,2,3,4,5]; track cx) {
                  <div class="matrix-cell" [class]="cellQuadrant(bv, cx)">
                    <!-- Corner labels only on the 4 corner cells -->
                    @if (bv === 5 && cx === 1) {
                      <span class="matrix-corner-label tl" style="background:#f6ffed;color:#389e0d">Quick wins</span>
                    }
                    @if (bv === 5 && cx === 5) {
                      <span class="matrix-corner-label tr" style="background:#e6f7ff;color:#096dd9">Estratégicos</span>
                    }
                    @if (bv === 1 && cx === 1) {
                      <span class="matrix-corner-label bl" style="background:#fff7e6;color:#d46b08">Relleno</span>
                    }
                    @if (bv === 1 && cx === 5) {
                      <span class="matrix-corner-label br" style="background:#fff1f0;color:#cf1322">Cuestionables</span>
                    }
                    @for (p of matrixProjects()[bv + '-' + cx] ?? []; track p.id) {
                      <a class="project-chip" [routerLink]="['/projects', p.id]"
                        [title]="p.title + ' · ' + statusLabels(p.status)">
                        {{ p.title }}
                      </a>
                    }
                  </div>
                }
              }
              <!-- X axis labels row -->
              <div></div><!-- empty corner -->
              @for (cx of [1,2,3,4,5]; track cx) {
                <div class="matrix-x-label">{{ complexityLabel(cx) }}</div>
              }
            </div>
          </div>

          <!-- Sin valorar -->
          @if (unvaluedProjects().length > 0) {
            <div style="margin-top:16px;padding-top:12px;border-top:1px solid #f0f0f0">
              <span style="font-size:12px;color:#8c8c8c;font-weight:600">
                Sin valorar ({{ unvaluedProjects().length }}):
              </span>
              <span style="font-size:12px;color:#8c8c8c;margin-left:6px">
                @for (p of unvaluedProjects(); track p.id; let last = $last) {
                  <a [routerLink]="['/projects', p.id]" style="color:#595959">{{ p.title }}</a>@if (!last) {<span>, </span>}
                }
              </span>
            </div>
          }
        </nz-card>
      }
    }
  `,
})
export class PortfolioComponent {
  private readonly http = inject(HttpClient);

  selectedYear: number | null = null;
  readonly selectedStatus = signal<string | null>(null);
  readonly viewMode = signal<'tabla' | 'matriz'>('tabla');
  private readonly filter$ = new Subject<{ year: number | null; status: string | null }>();

  readonly data = toSignal(
    this.filter$.pipe(
      startWith({ year: null, status: null }),
      switchMap(({ year, status }) => {
        let params = new HttpParams();
        if (year)   params = params.set('year', year);
        if (status) params = params.set('status', status);
        return this.http.get<PortfolioDto>('/api/portfolio', { params });
      })
    )
  );

  readonly STATUS_LABELS = STATUS_LABELS;

  // ── Matrix computed ─────────────────────────────────────────────────────────

  /** Map keyed "bv-cx" → projects in that cell */
  readonly matrixProjects = computed(() => {
    const map: Record<string, PortfolioProjectDto[]> = {};
    for (const p of this.data()?.projects ?? []) {
      const bv = p.businessValue;
      if (!bv) continue;
      const cx = PROJECT_COMPLEXITY_ORDER[p.complexity as ProjectComplexity] ?? 0;
      if (!cx) continue;
      const key = `${bv}-${cx}`;
      (map[key] ??= []).push(p);
    }
    return map;
  });

  readonly unvaluedProjects = computed(() =>
    (this.data()?.projects ?? []).filter(p => !p.businessValue)
  );

  cellQuadrant(bv: number, cx: number): string {
    const highValue = bv >= 4;
    const highEffort = cx >= 4;
    if (highValue && !highEffort) return 'matrix-cell quadrant-qw';   // Quick wins
    if (highValue && highEffort)  return 'matrix-cell quadrant-str';  // Estratégicos
    if (!highValue && !highEffort) return 'matrix-cell quadrant-fil'; // Relleno
    return 'matrix-cell quadrant-que';                                  // Cuestionables
  }

  complexityLabel(cx: number): string {
    const labels: Record<number, string> = { 1: 'M.Peq', 2: 'Peq', 3: 'Med', 4: 'Gde', 5: 'M.Gde' };
    return labels[cx] ?? cx.toString();
  }

  statusLabels(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  bvLabel(bv: number): string {
    return BV_LABEL[bv] ?? bv.toString();
  }

  starsArray(n: number): number[] {
    return Array.from({ length: Math.max(0, n) }, (_, i) => i);
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  asStatus(status: string): ProjectStatus {
    return status as ProjectStatus;
  }

  asComplexity(complexity: string): ProjectComplexity {
    return complexity as ProjectComplexity;
  }

  applyFilters(): void {
    this.filter$.next({ year: this.selectedYear, status: this.selectedStatus() });
  }

  toggleStatus(status: string): void {
    this.selectedStatus.update(s => s === status ? null : status);
    this.applyFilters();
  }

  clearFilters(): void {
    this.selectedYear = null;
    this.selectedStatus.set(null);
    this.applyFilters();
  }

  progressPct(p: PortfolioProjectDto): number {
    if (p.totalWorkItems === 0) return 0;
    return Math.round((p.doneWorkItems / p.totalWorkItems) * 100);
  }

  progressColor(p: PortfolioProjectDto): string {
    const pct = this.progressPct(p);
    return pct >= 80 ? '#52c41a' : pct >= 40 ? '#faad14' : '#1890ff';
  }
}
