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

interface PortfolioProjectDto {
  id: number; title: string; status: string; requestingUnit: string; complexity: string;
  portfolioYear?: number; startDate?: string; endDate?: string;
  primaryTeamName?: string;
  totalWorkItems: number; doneWorkItems: number;
  totalMilestones: number; reachedMilestones: number;
  activeSprintCount: number;
}
interface PortfolioStatsDto {
  total: number;
  stopped: number;
  planningWithClient: number;
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

const STATUS_COLORS: Record<string, string> = {
  Stopped: 'default', PlanningWithClient: 'blue', PlanningSprint: 'cyan',
  InSprint: 'green', DevelopmentOutsideSprint: 'geekblue',
  InTesting: 'orange', Completed: 'purple', PostponedByClient: 'red',
};
const STATUS_LABELS: Record<string, string> = {
  Stopped: 'Parado', PlanningWithClient: 'Planif. cliente', PlanningSprint: 'Planif. sprint',
  InSprint: 'En sprint', DevelopmentOutsideSprint: 'Desarro. fuera sprint',
  InTesting: 'En pruebas', Completed: 'Finalizado', PostponedByClient: 'Pospuesto cliente',
};
const COMPLEXITY_COLORS: Record<string, string> = {
  VerySmall: 'green', Small: 'blue', Medium: 'orange', Large: 'red',
};

@Component({
  selector: 'app-portfolio',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule,
    NzCardModule, NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzSpinModule, NzSelectModule, NzProgressModule, NzStatisticModule,
    NzEmptyModule, NzTooltipModule,
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
                <td><nz-tag [nzColor]="COMPLEXITY_COLORS[p.complexity]">{{ p.complexity }}</nz-tag></td>
                <td><nz-tag [nzColor]="STATUS_COLORS[p.status]">{{ STATUS_LABELS[p.status] }}</nz-tag></td>
                <td style="text-align:center">{{ p.portfolioYear ?? '—' }}</td>
                <td style="font-size:13px">{{ p.primaryTeamName ?? '—' }}</td>
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
              <tr><td colspan="9"><nz-empty nzNotFoundContent="Sin proyectos con los filtros aplicados" /></td></tr>
            }
          </tbody>
        </nz-table>
      </nz-card>
    }
  `,
})
export class PortfolioComponent {
  private readonly http = inject(HttpClient);

  selectedYear: number | null = null;
  readonly selectedStatus = signal<string | null>(null);
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

  readonly STATUS_COLORS     = STATUS_COLORS;
  readonly STATUS_LABELS     = STATUS_LABELS;
  readonly COMPLEXITY_COLORS = COMPLEXITY_COLORS;

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
