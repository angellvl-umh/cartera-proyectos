import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  Output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, Subject, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import {
  CdkDrag,
  CdkDragDrop,
  CdkDragHandle,
  CdkDropList,
  moveItemInArray,
} from '@angular/cdk/drag-drop';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import {
  WorkItem,
  WorkItemPriority,
  WorkItemsService,
  WorkItemStatus,
  WorkItemType,
  WORK_ITEM_TYPE_LABELS,
} from '../workitems.service';
import { Epic } from '../epics.service';
import { Sprint } from '../sprint.service';

export interface BacklogOpenFormEvent {
  workItem?: WorkItem;
}

export interface BacklogOpenCommentsEvent {
  workItem: WorkItem;
}

const STATUS_COLORS: Record<WorkItemStatus, string> = {
  Backlog: 'default',
  ToDo: 'blue',
  InProgress: 'processing',
  Blocked: 'error',
  Done: 'success',
  Discarded: 'default',
};

const STATUS_LABELS: Record<WorkItemStatus, string> = {
  Backlog: 'Backlog',
  ToDo: 'Por hacer',
  InProgress: 'En curso',
  Blocked: 'Bloqueada',
  Done: 'Hecho',
  Discarded: 'Descartada',
};

const PRIORITY_COLORS: Record<WorkItemPriority, string> = {
  Low: 'default',
  Medium: 'blue',
  High: 'orange',
  Critical: 'red',
};

const TERMINAL_STATUSES: WorkItemStatus[] = ['Done', 'Discarded'];

@Component({
  selector: 'app-product-backlog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    CdkDropList, CdkDrag, CdkDragHandle,
    NzTableModule, NzButtonModule, NzInputModule, NzSelectModule,
    NzTagModule, NzIconModule, NzSpinModule, NzPopconfirmModule,
    NzSpaceModule, NzTooltipModule, NzModalModule, NzEmptyModule,
  ],
  styles: [`
    .backlog-drag-handle {
      cursor: grab;
      color: #bfbfbf;
      font-size: 14px;
      padding: 0 4px;
      display: inline-flex;
      align-items: center;
    }
    .backlog-drag-handle:active { cursor: grabbing; }
    .cdk-drag-preview td { background: #fff; }
    .cdk-drag-placeholder { opacity: 0.4; background: #e6f7ff; }
    .cdk-drag-animating { transition: transform 200ms cubic-bezier(0,0,.2,1); }
    .backlog-table tbody tr:not(.cdk-drag-placeholder) { transition: transform 200ms; }
    .quick-add-row td { background: #fafafa; }
    .selection-bar {
      display: flex;
      align-items: center;
      gap: 12px;
      background: #e6f4ff;
      border: 1px solid #91caff;
      border-radius: 6px;
      padding: 8px 12px;
      margin-bottom: 8px;
    }
  `],
  template: `
    <!-- Filtros -->
    <div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin-bottom:12px">
      <nz-input-group [nzPrefix]="searchIcon" style="width:200px">
        <input nz-input placeholder="Buscar tarea..." [ngModel]="filterText()"
          (ngModelChange)="onFilterTextChange($event)" />
      </nz-input-group>
      <ng-template #searchIcon><span nz-icon nzType="search"></span></ng-template>

      <nz-select [(ngModel)]="filterEpicId" (ngModelChange)="onFilterChange()"
        nzAllowClear nzPlaceHolder="Épica" style="width:160px">
        @for (epic of epics; track epic.id) {
          <nz-option [nzValue]="epic.id" [nzLabel]="epic.title" />
        }
      </nz-select>

      <nz-select [(ngModel)]="filterPriority" (ngModelChange)="onFilterChange()"
        nzAllowClear nzPlaceHolder="Prioridad" style="width:130px">
        <nz-option nzValue="Low" nzLabel="Baja" />
        <nz-option nzValue="Medium" nzLabel="Media" />
        <nz-option nzValue="High" nzLabel="Alta" />
        <nz-option nzValue="Critical" nzLabel="Crítica" />
      </nz-select>

      <nz-select [(ngModel)]="filterType" (ngModelChange)="onFilterChange()"
        nzAllowClear nzPlaceHolder="Tipo" style="width:160px">
        <nz-option nzValue="Task" nzLabel="Tarea" />
        <nz-option nzValue="UserStory" nzLabel="Historia de usuario" />
      </nz-select>

      <nz-select [(ngModel)]="filterAssigneeId" (ngModelChange)="onFilterChange()"
        nzAllowClear nzPlaceHolder="Asignado" style="width:160px">
        @for (p of persons; track p.id) {
          <nz-option [nzValue]="p.id" [nzLabel]="p.name" />
        }
      </nz-select>

      @if (hasActiveFilters()) {
        <button nz-button nzSize="small" (click)="clearFilters()">
          <span nz-icon nzType="close"></span> Limpiar
        </button>
      }

      <span style="margin-left:auto;font-size:13px;color:#8c8c8c">
        {{ total() }} tarea{{ total() !== 1 ? 's' : '' }}
      </span>
    </div>

    <!-- Barra de acciones masivas -->
    @if (selectedIds().size > 0) {
      <div class="selection-bar">
        <span style="font-weight:600;font-size:13px">{{ selectedIds().size }} seleccionada{{ selectedIds().size !== 1 ? 's' : '' }}</span>
        <button nz-button nzSize="small" nzType="primary" (click)="openBulkSprintModal()">
          <span nz-icon nzType="link"></span> Mover a sprint…
        </button>
        <button nz-button nzSize="small" nzDanger
          nz-popconfirm nzPopconfirmTitle="¿Quitar de sprint las tareas seleccionadas?"
          (nzOnConfirm)="bulkRemoveFromSprint()">
          <span nz-icon nzType="disconnect"></span> Quitar de sprint
        </button>
        <button nz-button nzSize="small" (click)="clearSelection()">
          <span nz-icon nzType="close"></span> Cancelar selección
        </button>
      </div>
    }

    <!-- Tabla con CDK Drop List -->
    <div cdkDropList (cdkDropListDropped)="onDrop($event)" [cdkDropListDisabled]="hasActiveFilters()">
      <nz-table
        class="backlog-table"
        [nzData]="items()"
        [nzLoading]="loading()"
        [nzTotal]="total()"
        [nzPageIndex]="page()"
        [nzPageSize]="pageSize()"
        [nzFrontPagination]="false"
        (nzPageIndexChange)="onPageChange($event)"
        (nzPageSizeChange)="onPageSizeChange($event)"
        nzBordered
        nzSize="small"
        [nzShowSizeChanger]="true"
        [nzPageSizeOptions]="[10, 25, 50]"
      >
        <thead>
          <tr>
            <th nzWidth="32px"></th>
            <th nzWidth="40px" style="text-align:center">#</th>
            <th nzWidth="36px">
              <input type="checkbox" [checked]="allSelected()" [indeterminate]="someSelected()"
                (change)="toggleSelectAll($event)" />
            </th>
            <th>Título</th>
            <th nzWidth="120px">Épica</th>
            <th nzWidth="90px">Tipo</th>
            <th nzWidth="100px">Estado</th>
            <th nzWidth="90px">Prioridad</th>
            <th nzWidth="140px">Asignados</th>
            <th nzWidth="60px" style="text-align:center">Pts</th>
            <th nzWidth="100px">Fecha límite</th>
            <th nzWidth="180px">Acciones</th>
          </tr>
        </thead>
        <tbody>
          @for (wi of items(); track wi.id; let i = $index) {
            <tr cdkDrag [cdkDragData]="wi">
              <!-- Drag handle -->
              <td style="padding:0 4px;text-align:center">
                @if (!hasActiveFilters()) {
                  <span cdkDragHandle class="backlog-drag-handle" title="Arrastrar para reordenar">
                    ⠿
                  </span>
                } @else {
                  <span nz-tooltip nzTooltipTitle="Quita los filtros para reordenar"
                    style="color:#d9d9d9;font-size:14px;padding:0 4px;cursor:not-allowed">⠿</span>
                }
              </td>
              <!-- # posición -->
              <td style="text-align:center;color:#8c8c8c;font-size:12px">
                {{ (page() - 1) * pageSize() + i + 1 }}
              </td>
              <!-- Checkbox -->
              <td style="text-align:center">
                <input type="checkbox" [checked]="selectedIds().has(wi.id)"
                  (change)="toggleSelect(wi.id, $event)" />
              </td>
              <td style="font-weight:500">{{ wi.title }}</td>
              <td style="font-size:12px;color:#595959">{{ wi.epicTitle ?? '—' }}</td>
              <td style="font-size:12px">{{ workItemTypeLabel(wi.type) }}</td>
              <td><nz-tag [nzColor]="statusColor(wi.status)">{{ statusLabel(wi.status) }}</nz-tag></td>
              <td><nz-tag [nzColor]="priorityColor(wi.priority)">{{ wi.priority }}</nz-tag></td>
              <td style="font-size:12px">
                {{ wi.assignees.length > 0 ? wi.assignees.map(a => a.name).join(', ') : '—' }}
              </td>
              <td style="text-align:center;font-size:12px">{{ wi.estimationPoints ?? '—' }}</td>
              <td style="font-size:12px"
                [style.color]="isOverdue(wi) ? '#f5222d' : 'inherit'"
                [style.fontWeight]="isOverdue(wi) ? '600' : 'normal'">
                {{ wi.dueDate ?? '—' }}
              </td>
              <td>
                <nz-space nzSize="small">
                  <button *nzSpaceItem nz-button nzSize="small" (click)="openDrawer.emit(wi)"
                    title="Ver detalle">
                    <span nz-icon nzType="eye"></span>
                  </button>
                  <button *nzSpaceItem nz-button nzSize="small" (click)="openAssignSprint.emit(wi)"
                    title="Asignar sprint">
                    <span nz-icon nzType="link"></span>
                  </button>
                  <button *nzSpaceItem nz-button nzSize="small" (click)="openComments.emit(wi)"
                    title="Comentarios">
                    <span nz-icon nzType="comment"></span>
                  </button>
                  <button *nzSpaceItem nz-button nzSize="small" (click)="openForm.emit(wi)"
                    title="Editar">
                    <span nz-icon nzType="edit"></span>
                  </button>
                  @if (wi.status !== 'Done' && wi.status !== 'Discarded') {
                    <button *nzSpaceItem nz-button nzSize="small" nzDanger nz-popconfirm
                      nzPopconfirmTitle="¿Descartar esta tarea? Es irreversible."
                      (nzOnConfirm)="discardItem(wi)" title="Descartar">
                      <span nz-icon nzType="stop"></span>
                    </button>
                  }
                  <button *nzSpaceItem nz-button nzSize="small" nzDanger nz-popconfirm
                    nzPopconfirmTitle="¿Eliminar tarea?" (nzOnConfirm)="deleteItem(wi)" title="Eliminar">
                    <span nz-icon nzType="delete"></span>
                  </button>
                </nz-space>
              </td>
            </tr>
          } @empty {
            @if (!loading()) {
              <tr>
                <td colspan="12" style="text-align:center;padding:32px">
                  <nz-empty nzNotFoundContent="Sin tareas en el backlog" />
                </td>
              </tr>
            }
          }

          <!-- Fila de alta rápida -->
          <tr class="quick-add-row">
            <td colspan="3"></td>
            <td>
              <input nz-input nzSize="small" [(ngModel)]="quickAddTitle"
                placeholder="Añadir tarea de alto nivel…"
                style="width:100%"
                [disabled]="quickAdding()"
                (keydown.enter)="quickAdd()" />
            </td>
            <td colspan="7"></td>
            <td>
              <nz-space nzSize="small">
                <nz-select *nzSpaceItem [(ngModel)]="quickAddType" nzSize="small" style="width:110px">
                  <nz-option nzValue="Task" nzLabel="Tarea" />
                  <nz-option nzValue="UserStory" nzLabel="Historia" />
                </nz-select>
                <button *nzSpaceItem nz-button nzSize="small" nzType="primary"
                  [nzLoading]="quickAdding()"
                  [disabled]="!quickAddTitle.trim()"
                  (click)="quickAdd()">
                  <span nz-icon nzType="plus"></span> Añadir
                </button>
              </nz-space>
            </td>
          </tr>
        </tbody>
      </nz-table>
    </div>

    <!-- Modal asignación masiva a sprint -->
    <nz-modal
      [nzVisible]="bulkSprintModalVisible()"
      nzTitle="Mover selección a sprint"
      (nzOnCancel)="bulkSprintModalVisible.set(false)"
      (nzOnOk)="confirmBulkAssignSprint()"
      [nzOkLoading]="bulkLoading()"
    >
      <ng-container *nzModalContent>
        <nz-select [(ngModel)]="bulkTargetSprintId"
          nzPlaceHolder="Selecciona un sprint" style="width:100%">
          @for (s of activeOrPlanningSprintsForBulk(); track s.id) {
            <nz-option [nzValue]="s.id" [nzLabel]="s.name + ' (' + s.status + ')'" />
          }
        </nz-select>
        @if (activeOrPlanningSprintsForBulk().length === 0) {
          <p style="color:#8c8c8c;margin-top:8px;font-size:13px">
            No hay sprints en Planning o Active en este proyecto.
          </p>
        }
      </ng-container>
    </nz-modal>
  `,
})
export class ProductBacklogComponent implements OnChanges {
  @Input({ required: true }) projectId!: number;
  @Input() epics: Epic[] = [];
  @Input() sprints: Sprint[] = [];
  @Input() persons: { id: number; name: string }[] = [];

  @Output() openForm = new EventEmitter<WorkItem | undefined>();
  @Output() openComments = new EventEmitter<WorkItem>();
  @Output() openAssignSprint = new EventEmitter<WorkItem>();
  @Output() openDrawer = new EventEmitter<WorkItem>();
  @Output() refreshRequested = new EventEmitter<void>();

  private readonly workItemsService = inject(WorkItemsService);
  private readonly message = inject(NzMessageService);

  // ── State ─────────────────────────────────────────────────────────────────
  items = signal<WorkItem[]>([]);
  loading = signal(false);
  total = signal(0);
  page = signal(1);
  pageSize = signal(25);

  // ── Filters ───────────────────────────────────────────────────────────────
  filterText = signal('');
  filterEpicId: number | null = null;
  filterPriority: WorkItemPriority | null = null;
  filterType: WorkItemType | null = null;
  filterAssigneeId: number | null = null;

  private readonly searchSubject = new Subject<string>();

  // ── Selection ─────────────────────────────────────────────────────────────
  selectedIds = signal<Set<number>>(new Set());

  allSelected = computed(() => {
    const ids = this.selectedIds();
    const it = this.items();
    return it.length > 0 && it.every(w => ids.has(w.id));
  });

  someSelected = computed(() => {
    const ids = this.selectedIds();
    const it = this.items();
    return it.some(w => ids.has(w.id)) && !this.allSelected();
  });

  // ── Bulk sprint ───────────────────────────────────────────────────────────
  bulkSprintModalVisible = signal(false);
  bulkTargetSprintId: number | null = null;
  bulkLoading = signal(false);

  activeOrPlanningSprintsForBulk = computed(() =>
    this.sprints.filter(s => s.status === 'Planning' || s.status === 'Active')
  );

  // ── Quick add ─────────────────────────────────────────────────────────────
  quickAddTitle = '';
  quickAddType: WorkItemType = 'Task';
  quickAdding = signal(false);

  hasActiveFilters = computed(() =>
    !!this.filterText() || this.filterEpicId != null || this.filterPriority != null ||
    this.filterType != null || this.filterAssigneeId != null
  );

  constructor() {
    // Debounce text search
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {
      this.page.set(1);
      this.loadItems();
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['projectId'] && this.projectId) {
      this.loadItems();
    }
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadItems(): void {
    this.loading.set(true);
    this.workItemsService.searchWorkItems(this.projectId, {
      backlogOnly: true,
      page: this.page(),
      pageSize: this.pageSize(),
      q: this.filterText() || undefined,
      epicId: this.filterEpicId,
      priority: this.filterPriority,
      type: this.filterType,
      assigneeId: this.filterAssigneeId,
    }).subscribe({
      next: result => {
        this.items.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.message.error('Error al cargar el backlog');
      },
    });
  }

  // ── Filter handlers ───────────────────────────────────────────────────────
  onFilterTextChange(value: string): void {
    this.filterText.set(value);
    this.searchSubject.next(value);
  }

  onFilterChange(): void {
    this.page.set(1);
    this.loadItems();
  }

  clearFilters(): void {
    this.filterText.set('');
    this.filterEpicId = null;
    this.filterPriority = null;
    this.filterType = null;
    this.filterAssigneeId = null;
    this.page.set(1);
    this.loadItems();
  }

  // ── Pagination ────────────────────────────────────────────────────────────
  onPageChange(p: number): void {
    this.page.set(p);
    this.clearSelection();
    this.loadItems();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.clearSelection();
    this.loadItems();
  }

  // ── Selection ─────────────────────────────────────────────────────────────
  toggleSelect(id: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedIds.update(set => {
      const next = new Set(set);
      if (checked) next.add(id); else next.delete(id);
      return next;
    });
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedIds.set(checked ? new Set(this.items().map(w => w.id)) : new Set());
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  // ── Drag & drop reorder ───────────────────────────────────────────────────
  onDrop(event: CdkDragDrop<WorkItem[]>): void {
    if (event.previousIndex === event.currentIndex) return;

    const list = [...this.items()];
    moveItemInArray(list, event.previousIndex, event.currentIndex);
    this.items.set(list); // optimistic update

    this.workItemsService.reorder(this.projectId, list.map(w => w.id)).subscribe({
      error: () => {
        this.message.error('Error al reordenar. Recargando…');
        this.loadItems();
      },
    });
  }

  // ── Bulk actions ──────────────────────────────────────────────────────────
  openBulkSprintModal(): void {
    this.bulkTargetSprintId = null;
    this.bulkSprintModalVisible.set(true);
  }

  confirmBulkAssignSprint(): void {
    if (!this.bulkTargetSprintId) {
      this.message.warning('Selecciona un sprint destino');
      return;
    }
    this.bulkLoading.set(true);
    this.workItemsService.bulkAssignSprint(
      this.projectId, [...this.selectedIds()], this.bulkTargetSprintId
    ).subscribe({
      next: () => {
        this.bulkLoading.set(false);
        this.bulkSprintModalVisible.set(false);
        this.message.success('Tareas asignadas al sprint');
        this.clearSelection();
        this.loadItems();
      },
      error: () => {
        this.bulkLoading.set(false);
        this.message.error('Error al asignar al sprint');
      },
    });
  }

  bulkRemoveFromSprint(): void {
    this.workItemsService.bulkAssignSprint(
      this.projectId, [...this.selectedIds()], null
    ).subscribe({
      next: () => {
        this.message.success('Tareas quitadas del sprint');
        this.clearSelection();
        this.loadItems();
      },
      error: () => this.message.error('Error al quitar del sprint'),
    });
  }

  // ── Item actions ──────────────────────────────────────────────────────────
  discardItem(wi: WorkItem): void {
    this.workItemsService.transitionStatus(this.projectId, wi.id, 'Discarded').subscribe({
      next: () => { this.message.success('Tarea descartada'); this.loadItems(); },
      error: () => this.message.error('Error al descartar la tarea'),
    });
  }

  deleteItem(wi: WorkItem): void {
    this.workItemsService.deleteWorkItem(this.projectId, wi.id).subscribe({
      next: () => { this.message.success('Tarea eliminada'); this.loadItems(); },
      error: () => this.message.error('Error al eliminar la tarea'),
    });
  }

  // ── Quick add ─────────────────────────────────────────────────────────────
  // this.items() solo contiene la página actualmente cargada, así que el
  // sortOrder de la nueva tarea no puede calcularse a partir de ella (el bug
  // original: "max(página actual)+10" podía caer detrás de tareas de otras
  // páginas, o incluso quedar por delante de ellas, según en qué página
  // estuviera el usuario). En su lugar se pide al backend el sortOrder real
  // más alto del backlog completo, y tras crear la tarea se navega a la
  // página donde ha quedado, para que sea visible sin que el usuario tenga
  // que buscarla.
  quickAdd(): void {
    const title = this.quickAddTitle.trim();
    if (!title) return;

    this.quickAdding.set(true);

    this.fetchMaxBacklogSortOrder().pipe(
      switchMap(([maxSortOrder, total]) => this.workItemsService.createWorkItem(this.projectId, {
        title,
        type: this.quickAddType,
        priority: 'Medium',
        sortOrder: maxSortOrder + 10,
        isHito: false,
        assigneeIds: [],
      }).pipe(map(() => total))),
    ).subscribe({
      next: total => {
        this.quickAddTitle = '';
        this.quickAdding.set(false);
        this.message.success('Tarea añadida');
        this.page.set(Math.max(1, Math.ceil((total + 1) / this.pageSize())));
        this.loadItems();
        this.refreshRequested.emit();
      },
      error: () => {
        this.quickAdding.set(false);
        this.message.error('Error al crear la tarea');
      },
    });
  }

  // sortOrder de la última tarea del backlog completo (ignorando filtros de UI) + su total.
  private fetchMaxBacklogSortOrder(): Observable<[number, number]> {
    return this.workItemsService.searchWorkItems(this.projectId, { backlogOnly: true, page: 1, pageSize: 1 }).pipe(
      switchMap(first => {
        if (first.total <= 1) return of<[number, number]>([first.items[0]?.sortOrder ?? 0, first.total]);
        return this.workItemsService.searchWorkItems(this.projectId, { backlogOnly: true, page: first.total, pageSize: 1 })
          .pipe(map(last => [last.items[0]?.sortOrder ?? 0, first.total] as [number, number]));
      }),
    );
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  statusColor(s: WorkItemStatus): string { return STATUS_COLORS[s]; }
  statusLabel(s: WorkItemStatus): string { return STATUS_LABELS[s]; }
  priorityColor(p: WorkItemPriority): string { return PRIORITY_COLORS[p]; }
  workItemTypeLabel(t: WorkItemType): string { return WORK_ITEM_TYPE_LABELS[t]; }

  isOverdue(wi: WorkItem): boolean {
    if (!wi.dueDate) return false;
    if (TERMINAL_STATUSES.includes(wi.status)) return false;
    return new Date(wi.dueDate) < new Date();
  }
}
