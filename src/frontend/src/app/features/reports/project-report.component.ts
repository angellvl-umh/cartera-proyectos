import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, Subject, startWith, of, catchError } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzTimelineModule } from 'ng-zorro-antd/timeline';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { ProjectStatusBadgeComponent } from '../projects/project-status-badge/project-status-badge.component';
import { SprintService, ProjectVelocityDto, SprintBurndownDto, ProjectCycleTimeDto } from '../projects/sprint.service';
import { BarChartComponent } from '../../shared/charts/bar-chart.component';
import { LineChartComponent } from '../../shared/charts/line-chart.component';

interface EpicReport { id: number; title: string; totalWorkItems: number; doneWorkItems: number; }
interface Milestone { id: number; title: string; status: string; hitoDate?: string; assignees: string[]; }
interface SprintReport {
  id: number; name: string; status: string;
  totalWorkItems: number; doneWorkItems: number;
  totalPoints: number;
  startDate?: string; endDate?: string;
}
interface RiskSummary {
  id: number; description: string; probability: string; impact: string; severity: number;
}

interface ProjectReportDto {
  projectId: number; title: string; status: string; requestingUnit: string;
  startDate?: string; endDate?: string;
  totalWorkItems: number; doneWorkItems: number;
  epics: EpicReport[];
  milestonesReached: Milestone[];
  milestonesUpcoming: Milestone[];
  sprints: SprintReport[];
  openRisks: RiskSummary[];
  dependsOnTitles: string[];
}

const SPRINT_STATUS_COLORS: Record<string, string> = {
  Planning: 'default', Active: 'processing', Completed: 'success',
};

@Component({
  selector: 'app-project-report',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, DecimalPipe, RouterLink,
    NzCardModule, NzProgressModule, NzTableModule, NzTagModule,
    NzButtonModule, NzIconModule, NzSpinModule, NzDividerModule, NzEmptyModule,
    NzStatisticModule, NzBadgeModule, NzTimelineModule, NzSelectModule,
    ProjectStatusBadgeComponent, BarChartComponent, LineChartComponent,
  ],
  styles: [`
    .report-wrapper { max-width: 1000px; margin: 0 auto; }
    .stat-row { display: flex; gap: 16px; margin-bottom: 24px; flex-wrap: wrap; }
    .stat-box { flex: 1; min-width: 140px; background: #fff; border-radius: 8px;
                padding: 16px 20px; border: 1px solid #f0f0f0;
                box-shadow: 0 1px 4px rgba(0,0,0,.06); }
    .epic-title { font-weight: 600; font-size: 13px; }
    .timeline-item { font-size: 13px; }
    .sprint-row { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; }
    .sprint-name { width: 120px; font-size: 13px; font-weight: 600; flex-shrink: 0;
                   white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .sprint-bar  { flex: 1; }
    .sprint-count { width: 60px; text-align: right; font-size: 12px; color: #8c8c8c; }
    .metrics-stat-row { display: flex; gap: 16px; flex-wrap: wrap; }
    .metrics-stat { flex: 1; min-width: 160px; background: #fafafa; border: 1px solid #f0f0f0;
                    border-radius: 8px; padding: 20px; text-align: center; }
    .metrics-stat .value { font-size: 28px; font-weight: 700; color: #1C7A4B; }
    .metrics-stat .label { font-size: 12px; color: #8c8c8c; margin-top: 4px; }
    .metrics-stat .sub { font-size: 11px; color: #bfbfbf; margin-top: 2px; }
  `],
  template: `
    @if (!report()) {
      <div style="display:flex;justify-content:center;padding:80px"><nz-spin nzSize="large" /></div>
    } @else {
      <div class="report-wrapper">

        <!-- Header -->
        <div style="margin-bottom:20px">
          <a [routerLink]="['/projects', report()!.projectId]" style="color:#595959;font-size:13px;display:inline-flex;align-items:center;gap:4px;text-decoration:none;margin-bottom:8px">
            <span nz-icon nzType="arrow-left"></span> Volver al proyecto
          </a>
          <div style="display:flex;align-items:center;gap:12px">
            <h2 style="margin:0">{{ report()!.title }}</h2>
            <app-project-status-badge [status]="report()!.status" />
          </div>
          <p style="color:#8c8c8c;margin:4px 0 0;font-size:13px">{{ report()!.requestingUnit }}</p>
        </div>

        <!-- Stats -->
        <div class="stat-row">
          <div class="stat-box">
            <div style="font-size:12px;color:#8c8c8c;margin-bottom:6px">Progreso global</div>
            <nz-progress [nzPercent]="globalPct()" nzStrokeColor="#52c41a" [nzFormat]="fmtPct"></nz-progress>
          </div>
          <div class="stat-box" style="text-align:center">
            <nz-statistic [nzValue]="report()!.totalWorkItems" nzTitle="Tareas totales"></nz-statistic>
          </div>
          <div class="stat-box" style="text-align:center">
            <nz-statistic [nzValue]="report()!.doneWorkItems" nzTitle="Completadas" [nzValueStyle]="{color:'#52c41a'}"></nz-statistic>
          </div>
          <div class="stat-box" style="text-align:center">
            <nz-statistic [nzValue]="report()!.totalWorkItems - report()!.doneWorkItems" nzTitle="Pendientes" [nzValueStyle]="{color:'#faad14'}"></nz-statistic>
          </div>
          <div class="stat-box" style="text-align:center">
            <nz-statistic [nzValue]="report()!.sprints.length" nzTitle="Sprints"></nz-statistic>
          </div>
        </div>

        <!-- Épicas -->
        <nz-card nzTitle="Épicas" style="margin-bottom:16px">
          @if (report()!.epics.length === 0) {
            <nz-empty nzNotFoundContent="Sin épicas" />
          } @else {
            @for (epic of report()!.epics; track epic.id) {
              <div style="margin-bottom:14px">
                <div style="display:flex;justify-content:space-between;align-items:baseline;margin-bottom:4px">
                  <span class="epic-title">{{ epic.title }}</span>
                  <span style="font-size:12px;color:#8c8c8c">{{ epic.doneWorkItems }}/{{ epic.totalWorkItems }} tareas</span>
                </div>
                <nz-progress
                  [nzPercent]="epicPct(epic)"
                  [nzStrokeColor]="epicColor(epic)"
                  nzSize="small"
                  [nzShowInfo]="false">
                </nz-progress>
              </div>
            }
          }
        </nz-card>

        <!-- Hitos -->
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px">

          <nz-card>
            <div style="font-weight:600;font-size:14px;margin-bottom:16px;display:flex;align-items:center;gap:8px">
              <span nz-icon nzType="check-circle" style="color:#52c41a"></span> Hitos alcanzados
              <nz-badge [nzCount]="report()!.milestonesReached.length" [nzStyle]="{backgroundColor:'#52c41a'}"></nz-badge>
            </div>
            @if (report()!.milestonesReached.length === 0) {
              <nz-empty nzNotFoundContent="Sin hitos completados aún" />
            } @else {
              <nz-timeline>
                @for (m of report()!.milestonesReached; track m.id) {
                  <nz-timeline-item nzColor="green">
                    <div class="timeline-item">
                      <span style="font-weight:600">{{ m.title }}</span>
                      @if (m.hitoDate) { <span style="color:#8c8c8c;font-size:12px;margin-left:8px">{{ m.hitoDate }}</span> }
                      @if (m.assignees.length > 0) {
                        <div style="font-size:11px;color:#8c8c8c;margin-top:2px">{{ m.assignees.join(', ') }}</div>
                      }
                    </div>
                  </nz-timeline-item>
                }
              </nz-timeline>
            }
          </nz-card>

          <nz-card>
            <div style="font-weight:600;font-size:14px;margin-bottom:16px;display:flex;align-items:center;gap:8px">
              <span nz-icon nzType="clock-circle" style="color:#1890ff"></span> Hitos próximos
              <nz-badge [nzCount]="report()!.milestonesUpcoming.length"></nz-badge>
            </div>
            @if (report()!.milestonesUpcoming.length === 0) {
              <nz-empty nzNotFoundContent="Sin hitos pendientes" />
            } @else {
              <nz-timeline>
                @for (m of report()!.milestonesUpcoming; track m.id) {
                  <nz-timeline-item [nzColor]="milestoneColor(m)">
                    <div class="timeline-item">
                      <span style="font-weight:600">{{ m.title }}</span>
                      @if (m.hitoDate) {
                        <span style="font-size:12px;margin-left:8px"
                          [style.color]="isOverdue(m.hitoDate!) ? '#ff4d4f' : '#8c8c8c'">
                          {{ m.hitoDate }}
                          @if (isOverdue(m.hitoDate!)) { <span> ⚠️ vencido</span> }
                        </span>
                      }
                      @if (m.assignees.length > 0) {
                        <div style="font-size:11px;color:#8c8c8c;margin-top:2px">{{ m.assignees.join(', ') }}</div>
                      }
                    </div>
                  </nz-timeline-item>
                }
              </nz-timeline>
            }
          </nz-card>

        </div>

        <!-- Sprints -->
        <nz-card nzTitle="Resumen de sprints" style="margin-bottom:16px">
          @if (report()!.sprints.length === 0) {
            <nz-empty nzNotFoundContent="Sin sprints" />
          } @else {
            @for (sprint of report()!.sprints; track sprint.id) {
              <div class="sprint-row">
                <span class="sprint-name">
                  {{ sprint.name }}
                  <nz-tag [nzColor]="SPRINT_STATUS_COLORS[sprint.status]" style="font-size:10px;margin-left:4px">{{ sprint.status }}</nz-tag>
                </span>
                <nz-progress
                  class="sprint-bar"
                  [nzPercent]="sprintPct(sprint)"
                  nzSize="small"
                  [nzShowInfo]="false"
                  [nzStrokeColor]="sprint.status === 'Completed' ? '#52c41a' : '#1890ff'">
                </nz-progress>
                <span class="sprint-count">{{ sprint.doneWorkItems }}/{{ sprint.totalWorkItems }}</span>
                @if (sprint.totalPoints > 0) {
                  <span style="font-size:12px;color:#722ed1;width:70px;text-align:right">🎯 {{ sprint.totalPoints }}pts</span>
                }
              </div>
            }
          }
        </nz-card>

        <!-- ═══════════════════════════════════════════════════════════ -->
        <!-- Riesgos abiertos                                            -->
        <!-- ═══════════════════════════════════════════════════════════ -->
        <nz-card nzTitle="Riesgos abiertos" style="margin-bottom:16px">
          @if (report()!.openRisks.length === 0) {
            <nz-empty nzNotFoundContent="Sin riesgos abiertos" />
          } @else {
            <nz-table [nzData]="report()!.openRisks" nzSize="small" [nzShowPagination]="false" nzBordered>
              <thead>
                <tr>
                  <th>Descripción</th>
                  <th style="width:90px">Probabilidad</th>
                  <th style="width:90px">Impacto</th>
                  <th style="width:80px">Severidad</th>
                </tr>
              </thead>
              <tbody>
                @for (r of report()!.openRisks; track r.id) {
                  <tr>
                    <td>{{ r.description }}</td>
                    <td>{{ riskLevelLabel(r.probability) }}</td>
                    <td>{{ riskLevelLabel(r.impact) }}</td>
                    <td>
                      <nz-tag [nzColor]="severityColor(r.severity)">{{ r.severity }}</nz-tag>
                    </td>
                  </tr>
                }
              </tbody>
            </nz-table>
          }
        </nz-card>

        <!-- ═══════════════════════════════════════════════════════════ -->
        <!-- Dependencias                                                 -->
        <!-- ═══════════════════════════════════════════════════════════ -->
        @if (report()!.dependsOnTitles.length > 0) {
          <nz-card style="margin-bottom:16px">
            <div style="font-size:13px;color:#595959">
              <strong>Depende de:</strong>
              @for (t of report()!.dependsOnTitles; track t; let last = $last) {
                <span style="margin-left:4px">{{ t }}@if (!last) {<span>,</span>}</span>
              }
            </div>
          </nz-card>
        }

        <!-- ═══════════════════════════════════════════════════════════ -->
        <!-- Velocidad                                                    -->
        <!-- ═══════════════════════════════════════════════════════════ -->
        <nz-card nzTitle="Velocidad por sprint" style="margin-bottom:16px">
          @if (!velocity()) {
            <div style="text-align:center;padding:32px"><nz-spin /></div>
          } @else if (velocity()!.sprints.length === 0) {
            <nz-empty nzNotFoundContent="Sin sprints completados aún" />
          } @else {
            <div style="margin-bottom:12px;font-size:13px;color:#595959">
              Velocidad media:
              <strong style="color:#1C7A4B">
                {{ velocity()!.averageVelocity !== null ? (velocity()!.averageVelocity! | number:'1.1-1') + ' pts/sprint' : '—' }}
              </strong>
            </div>
            <app-bar-chart
              [labels]="velocityLabels()"
              [series]="velocitySeries()"
              [height]="240"
            />
          }
        </nz-card>

        <!-- ═══════════════════════════════════════════════════════════ -->
        <!-- Burndown                                                     -->
        <!-- ═══════════════════════════════════════════════════════════ -->
        <nz-card nzTitle="Burndown del sprint" style="margin-bottom:16px">
          @if (sprintsWithDates().length === 0) {
            <nz-empty nzNotFoundContent="Sin sprints con fechas definidas" />
          } @else {
            <div style="margin-bottom:12px">
              <nz-select
                [(ngModel)]="selectedBurndownSprintId"
                (ngModelChange)="onBurndownSprintChange($event)"
                style="width:280px"
              >
                @for (s of sprintsWithDates(); track s.id) {
                  <nz-option [nzValue]="s.id" [nzLabel]="s.name + ' (' + s.status + ')'" />
                }
              </nz-select>
            </div>

            @if (burndownLoading()) {
              <div style="text-align:center;padding:32px"><nz-spin /></div>
            } @else if (burndownError()) {
              <div style="color:#8c8c8c;font-size:13px;padding:16px 0">
                <span nz-icon nzType="info-circle" style="margin-right:6px"></span>{{ burndownError() }}
              </div>
            } @else if (!burndown()) {
              <div style="text-align:center;padding:32px"><nz-spin /></div>
            } @else {
              <div style="margin-bottom:8px;font-size:13px;color:#595959">
                <strong>{{ burndown()!.name }}</strong>
                &nbsp;·&nbsp; Total: <strong>{{ burndown()!.totalPoints }} pts</strong>
                &nbsp;·&nbsp; {{ burndown()!.startDate }} → {{ burndown()!.endDate }}
              </div>
              <app-line-chart
                [labels]="burndownLabels()"
                [series]="burndownSeries()"
                [height]="260"
              />
            }
          }
        </nz-card>

        <!-- ═══════════════════════════════════════════════════════════ -->
        <!-- Cycle / Lead time                                           -->
        <!-- ═══════════════════════════════════════════════════════════ -->
        <nz-card nzTitle="Cycle time y Lead time">
          @if (!cycleTime()) {
            <div style="text-align:center;padding:32px"><nz-spin /></div>
          } @else if (cycleTime()!.completedItemsCount === 0) {
            <nz-empty nzNotFoundContent="Sin tareas completadas aún" />
          } @else {
            <div class="metrics-stat-row">
              <div class="metrics-stat">
                <div class="value">
                  {{ cycleTime()!.averageCycleTimeDays !== null ? (cycleTime()!.averageCycleTimeDays! | number:'1.1-1') : '—' }}
                </div>
                <div class="label">Cycle time medio (días)</div>
                <div class="sub">Desde inicio de trabajo hasta Done</div>
              </div>
              <div class="metrics-stat">
                <div class="value" style="color:#3A74D0">
                  {{ cycleTime()!.averageLeadTimeDays !== null ? (cycleTime()!.averageLeadTimeDays! | number:'1.1-1') : '—' }}
                </div>
                <div class="label">Lead time medio (días)</div>
                <div class="sub">Desde creación hasta Done</div>
              </div>
              <div class="metrics-stat">
                <div class="value" style="color:#8c8c8c">{{ cycleTime()!.completedItemsCount }}</div>
                <div class="label">Tareas completadas</div>
                <div class="sub">Base del cálculo</div>
              </div>
            </div>
          }
        </nz-card>

      </div>
    }
  `,
})
export class ProjectReportComponent {
  private readonly http = inject(HttpClient);
  private readonly sprintService = inject(SprintService);
  private readonly projectId = +inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  readonly report = toSignal(
    this.http.get<ProjectReportDto>(`/api/projects/${this.projectId}/report`)
  );

  // ── Velocity ────────────────────────────────────────────────────────────────

  readonly velocity = toSignal(
    this.sprintService.getVelocity(this.projectId)
  );

  readonly velocityLabels = computed(() =>
    (this.velocity()?.sprints ?? []).map(s => s.name)
  );

  readonly velocitySeries = computed(() => {
    const sprints = this.velocity()?.sprints ?? [];
    return [
      { name: 'Comprometido', color: '#3A74D0', values: sprints.map(s => s.committedPoints) },
      { name: 'Entregado', color: '#1C7A4B', values: sprints.map(s => s.deliveredPoints) },
    ];
  });

  // ── Burndown ─────────────────────────────────────────────────────────────────

  readonly sprintsWithDates = computed(() =>
    (this.report()?.sprints ?? []).filter(s => s.startDate && s.endDate)
  );

  selectedBurndownSprintId: number | null = null;

  private readonly burndownRefresh$ = new Subject<number>();

  readonly burndownLoading = signal(false);
  readonly burndownError = signal<string | null>(null);
  readonly burndown = signal<SprintBurndownDto | null>(null);

  readonly burndownLabels = computed(() =>
    (this.burndown()?.days ?? []).map(d => d.date.slice(5)) // MM-DD
  );

  readonly burndownSeries = computed(() => {
    const days = this.burndown()?.days ?? [];
    return [
      {
        name: 'Ideal',
        color: '#bfbfbf',
        dashed: true,
        values: days.map(d => d.idealPoints),
      },
      {
        name: 'Real',
        color: '#3A74D0',
        dashed: false,
        values: days.map(d => d.remainingPoints as number | null),
      },
    ];
  });

  onBurndownSprintChange(sprintId: number): void {
    this.selectedBurndownSprintId = sprintId;
    this.loadBurndown(sprintId);
  }

  private loadBurndown(sprintId: number): void {
    this.burndownLoading.set(true);
    this.burndownError.set(null);
    this.burndown.set(null);
    this.sprintService.getBurndown(this.projectId, sprintId).subscribe({
      next: data => {
        this.burndown.set(data);
        this.burndownLoading.set(false);
      },
      error: (err) => {
        const msg = err?.error?.message ?? err?.error ?? 'Error al cargar el burndown';
        this.burndownError.set(typeof msg === 'string' ? msg : 'El sprint no tiene fechas definidas');
        this.burndownLoading.set(false);
      },
    });
  }

  // Pre-select the active sprint or last completed once report loads
  private autoSelectBurndownSprint(): void {
    const sprints = this.sprintsWithDates();
    if (!sprints.length || this.selectedBurndownSprintId !== null) return;
    const active = sprints.find(s => s.status === 'Active');
    const target = active ?? sprints[sprints.length - 1];
    if (target) {
      this.selectedBurndownSprintId = target.id;
      this.loadBurndown(target.id);
    }
  }

  // ── Cycle time ──────────────────────────────────────────────────────────────

  readonly cycleTime = toSignal(
    this.sprintService.getCycleTime(this.projectId)
  );

  // ── Shared helpers ──────────────────────────────────────────────────────────

  readonly globalPct = computed(() => {
    const r = this.report();
    if (!r || r.totalWorkItems === 0) return 0;
    return Math.round((r.doneWorkItems / r.totalWorkItems) * 100);
  });

  readonly fmtPct = (p: number) => `${p}%`;
  readonly SPRINT_STATUS_COLORS = SPRINT_STATUS_COLORS;

  // Auto-select burndown sprint once report has loaded (effect via computed side-effect trick)
  private readonly _autoSelect = computed(() => {
    // triggers when sprintsWithDates changes
    if (this.sprintsWithDates().length > 0 && this.selectedBurndownSprintId === null) {
      // Use setTimeout to avoid writing signals during computation
      setTimeout(() => this.autoSelectBurndownSprint(), 0);
    }
    return null;
  });

  constructor() {
    // Activate the auto-select computed
    this._autoSelect();
  }

  epicPct(e: EpicReport): number {
    if (e.totalWorkItems === 0) return 0;
    return Math.round((e.doneWorkItems / e.totalWorkItems) * 100);
  }

  epicColor(e: EpicReport): string {
    const pct = this.epicPct(e);
    return pct >= 80 ? '#52c41a' : pct >= 40 ? '#faad14' : '#1890ff';
  }

  sprintPct(s: SprintReport): number {
    if (s.totalWorkItems === 0) return 0;
    return Math.round((s.doneWorkItems / s.totalWorkItems) * 100);
  }

  isOverdue(dateStr: string): boolean {
    return new Date(dateStr) < new Date();
  }

  milestoneColor(m: Milestone): string {
    if (!m.hitoDate) return 'blue';
    return this.isOverdue(m.hitoDate) ? 'red' : 'blue';
  }

  riskLevelLabel(level: string): string {
    const map: Record<string, string> = { Low: 'Baja', Medium: 'Media', High: 'Alta' };
    return map[level] ?? level;
  }

  severityColor(severity: number): string {
    if (severity <= 2) return 'success';
    if (severity <= 4) return 'warning';
    return 'error';
  }
}
