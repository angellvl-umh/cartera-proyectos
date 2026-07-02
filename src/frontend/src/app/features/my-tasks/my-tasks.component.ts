import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, Subject, startWith } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzMessageService } from 'ng-zorro-antd/message';
import { WorkItemsService, WorkItemStatus } from '../projects/workitems.service';

interface MyWorkItemDto {
  id: number; projectId: number; projectTitle: string;
  epicId?: number; epicTitle?: string;
  sprintId?: number; sprintName?: string;
  title: string; status: string; priority: string;
  estimationHours?: number; estimationPoints?: number;
  isHito: boolean; hitoDate?: string; dueDate?: string;
}
interface WorkItemCountsDto {
  total: number; backlog: number; toDo: number; inProgress: number; blocked: number; done: number;
}
interface MyWorkItemsDto { items: MyWorkItemDto[]; total: number; counts: WorkItemCountsDto; }

const STATUS_COLORS: Record<string, string> = {
  Backlog: 'default', ToDo: 'processing', InProgress: 'warning',
  Blocked: 'error', Done: 'success', Discarded: 'default',
};
const STATUS_LABELS: Record<string, string> = {
  Backlog: 'Backlog', ToDo: 'Por hacer', InProgress: 'En progreso',
  Blocked: 'Bloqueada', Done: 'Hecho', Discarded: 'Descartada',
};
const PRIORITY_COLORS: Record<string, string> = {
  Low: 'default', Medium: 'processing', High: 'warning', Critical: 'error',
};

const TERMINAL_STATUSES = ['Done', 'Discarded'];
const SELECTABLE_STATUSES: WorkItemStatus[] = ['Backlog', 'ToDo', 'InProgress', 'Blocked', 'Done'];

@Component({
  selector: 'app-my-tasks',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule,
    NzCardModule, NzTableModule, NzTagModule, NzButtonModule,
    NzIconModule, NzSpinModule, NzTabsModule, NzEmptyModule, NzProgressModule,
    NzBadgeModule, NzTooltipModule, NzSelectModule,
  ],
  styles: [`
    .header { margin-bottom: 20px; }
    .header h2 { margin: 0 0 4px; font-size: 22px; }
    .header p  { margin: 0; color: #8c8c8c; font-size: 13px; }
    .counts-row { display: flex; gap: 12px; margin-bottom: 20px; flex-wrap: wrap; }
    .count-chip {
      display: flex; align-items: center; gap: 6px;
      padding: 6px 14px; border-radius: 20px;
      cursor: pointer; border: 1.5px solid transparent;
      font-size: 13px; font-weight: 500; transition: all .15s;
    }
    .count-chip.active { border-color: #1890ff; background: #e6f7ff; }
    .count-chip:hover:not(.active) { background: #f5f5f5; }
    .overdue { color: #ff4d4f; font-weight: 600; }
    .status-select-cell { min-width: 130px; }
  `],
  template: `
    <div class="header">
      <h2><span nz-icon nzType="check-square" style="color:#1890ff;margin-right:8px"></span>Mis tareas</h2>
      <p>Todas las tareas asignadas a ti, de todos tus proyectos</p>
    </div>

    @if (!data()) {
      <div style="display:flex;justify-content:center;padding:80px"><nz-spin nzSize="large" /></div>
    } @else {

      <!-- Filtros rápidos por estado -->
      <div class="counts-row">
        <div class="count-chip" [class.active]="activeStatus() === null" (click)="setStatus(null)">
          <span nz-icon nzType="appstore"></span> Todas <strong>{{ data()!.counts.total }}</strong>
        </div>
        <div class="count-chip" [class.active]="activeStatus() === 'InProgress'" (click)="setStatus('InProgress')">
          🟡 En progreso <strong>{{ data()!.counts.inProgress }}</strong>
        </div>
        <div class="count-chip" [class.active]="activeStatus() === 'Blocked'" (click)="setStatus('Blocked')">
          🔴 Bloqueadas <strong>{{ data()!.counts.blocked }}</strong>
        </div>
        <div class="count-chip" [class.active]="activeStatus() === 'ToDo'" (click)="setStatus('ToDo')">
          🔵 Por hacer <strong>{{ data()!.counts.toDo }}</strong>
        </div>
        <div class="count-chip" [class.active]="activeStatus() === 'Backlog'" (click)="setStatus('Backlog')">
          ⚪ Backlog <strong>{{ data()!.counts.backlog }}</strong>
        </div>
        <div class="count-chip" [class.active]="activeStatus() === 'Done'" (click)="setStatus('Done')">
          ✅ Hecho <strong>{{ data()!.counts.done }}</strong>
        </div>
      </div>

      <!-- Progreso global -->
      @if (data()!.counts.total > 0) {
        <div style="display:flex;align-items:center;gap:16px;margin-bottom:20px;background:#fff;padding:12px 16px;border-radius:8px;border:1px solid #f0f0f0">
          <span style="font-size:13px;color:#595959;white-space:nowrap">Completado</span>
          <nz-progress style="flex:1" [nzPercent]="donePct()" nzStrokeColor="#52c41a" [nzShowInfo]="true" nzSize="small"></nz-progress>
          <span style="font-size:13px;color:#8c8c8c;white-space:nowrap">{{ data()!.counts.done }}/{{ data()!.counts.total }}</span>
        </div>
      }

      <!-- Tabla -->
      <nz-card [nzBordered]="true" [nzBodyStyle]="{padding:'0'}">
        <nz-table
          [nzData]="data()!.items"
          nzBordered
          nzSize="small"
          [nzShowPagination]="data()!.total > 50"
          [nzTotal]="data()!.total"
          [nzLoading]="loading()">
          <thead>
            <tr>
              <th>Tarea</th>
              <th>Proyecto</th>
              <th>Sprint / Épica</th>
              <th>Estado</th>
              <th>Prioridad</th>
              <th>Fecha límite</th>
              <th style="width:90px">Est.</th>
            </tr>
          </thead>
          <tbody>
            @for (wi of data()!.items; track wi.id) {
              <tr>
                <td>
                  <div style="font-weight:600;font-size:13px">
                    @if (wi.isHito) { <span nz-icon nzType="flag" style="color:#eb2f96;margin-right:4px"></span> }
                    {{ wi.title }}
                  </div>
                </td>
                <td>
                  <a [routerLink]="['/projects', wi.projectId]" style="font-size:13px">{{ wi.projectTitle }}</a>
                </td>
                <td style="font-size:12px">
                  @if (wi.sprintName) {
                    <div>
                      <a [routerLink]="['/projects', wi.projectId, 'sprints', wi.sprintId, 'kanban']"
                        style="color:#722ed1">
                        ⚡ {{ wi.sprintName }}
                      </a>
                    </div>
                  }
                  @if (wi.epicTitle) {
                    <div style="color:#1890ff">📋 {{ wi.epicTitle }}</div>
                  }
                  @if (!wi.sprintName && !wi.epicTitle) { <span style="color:#bfbfbf">—</span> }
                </td>
                <td class="status-select-cell">
                  @if (isTerminal(wi.status)) {
                    <nz-tag [nzColor]="statusColors[wi.status]">{{ statusLabel(wi.status) }}</nz-tag>
                  } @else {
                    <nz-select
                      [ngModel]="wi.status"
                      (ngModelChange)="changeStatus(wi, $event)"
                      nzBorderless
                      nzSize="small"
                      style="width:130px"
                      [nzLoading]="transitioning()[wi.id]">
                      @for (s of selectableStatuses; track s) {
                        <nz-option [nzValue]="s" [nzLabel]="statusLabel(s)" />
                      }
                    </nz-select>
                  }
                </td>
                <td>
                  <nz-tag [nzColor]="priorityColors[wi.priority]">{{ wi.priority }}</nz-tag>
                </td>
                <td>
                  @if (wi.dueDate) {
                    <span [class.overdue]="isOverdue(wi.dueDate)">
                      {{ wi.dueDate }}
                      @if (isOverdue(wi.dueDate)) { <span nz-icon nzType="warning" nz-tooltip nzTooltipTitle="Fecha vencida"></span> }
                    </span>
                  } @else { <span style="color:#bfbfbf">—</span> }
                </td>
                <td style="font-size:12px;color:#8c8c8c">
                  @if (wi.estimationHours) { <span title="Horas">⏱ {{ wi.estimationHours }}h</span> }
                  @if (wi.estimationPoints) { <span title="Story points" style="margin-left:4px">🎯 {{ wi.estimationPoints }}</span> }
                  @if (!wi.estimationHours && !wi.estimationPoints) { <span style="color:#bfbfbf">—</span> }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7">
                  <nz-empty nzNotFoundContent="Sin tareas en este filtro" />
                </td>
              </tr>
            }
          </tbody>
        </nz-table>
      </nz-card>
    }
  `,
})
export class MyTasksComponent {
  private readonly http = inject(HttpClient);
  private readonly workItemsService = inject(WorkItemsService);
  private readonly message = inject(NzMessageService);

  readonly activeStatus = signal<string | null>(null);
  readonly loading = signal(false);
  /** Track per-item transition loading: Record<workItemId, boolean> */
  readonly transitioning = signal<Record<number, boolean>>({});
  private readonly refresh$ = new Subject<string | null>();

  readonly data = toSignal(
    this.refresh$.pipe(
      startWith(null),
      switchMap(status => {
        const url = status
          ? `/api/me/workitems?status=${status}&pageSize=100`
          : `/api/me/workitems?pageSize=100`;
        return this.http.get<MyWorkItemsDto>(url);
      })
    )
  );

  readonly donePct = computed(() => {
    const c = this.data()?.counts;
    if (!c || c.total === 0) return 0;
    return Math.round((c.done / c.total) * 100);
  });

  readonly statusColors = STATUS_COLORS;
  readonly priorityColors = PRIORITY_COLORS;
  readonly selectableStatuses = SELECTABLE_STATUSES;

  setStatus(status: string | null): void {
    this.activeStatus.set(status);
    this.refresh$.next(status);
  }

  statusLabel(s: string): string { return STATUS_LABELS[s] ?? s; }

  isOverdue(dateStr: string): boolean {
    return new Date(dateStr) < new Date();
  }

  isTerminal(status: string): boolean {
    return TERMINAL_STATUSES.includes(status);
  }

  changeStatus(wi: MyWorkItemDto, newStatus: WorkItemStatus): void {
    if (wi.status === newStatus) return;

    this.transitioning.update(t => ({ ...t, [wi.id]: true }));

    this.workItemsService.transitionStatus(wi.projectId, wi.id, newStatus).subscribe({
      next: () => {
        this.transitioning.update(t => { const next = { ...t }; delete next[wi.id]; return next; });
        this.message.success(`Estado → ${STATUS_LABELS[newStatus] ?? newStatus}`);
        // Reload with current filter to refresh counts
        this.refresh$.next(this.activeStatus());
      },
      error: () => {
        this.transitioning.update(t => { const next = { ...t }; delete next[wi.id]; return next; });
        this.message.error('No se pudo cambiar el estado');
      },
    });
  }
}
