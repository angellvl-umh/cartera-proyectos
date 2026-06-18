import { ChangeDetectionStrategy, Component, computed, inject, Input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import {
  PROJECT_STATUS_LABELS,
  PROJECT_STATUS_PILL_COLORS,
  ProjectComplexity,
  ProjectStatus,
} from '../project.model';
import { ComplexityIndicatorComponent } from '../complexity-indicator/complexity-indicator.component';

interface PortfolioProjectDto {
  id: number;
  title: string;
  status: ProjectStatus;
  requestingUnit: string;
  complexity: ProjectComplexity;
  portfolioYear?: number;
  primaryTeamName?: string;
}

interface PortfolioDto {
  projects: PortfolioProjectDto[];
}

const STATUS_COLUMN_ORDER: ProjectStatus[] = [
  'PlanningWithClient',
  'PlanningSprint',
  'InSprint',
  'DevelopmentOutsideSprint',
  'WaitingForDevelopers',
  'InTesting',
  'Completed',
  'PostponedByClient',
  'Stopped',
];

@Component({
  selector: 'app-kanban-by-status',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzSpinModule, ComplexityIndicatorComponent],
  styles: [`
    .board { display: flex; gap: 14px; overflow-x: auto; padding-bottom: 8px; }
    .column { width: 288px; flex: 0 0 auto; background: #EFECE6; border-radius: 12px; padding: 12px; }
    .column-header { display: flex; align-items: center; gap: 8px; padding: 4px 6px 12px; font-size: 13px; font-weight: 700; color: var(--ink); }
    .column-header .dot { width: 8px; height: 8px; border-radius: 999px; flex: 0 0 auto; }
    .column-header .count { margin-left: auto; font-size: 12px; font-weight: 600; color: var(--text-muted); }
    .card { background: #fff; border-radius: 10px; padding: 12px; margin-bottom: 10px; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
    .card-title { font-size: 13.5px; font-weight: 600; color: var(--ink); margin-bottom: 6px; }
    .card-team { font-size: 12px; color: var(--text-muted); margin-bottom: 8px; }
    .empty { font-size: 12.5px; color: var(--text-muted); padding: 8px 6px; }
  `],
  template: `
    @if (loading()) {
      <div style="display:flex;justify-content:center;padding:60px"><nz-spin nzSize="large" /></div>
    } @else {
      <div class="board">
        @for (status of statusOrder; track status) {
          <div class="column">
            <div class="column-header">
              <span class="dot" [style.background]="pillColors[status].dot"></span>
              <span>{{ statusLabels[status] }}</span>
              <span class="count">{{ projectsByStatus()[status]?.length ?? 0 }}</span>
            </div>
            @for (p of projectsByStatus()[status] ?? []; track p.id) {
              <div class="card" (click)="goToDetail(p.id)" style="cursor:pointer">
                <div class="card-title">{{ p.title }}</div>
                <div class="card-team">{{ p.primaryTeamName ?? p.requestingUnit }}</div>
                <app-complexity-indicator [complexity]="p.complexity" size="card" [showLabel]="false" />
              </div>
            } @empty {
              <div class="empty">Sin proyectos</div>
            }
          </div>
        }
      </div>
    }
  `,
})
export class KanbanByStatusComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  @Input() filterQ = '';
  @Input() filterComplexity: ProjectComplexity | null = null;
  @Input() filterTagIds: number[] = [];

  readonly statusOrder = STATUS_COLUMN_ORDER;
  readonly statusLabels = PROJECT_STATUS_LABELS;
  readonly pillColors = PROJECT_STATUS_PILL_COLORS;

  private readonly portfolio = toSignal(this.http.get<PortfolioDto>('/api/portfolio'));
  readonly loading = computed(() => !this.portfolio());

  private readonly filteredProjects = computed(() => {
    const all = this.portfolio()?.projects ?? [];
    const q = this.filterQ.trim().toLowerCase();
    return all.filter(p => {
      if (q && !p.title.toLowerCase().includes(q)) return false;
      if (this.filterComplexity && p.complexity !== this.filterComplexity) return false;
      return true;
    });
  });

  readonly projectsByStatus = computed(() => {
    const map: Partial<Record<ProjectStatus, PortfolioProjectDto[]>> = {};
    for (const p of this.filteredProjects()) {
      (map[p.status] ??= []).push(p);
    }
    return map;
  });

  goToDetail(id: number): void {
    this.router.navigate(['/projects', id]);
  }
}
