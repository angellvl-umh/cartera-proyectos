import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, startWith, switchMap, of } from 'rxjs';
import { FormsModule } from '@angular/forms';
import {
  CdkDropListGroup,
  CdkDropList,
  CdkDrag,
  CdkDragPlaceholder,
  CdkDragDrop,
} from '@angular/cdk/drag-drop';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzMessageService } from 'ng-zorro-antd/message';
import { WorkItemsService, WorkItem, WorkItemStatus, WorkItemType, WORK_ITEM_TYPE_LABELS } from '../workitems.service';
import { EpicsService } from '../epics.service';
import { SprintService } from '../sprint.service';

interface KanbanColumn {
  status: WorkItemStatus;
  label: string;
  items: WorkItem[];
}

const COLUMNS_DEF: { status: WorkItemStatus; label: string }[] = [
  { status: 'Backlog',    label: 'Backlog' },
  { status: 'ToDo',       label: 'Por hacer' },
  { status: 'InProgress', label: 'En progreso' },
  { status: 'Blocked',    label: 'Bloqueada' },
  { status: 'Done',       label: 'Hecha' },
];

const PRIORITY_COLORS: Record<string, string> = {
  Low:      '#52c41a',
  Medium:   '#faad14',
  High:     '#fa8c16',
  Critical: '#f5222d',
};

const ASSIGNEE_COLORS = [
  '#1890ff', '#13c2c2', '#52c41a', '#722ed1', '#eb2f96', '#fa8c16',
];

function assigneeColor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
  return ASSIGNEE_COLORS[Math.abs(hash) % ASSIGNEE_COLORS.length];
}

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, RouterLink,
    CdkDropListGroup, CdkDropList, CdkDrag, CdkDragPlaceholder,
    NzInputModule, NzSelectModule, NzButtonModule, NzTagModule,
    NzIconModule, NzSpinModule,
  ],
  styles: [`
    .kanban-wrapper {
      display: flex;
      flex-direction: column;
      height: calc(100vh - 130px);
    }
    .board-scroll {
      display: flex;
      gap: 16px;
      overflow-x: auto;
      align-items: flex-start;
      flex: 1;
      padding-bottom: 16px;
    }
    .kanban-col {
      min-width: 252px;
      width: 252px;
      flex-shrink: 0;
    }
    .col-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
      padding: 0 2px;
    }
    .col-label {
      font-weight: 700;
      text-transform: uppercase;
      font-size: 11px;
      letter-spacing: .5px;
      color: #595959;
    }
    .col-count {
      background: #f0f0f0;
      border-radius: 10px;
      padding: 1px 8px;
      font-size: 12px;
      color: #595959;
      font-weight: 500;
    }
    .drop-zone {
      min-height: 80px;
      border-radius: 8px;
      transition: background .2s;
      max-height: calc(100vh - 230px);
      overflow-y: auto;
      padding: 2px;
    }
    .drop-zone-empty {
      background: #fafafa;
      border: 1.5px dashed #e8e8e8;
    }
    .kanban-card {
      background: #fff;
      border-radius: 8px;
      padding: 12px 12px 15px;
      margin-bottom: 8px;
      box-shadow: 0 1px 4px rgba(0,0,0,.07);
      cursor: grab;
      position: relative;
      overflow: hidden;
      border: 1px solid #f0f0f0;
      transition: box-shadow .15s;
    }
    .kanban-card:hover {
      box-shadow: 0 3px 10px rgba(0,0,0,.12);
    }
    .kanban-card:active { cursor: grabbing; }
    .card-title {
      font-weight: 600;
      font-size: 13px;
      line-height: 1.4;
      margin-bottom: 8px;
      color: #262626;
    }
    .card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 8px;
    }
    .assignee-pill {
      color: #fff;
      border-radius: 10px;
      padding: 1px 9px;
      font-size: 11px;
      font-weight: 500;
      white-space: nowrap;
      overflow: hidden;
      max-width: 130px;
      text-overflow: ellipsis;
    }
    .priority-bar {
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      height: 3px;
    }
    .drag-placeholder {
      height: 76px;
      background: #e6f7ff;
      border: 1.5px dashed #91d5ff;
      border-radius: 8px;
      margin-bottom: 8px;
    }
    .cdk-drag-preview {
      box-shadow: 0 6px 20px rgba(0,0,0,.18);
      border-radius: 8px;
      background: #fff;
    }
    .cdk-drag-animating { transition: transform 200ms cubic-bezier(0,0,.2,1); }
    .drop-zone.cdk-drop-list-dragging .kanban-card:not(.cdk-drag-placeholder) {
      transition: transform 200ms cubic-bezier(0,0,.2,1);
    }
  `],
  template: `
    <div class="kanban-wrapper">

      <!-- Header -->
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
        <div>
          <a [routerLink]="['/projects', projectId]"
            style="color:#595959;font-size:13px;display:inline-flex;align-items:center;gap:4px;margin-bottom:4px;text-decoration:none">
            <span nz-icon nzType="arrow-left"></span> Volver al proyecto
          </a>
          <div style="display:flex;align-items:baseline;gap:12px">
            <h2 style="margin:0;font-size:20px">
              @if (sprintId > 0) {
                Tablero Kanban
                @if (sprint()) {
                  <span style="font-weight:400;color:#595959"> — {{ sprint()!.name }}</span>
                }
              } @else {
                Kanban del proyecto
              }
            </h2>
            @if (totalItems() !== null) {
              <span style="color:#8c8c8c;font-size:13px">{{ totalItems() }} tareas</span>
            }
          </div>
        </div>
      </div>

      <!-- Filters -->
      <div style="display:flex;gap:12px;align-items:center;margin-bottom:20px;flex-wrap:wrap">
        <nz-input-group [nzPrefix]="searchTpl" style="max-width:220px">
          <input nz-input placeholder="Buscar tarea..."
            [ngModel]="filterText()" (ngModelChange)="filterText.set($event)" />
        </nz-input-group>
        <ng-template #searchTpl><span nz-icon nzType="search"></span></ng-template>

        <nz-select
          [ngModel]="filterEpicId()"
          (ngModelChange)="filterEpicId.set($event)"
          nzAllowClear
          nzPlaceHolder="Todas las épicas"
          style="min-width:200px">
          @for (epic of epics()?.items ?? []; track epic.id) {
            <nz-option [nzValue]="epic.id" [nzLabel]="epic.title" />
          }
        </nz-select>

        @if (isFiltered()) {
          <button nz-button (click)="clearFilters()">Limpiar filtros</button>
        }
      </div>

      <!-- Board -->
      @if (!rawItems()) {
        <div style="display:flex;justify-content:center;padding:64px">
          <nz-spin nzSize="large" />
        </div>
      } @else {
        <div class="board-scroll" cdkDropListGroup>
          @for (col of columns(); track col.status) {
            <div class="kanban-col">

              <!-- Column header -->
              <div class="col-header">
                <span class="col-label">{{ col.label }}</span>
                <span class="col-count">{{ col.items.length }}</span>
              </div>

              <!-- Drop zone -->
              <div
                cdkDropList
                [id]="col.status"
                [cdkDropListData]="col.status"
                [cdkDropListConnectedTo]="connectedListIds"
                (cdkDropListDropped)="drop($event)"
                class="drop-zone"
                [class.drop-zone-empty]="col.items.length === 0"
              >
                @for (wi of col.items; track wi.id) {
                  <div
                    cdkDrag
                    [cdkDragData]="wi"
                    class="kanban-card"
                  >
                    <!-- Title -->
                    <div style="display:flex;align-items:center;gap:4px;margin-bottom:2px">
                      <span nz-icon [nzType]="wi.type === 'UserStory' ? 'read' : 'file-text'" style="font-size:11px;color:#8c8c8c" [title]="typeLabel(wi.type)"></span>
                    </div>
                    <div class="card-title">{{ wi.title }}</div>

                    <!-- Epic tag -->
                    @if (wi.epicTitle) {
                      <nz-tag nzColor="blue" style="font-size:11px;border-radius:10px;line-height:18px;margin-bottom:4px">
                        {{ wi.epicTitle }}
                      </nz-tag>
                    }

                    <!-- Assignees -->
                    @if (wi.assignees.length > 0) {
                      <div style="display:flex;flex-wrap:wrap;gap:4px;margin-bottom:4px">
                        @for (a of wi.assignees; track a.id) {
                          <span class="assignee-pill" [style.background]="avatarColor(a.name)" [title]="a.name">
                            {{ a.name }}
                          </span>
                        }
                      </div>
                    }

                    <!-- Meta: estimation + dueDate + hito -->
                    <div class="card-footer">
                      <div style="display:flex;align-items:center;gap:8px">
                        @if (wi.estimationHours) {
                          <span style="font-size:11px;color:#8c8c8c" title="Estimación horas">
                            ⏱ {{ wi.estimationHours }}h
                          </span>
                        }
                        @if (wi.estimationPoints) {
                          <span style="font-size:11px;color:#8c8c8c" title="Story points">
                            🎯 {{ wi.estimationPoints }}pts
                          </span>
                        }
                        @if (wi.dueDate) {
                          <span style="font-size:11px;color:#8c8c8c" [style.color]="isOverdue(wi.dueDate) ? '#f5222d' : '#8c8c8c'" title="Fecha fin">
                            📅 {{ wi.dueDate }}
                          </span>
                        }
                      </div>
                      @if (wi.isHito) {
                        <span nz-icon nzType="flag" style="color:#eb2f96;font-size:13px" title="Hito"></span>
                      }
                    </div>

                    <!-- Priority bar -->
                    <div class="priority-bar" [style.background]="priorityColor(wi.priority)"></div>

                    <!-- Drag placeholder -->
                    <div *cdkDragPlaceholder class="drag-placeholder"></div>
                  </div>
                }
              </div>

            </div>
          }
        </div>
      }

    </div>
  `,
})
export class KanbanBoardComponent {
  private readonly workItemsService = inject(WorkItemsService);
  private readonly epicsService = inject(EpicsService);
  private readonly sprintService = inject(SprintService);
  private readonly message = inject(NzMessageService);

  readonly projectId = +inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly sprintId = +(inject(ActivatedRoute).snapshot.paramMap.get('sprintId') ?? 0);

  readonly sprint = toSignal(
    this.sprintId > 0
      ? this.sprintService.getSprintById(this.projectId, this.sprintId)
      : of(null)
  );

  private readonly refresh$ = new Subject<void>();

  private readonly statusOverrides = signal<Record<number, WorkItemStatus>>({});

  readonly filterText = signal('');
  readonly filterEpicId = signal<number | null>(null);

  readonly rawItems = toSignal(
    this.refresh$.pipe(
      startWith(null),
      switchMap(() => this.workItemsService.getWorkItems(this.projectId, this.sprintId)),
    )
  );

  readonly epics = toSignal(this.epicsService.getEpics(this.projectId));

  private readonly effectiveItems = computed(() => {
    const raw = this.rawItems();
    if (!raw) return null;
    const overrides = this.statusOverrides();
    return raw.items
      .filter(i => (overrides[i.id] ?? i.status) !== 'Discarded')
      .map(i => overrides[i.id] ? { ...i, status: overrides[i.id] } : i);
  });

  readonly columns = computed<KanbanColumn[]>(() => {
    const items = this.effectiveItems();
    if (!items) return COLUMNS_DEF.map(c => ({ ...c, items: [] }));

    const text = this.filterText().toLowerCase().trim();
    const epicId = this.filterEpicId();

    let filtered = items;
    if (text) filtered = filtered.filter(i => i.title.toLowerCase().includes(text));
    if (epicId !== null) filtered = filtered.filter(i => i.epicId === epicId);

    return COLUMNS_DEF.map(col => ({
      ...col,
      items: filtered.filter(i => i.status === col.status),
    }));
  });

  readonly totalItems = computed(() => this.effectiveItems()?.length ?? null);
  readonly isFiltered = computed(() => !!this.filterText() || this.filterEpicId() !== null);

  readonly connectedListIds = COLUMNS_DEF.map(c => c.status);

  clearFilters(): void {
    this.filterText.set('');
    this.filterEpicId.set(null);
  }

  priorityColor(priority: string): string {
    return PRIORITY_COLORS[priority] ?? '#d9d9d9';
  }

  avatarColor(name: string): string {
    return assigneeColor(name);
  }

  isOverdue(dueDate: string): boolean {
    return new Date(dueDate) < new Date();
  }

  typeLabel(t: WorkItemType): string { return WORK_ITEM_TYPE_LABELS[t]; }

  drop(event: CdkDragDrop<WorkItemStatus>): void {
    const wi = event.item.data as WorkItem;
    const newStatus = event.container.id as WorkItemStatus;

    if (wi.status === newStatus) return;

    this.statusOverrides.update(map => ({ ...map, [wi.id]: newStatus }));

    this.workItemsService.transitionStatus(this.projectId, wi.id, newStatus).subscribe({
      next: () => {
        this.refresh$.next();
        this.statusOverrides.update(map => { const n = { ...map }; delete n[wi.id]; return n; });
      },
      error: () => {
        this.statusOverrides.update(map => { const n = { ...map }; delete n[wi.id]; return n; });
        this.message.error('No se pudo cambiar el estado');
      },
    });
  }
}
