import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
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

interface EpicReport { id: number; title: string; totalWorkItems: number; doneWorkItems: number; }
interface Milestone { id: number; title: string; status: string; hitoDate?: string; assignees: string[]; }
interface SprintReport { id: number; name: string; status: string; totalWorkItems: number; doneWorkItems: number; totalPoints: number; }

interface ProjectReportDto {
  projectId: number; title: string; status: string; requestingUnit: string;
  startDate?: string; endDate?: string;
  totalWorkItems: number; doneWorkItems: number;
  epics: EpicReport[];
  milestonesReached: Milestone[];
  milestonesUpcoming: Milestone[];
  sprints: SprintReport[];
}

const STATUS_LABELS: Record<string, string> = {
  Proposed: 'Propuesto', Approved: 'Aprobado', InProgress: 'En ejecución',
  Paused: 'Pausado', Completed: 'Completado', Cancelled: 'Cancelado',
};
const SPRINT_STATUS_COLORS: Record<string, string> = {
  Planning: 'default', Active: 'processing', Completed: 'success',
};

@Component({
  selector: 'app-project-report',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, NzCardModule, NzProgressModule, NzTableModule, NzTagModule,
    NzButtonModule, NzIconModule, NzSpinModule, NzDividerModule, NzEmptyModule,
    NzStatisticModule, NzBadgeModule, NzTimelineModule,
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
            <nz-tag nzColor="processing">{{ statusLabel(report()!.status) }}</nz-tag>
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
        <nz-card nzTitle="Resumen de sprints">
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

      </div>
    }
  `,
})
export class ProjectReportComponent {
  private readonly http = inject(HttpClient);
  private readonly projectId = +inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  readonly report = toSignal(
    this.http.get<ProjectReportDto>(`/api/projects/${this.projectId}/report`)
  );

  readonly globalPct = computed(() => {
    const r = this.report();
    if (!r || r.totalWorkItems === 0) return 0;
    return Math.round((r.doneWorkItems / r.totalWorkItems) * 100);
  });

  readonly fmtPct = (p: number) => `${p}%`;

  readonly SPRINT_STATUS_COLORS = SPRINT_STATUS_COLORS;

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

  statusLabel(s: string): string { return STATUS_LABELS[s] ?? s; }
}
