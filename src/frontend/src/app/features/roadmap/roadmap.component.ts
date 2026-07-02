import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzCollapseModule } from 'ng-zorro-antd/collapse';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzIconModule } from 'ng-zorro-antd/icon';
import {
  RoadmapService,
  PortfolioRoadmapDto,
  RoadmapProjectDto,
} from './roadmap.service';
import {
  PROJECT_STATUS_PILL_COLORS,
  PROJECT_STATUS_LABELS,
  ProjectStatus,
} from '../projects/project.model';

const MONTHS = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

interface BarPosition {
  left: number;
  width: number;
}

interface MilestoneMarker {
  id: number;
  title: string;
  hitoDate: string;
  reached: boolean;
  left: number;
}

interface DeploymentMarker {
  date: string;
  left: number;
}

@Component({
  selector: 'app-roadmap',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    FormsModule,
    NzSelectModule,
    NzSpinModule,
    NzEmptyModule,
    NzCollapseModule,
    NzTooltipModule,
    NzIconModule,
  ],
  styles: [`
    .roadmap-wrapper { font-size: 13px; }

    .top-bar {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }
    .top-bar h2 { margin: 0; font-size: 22px; }

    .legend {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
      font-size: 12px;
      margin-left: auto;
    }
    .legend-item { display: flex; align-items: center; gap: 5px; }
    .legend-swatch {
      width: 14px; height: 14px; border-radius: 3px;
      border-width: 1.5px; border-style: solid;
      flex: 0 0 auto;
    }

    .grid-container {
      overflow-x: auto;
      border: 1px solid #e8e8e8;
      border-radius: 8px;
    }
    .grid-inner { min-width: 800px; }

    /* Month header */
    .month-header {
      display: grid;
      grid-template-columns: 220px 1fr;
      background: #fafafa;
      border-bottom: 2px solid #e8e8e8;
      position: sticky;
      top: 0;
      z-index: 10;
    }
    .month-header-label {
      padding: 8px 12px;
      font-weight: 700;
      font-size: 12px;
      color: #595959;
      border-right: 1px solid #e8e8e8;
    }
    .month-cells {
      position: relative;
      display: grid;
      grid-template-columns: repeat(12, 1fr);
    }
    .month-cell {
      padding: 8px 4px;
      text-align: center;
      font-size: 11px;
      font-weight: 700;
      color: #8c8c8c;
      border-right: 1px solid #f0f0f0;
    }
    .month-cell:last-child { border-right: none; }
    .month-cell.current-month { color: #1890ff; }

    .today-line {
      position: absolute;
      top: 0;
      bottom: -9999px; /* extends through entire grid */
      width: 2px;
      background: rgba(24, 144, 255, 0.4);
      pointer-events: none;
      z-index: 4;
    }

    /* Team separator */
    .team-row {
      display: grid;
      grid-template-columns: 220px 1fr;
      background: #f5f5f5;
      border-top: 1px solid #e8e8e8;
      border-bottom: 1px solid #e0e0e0;
    }
    .team-label {
      padding: 6px 12px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #595959;
      border-right: 1px solid #e8e8e8;
    }

    /* Project row */
    .project-row {
      display: grid;
      grid-template-columns: 220px 1fr;
      min-height: 40px;
      border-bottom: 1px solid #f0f0f0;
    }
    .project-row:hover .bar-area { background: #f9feff; }
    .project-label {
      padding: 8px 12px;
      display: flex;
      align-items: center;
      border-right: 1px solid #e8e8e8;
      min-width: 0;
    }
    .project-title-link {
      font-weight: 600;
      font-size: 12.5px;
      color: #262626;
      text-decoration: none;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 196px;
      display: block;
    }
    .project-title-link:hover { color: #1890ff; }

    /* Bar area */
    .bar-area {
      position: relative;
      display: grid;
      grid-template-columns: repeat(12, 1fr);
      align-items: center;
    }
    .bar-bg-col {
      height: 100%;
      border-right: 1px solid #f5f5f5;
      min-height: 40px;
    }
    .bar-bg-col:last-child { border-right: none; }

    /* Gantt bar */
    .gantt-bar {
      position: absolute;
      top: 50%;
      transform: translateY(-50%);
      height: 22px;
      border-radius: 4px;
      border-width: 1.5px;
      border-style: solid;
      min-width: 3px;
    }

    /* Markers */
    .marker {
      position: absolute;
      top: 50%;
      transform: translate(-50%, -50%);
      font-size: 13px;
      line-height: 1;
      cursor: default;
      z-index: 3;
    }
    .deploy-marker {
      position: absolute;
      top: 50%;
      transform: translate(-50%, -50%);
      font-size: 12px;
      line-height: 1;
      color: #722ed1;
      cursor: default;
      z-index: 3;
    }

    .collapse-section { margin-top: 16px; }
  `],
  template: `
    <div class="roadmap-wrapper">

      <!-- Top bar -->
      <div class="top-bar">
        <h2>
          <span nz-icon nzType="schedule" style="color:#1890ff;margin-right:8px"></span>
          Roadmap de cartera
        </h2>

        <nz-select [(ngModel)]="selectedYear" (ngModelChange)="onYearChange($event)" style="width:120px">
          @for (y of availableYears(); track y) {
            <nz-option [nzValue]="y" [nzLabel]="y.toString()" />
          }
        </nz-select>

        <!-- Legend -->
        <div class="legend">
          @for (e of legendEntries; track e.status) {
            <div class="legend-item">
              <span class="legend-swatch" [style.background]="e.bg" [style.border-color]="e.fg"></span>
              <span style="color:#595959">{{ e.label }}</span>
            </div>
          }
          <div class="legend-item"><span>◆</span><span style="color:#595959">Hito alcanzado</span></div>
          <div class="legend-item"><span style="color:#8c8c8c">◇</span><span style="color:#595959">Hito pendiente</span></div>
          <div class="legend-item"><span style="color:#722ed1">▼</span><span style="color:#595959">Implantación</span></div>
        </div>
      </div>

      <!-- Loading / empty -->
      @if (loading()) {
        <div style="display:flex;justify-content:center;padding:80px">
          <nz-spin nzSize="large" />
        </div>
      } @else if (!roadmap()) {
        <nz-empty nzNotFoundContent="Sin datos de roadmap" />
      } @else {

        <!-- Grid -->
        <div class="grid-container">
          <div class="grid-inner">

            <!-- Month header -->
            <div class="month-header">
              <div class="month-header-label">Proyecto</div>
              <div class="month-cells">
                @for (m of MONTHS; track $index) {
                  <div class="month-cell" [class.current-month]="isCurrentMonth($index + 1)">{{ m }}</div>
                }
                @if (isCurrentYear()) {
                  <div class="today-line" [style.left]="todayPercent() + '%'"></div>
                }
              </div>
            </div>

            <!-- Teams -->
            @for (team of roadmap()!.teams; track team.teamId) {

              <!-- Team separator -->
              <div class="team-row">
                <div class="team-label">
                  <span nz-icon nzType="team" style="margin-right:6px"></span>{{ team.teamName }}
                </div>
                <div style="background:#f5f5f5"></div>
              </div>

              <!-- Project rows -->
              @for (project of team.projects; track project.id) {
                <div class="project-row">
                  <div class="project-label">
                    <a [routerLink]="['/projects', project.id]"
                      class="project-title-link"
                      [title]="project.title">{{ project.title }}</a>
                  </div>
                  <div class="bar-area">
                    @for (m of MONTHS; track $index) {
                      <div class="bar-bg-col"></div>
                    }

                    @if (getBarPosition(project); as bar) {
                      <div class="gantt-bar"
                        [style.left]="bar.left + '%'"
                        [style.width]="bar.width + '%'"
                        [style.background]="statusColors(project.status).bg"
                        [style.border-color]="statusColors(project.status).fg"
                        nz-tooltip
                        [nzTooltipTitle]="project.title + ' · ' + (project.startDate ?? '?') + ' → ' + (project.endDate ?? project.desiredDeploymentDate ?? '?')"
                      ></div>
                    }

                    @for (ms of getMilestoneMarkers(project); track ms.id) {
                      <span class="marker"
                        [style.left]="ms.left + '%'"
                        nz-tooltip
                        [nzTooltipTitle]="ms.title + ' · ' + ms.hitoDate">
                        {{ ms.reached ? '◆' : '◇' }}
                      </span>
                    }

                    @if (getDeploymentMarker(project); as dm) {
                      <span class="deploy-marker"
                        [style.left]="dm.left + '%'"
                        nz-tooltip
                        [nzTooltipTitle]="'Implantación deseada: ' + dm.date">▼</span>
                    }
                  </div>
                </div>
              }

              @if (team.projects.length === 0) {
                <div style="padding:8px 12px;color:#8c8c8c;font-size:12px;border-bottom:1px solid #f0f0f0">
                  Sin proyectos para este año
                </div>
              }
            }

            @if (roadmap()!.teams.length === 0) {
              <div style="padding:24px;text-align:center">
                <nz-empty nzNotFoundContent="No hay proyectos con fechas para este año" />
              </div>
            }

          </div>
        </div>

        <!-- Collapse sections for unassigned / undated -->
        <div class="collapse-section">
          <nz-collapse [nzBordered]="true">
            @if (roadmap()!.unassigned.length > 0) {
              <nz-collapse-panel
                [nzHeader]="'Sin equipo asignado (' + roadmap()!.unassigned.length + ')'"
                [nzActive]="false">
                <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:8px;padding:4px 0">
                  @for (p of roadmap()!.unassigned; track p.id) {
                    <div style="padding:8px 12px;background:#fafafa;border:1px solid #f0f0f0;border-radius:8px">
                      <a [routerLink]="['/projects', p.id]"
                        style="font-weight:600;font-size:13px;color:#262626;text-decoration:none;display:block">
                        {{ p.title }}
                      </a>
                      <span style="font-size:11px;color:#8c8c8c">
                        {{ p.startDate ?? '—' }} → {{ p.endDate ?? p.desiredDeploymentDate ?? '—' }}
                      </span>
                    </div>
                  }
                </div>
              </nz-collapse-panel>
            }
            @if (roadmap()!.undated.length > 0) {
              <nz-collapse-panel
                [nzHeader]="'Sin fechas (' + roadmap()!.undated.length + ')'"
                [nzActive]="false">
                <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:8px;padding:4px 0">
                  @for (p of roadmap()!.undated; track p.id) {
                    <div style="padding:8px 12px;background:#fafafa;border:1px solid #f0f0f0;border-radius:8px">
                      <a [routerLink]="['/projects', p.id]"
                        style="font-weight:600;font-size:13px;color:#262626;text-decoration:none;display:block">
                        {{ p.title }}
                      </a>
                    </div>
                  }
                </div>
              </nz-collapse-panel>
            }
          </nz-collapse>
        </div>

      }
    </div>
  `,
})
export class RoadmapComponent {
  private readonly roadmapService = inject(RoadmapService);

  readonly MONTHS = MONTHS;

  loading = signal(false);
  roadmap = signal<PortfolioRoadmapDto | null>(null);
  selectedYear = new Date().getFullYear();

  readonly availableYears = computed(() => {
    const backendYears = this.roadmap()?.availableYears ?? [];
    const set = new Set([this.selectedYear, ...backendYears]);
    return Array.from(set).sort((a, b) => b - a);
  });

  readonly legendEntries = (Object.keys(PROJECT_STATUS_PILL_COLORS) as ProjectStatus[]).map(s => ({
    status: s,
    label: PROJECT_STATUS_LABELS[s],
    bg: PROJECT_STATUS_PILL_COLORS[s].bg,
    fg: PROJECT_STATUS_PILL_COLORS[s].fg,
  }));

  constructor() {
    this.loadRoadmap(this.selectedYear);
  }

  onYearChange(year: number): void {
    this.selectedYear = year;
    this.loadRoadmap(year);
  }

  private loadRoadmap(year: number): void {
    this.loading.set(true);
    this.roadmapService.getRoadmap(year).subscribe({
      next: data => { this.roadmap.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  // ── Date helpers ───────────────────────────────────────────────────────────

  private daysInYear(year: number): number {
    return year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0) ? 366 : 365;
  }

  private percentInYear(dateStr: string, year: number): number {
    const d = new Date(dateStr + 'T00:00:00');
    const y = d.getFullYear();
    if (y < year) return 0;
    if (y > year) return 100;
    const startOfYear = new Date(year, 0, 1);
    const days = Math.floor((d.getTime() - startOfYear.getTime()) / 86400000);
    return (days / this.daysInYear(year)) * 100;
  }

  isCurrentYear(): boolean {
    return this.selectedYear === new Date().getFullYear();
  }

  isCurrentMonth(month: number): boolean {
    const now = new Date();
    return this.selectedYear === now.getFullYear() && now.getMonth() + 1 === month;
  }

  readonly todayPercent = computed(() => {
    const now = new Date();
    const year = this.selectedYear;
    const startOfYear = new Date(year, 0, 1);
    const days = Math.floor((now.getTime() - startOfYear.getTime()) / 86400000);
    return (days / this.daysInYear(year)) * 100;
  });

  getBarPosition(project: RoadmapProjectDto): BarPosition | null {
    if (!project.startDate) return null;

    const year = this.selectedYear;
    const yearStart = new Date(year, 0, 1);
    const yearEnd = new Date(year, 11, 31);

    const startD = new Date(project.startDate + 'T00:00:00');
    const effectiveEndStr = project.endDate ?? project.desiredDeploymentDate;
    const endD = effectiveEndStr
      ? new Date(effectiveEndStr + 'T00:00:00')
      : new Date(year, 11, 31);

    if (endD < yearStart || startD > yearEnd) return null;

    const clippedStart = startD < yearStart ? yearStart : startD;
    const clippedEnd = endD > yearEnd ? yearEnd : endD;

    const leftPct = this.percentInYear(clippedStart.toISOString().split('T')[0], year);
    const rightPct = this.percentInYear(clippedEnd.toISOString().split('T')[0], year);
    const width = Math.max(rightPct - leftPct, 0.5);

    return { left: leftPct, width };
  }

  getMilestoneMarkers(project: RoadmapProjectDto): MilestoneMarker[] {
    const year = this.selectedYear;
    return project.milestones
      .filter(m => m.hitoDate?.startsWith(String(year)))
      .map(m => ({
        id: m.id,
        title: m.title,
        hitoDate: m.hitoDate!,
        reached: m.reached,
        left: this.percentInYear(m.hitoDate!, year),
      }));
  }

  getDeploymentMarker(project: RoadmapProjectDto): DeploymentMarker | null {
    const ddd = project.desiredDeploymentDate;
    if (!ddd?.startsWith(String(this.selectedYear))) return null;
    return { date: ddd, left: this.percentInYear(ddd, this.selectedYear) };
  }

  statusColors(status: string): { bg: string; fg: string } {
    const s = status as ProjectStatus;
    const c = PROJECT_STATUS_PILL_COLORS[s];
    return c ? { bg: c.bg, fg: c.fg } : { bg: '#ECEAE6', fg: '#6B6661' };
  }
}
