import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NzTableModule, NzTableSortOrder } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';
import {
  catchError,
  debounceTime,
  EMPTY,
  Subject,
  Subscription,
  switchMap,
} from 'rxjs';
import { ProjectsService } from '../projects.service';
import {
  PROJECT_COMPLEXITY_LABELS,
  PROJECT_STATUS_LABELS,
  Project,
  ProjectComplexity,
  ProjectDetail,
  ProjectFilters,
  ProjectStatus,
  PromoterDto,
  TagDto,
} from '../project.model';
import { ProjectStatusBadgeComponent } from '../project-status-badge/project-status-badge.component';
import { ProjectFormComponent } from '../project-form/project-form.component';
import { ComplexityIndicatorComponent } from '../complexity-indicator/complexity-indicator.component';
import { KanbanByStatusComponent } from '../kanban-by-status/kanban-by-status.component';
import {
  DEFAULT_PAGE,
  DEFAULT_PAGE_SIZE,
  parseProjectsListParams,
  serializeProjectsListParams,
  SortDir,
  SortField,
} from './projects-list-query';

type SortOrderMap = Partial<Record<SortField, NzTableSortOrder>>;

/** Current UI state, kept in sync with the URL. */
interface ListState {
  q: string;
  status: ProjectStatus | null;
  complexity: ProjectComplexity | null;
  tagIds: number[];
  promoterId: number | null;
  page: number;
  pageSize: number;
  sortBy: SortField | null;
  sortDir: SortDir;
  viewMode: 'tabla' | 'tablero';
}

@Component({
  selector: 'app-projects-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzSelectModule,
    NzPopconfirmModule,
    NzSpaceModule,
    NzIconModule,
    NzSpinModule,
    NzTagModule,
    ProjectStatusBadgeComponent,
    ProjectFormComponent,
    ComplexityIndicatorComponent,
    KanbanByStatusComponent,
  ],
  styles: [`
    .eyebrow { font-size: 12px; font-weight: 700; letter-spacing: 1.2px; text-transform: uppercase; color: var(--brand-primary); margin: 0 0 4px; }
    h1.page-title { font-size: 28px; font-weight: 800; letter-spacing: -0.4px; margin: 0; color: var(--ink); }
    .page-subtitle { font-size: 13px; color: var(--text-muted); margin: 4px 0 0; }
    .toolbar { display: flex; gap: 12px; margin-bottom: 16px; flex-wrap: wrap; align-items: center; }
    .view-toggle { margin-left: auto; display: flex; gap: 3px; background: #EAE6DF; border-radius: 9px; padding: 3px; }
    .view-toggle button {
      border: none; background: transparent; border-radius: 7px; padding: 6px 14px;
      font-size: 13px; font-weight: 600; color: #6B6661; cursor: pointer;
    }
    .view-toggle button.active { background: var(--ink); color: #fff; }
  `],
  template: `
    <div>
      <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:16px;flex-wrap:wrap;gap:12px">
        <div>
          <p class="eyebrow">Cartera de Proyectos TIC</p>
          <h1 class="page-title">Cartera de Proyectos</h1>
          <p class="page-subtitle">{{ projects().length }} de {{ total() }} proyectos</p>
        </div>
        <button nz-button nzType="primary" (click)="openCreate()">
          <span nz-icon nzType="plus"></span>
          Nuevo proyecto
        </button>
      </div>

      <!-- Filtros -->
      <div class="toolbar">
        <!-- inputQ es local: el usuario escribe libremente sin que la URL lo
             reescriba en cada pulsación; el valor se propaga con debounce. -->
        <input
          nz-input
          placeholder="Buscar por título..."
          [ngModel]="inputQ()"
          (ngModelChange)="onQChange($event)"
          style="width: 240px"
        />
        <nz-select
          [ngModel]="filterStatus()"
          (ngModelChange)="onFilterChange('status', $event)"
          nzPlaceHolder="Estado"
          nzAllowClear
          style="width: 180px"
        >
          @for (opt of statusOptions; track opt.value) {
            <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
          }
        </nz-select>
        <nz-select
          [ngModel]="filterComplexity()"
          (ngModelChange)="onFilterChange('complexity', $event)"
          nzPlaceHolder="Complejidad"
          nzAllowClear
          style="width: 160px"
        >
          @for (opt of complexityOptions; track opt.value) {
            <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
          }
        </nz-select>
        <nz-select
          [ngModel]="filterTagIds()"
          (ngModelChange)="onFilterChange('tagIds', $event)"
          nzMode="multiple"
          nzPlaceHolder="Etiquetas"
          nzAllowClear
          nzShowSearch
          [nzMaxTagCount]="2"
          style="width: 240px"
        >
          @for (t of allTags(); track t.id) {
            <nz-option [nzValue]="t.id" [nzLabel]="t.name" />
          }
        </nz-select>
        <nz-select
          [ngModel]="filterPromoterId()"
          (ngModelChange)="onFilterChange('promoterId', $event)"
          nzPlaceHolder="Promotor"
          nzAllowClear
          nzShowSearch
          style="width: 200px"
        >
          @for (p of allPromoters(); track p.id) {
            <nz-option [nzValue]="p.id" [nzLabel]="p.name" />
          }
        </nz-select>

        <div class="view-toggle">
          <button type="button" [class.active]="viewMode() === 'tabla'" (click)="onViewChange('tabla')">Tabla</button>
          <button type="button" [class.active]="viewMode() === 'tablero'" (click)="onViewChange('tablero')">Tablero</button>
        </div>
      </div>

      @if (viewMode() === 'tabla') {
        <!-- Tabla -->
        <nz-table
          [nzData]="projects()"
          [nzLoading]="loading()"
          [nzTotal]="total()"
          [nzPageIndex]="currentPage()"
          [nzPageSize]="pageSize()"
          [nzFrontPagination]="false"
          [nzShowSizeChanger]="true"
          [nzPageSizeOptions]="[10, 20, 50, 100]"
          (nzPageIndexChange)="onPageChange($event)"
          (nzPageSizeChange)="onPageSizeChange($event)"
          nzBordered
          nzSize="middle"
        >
          <thead>
            <tr>
              <th
                [nzSortFn]="true"
                [nzSortOrder]="sortOrders()['title'] ?? null"
                (nzSortOrderChange)="onSortChange('title', $event)"
              >Título</th>
              <th
                [nzSortFn]="true"
                [nzSortOrder]="sortOrders()['promoter'] ?? null"
                (nzSortOrderChange)="onSortChange('promoter', $event)"
              >Promotor</th>
              <th
                [nzSortFn]="true"
                [nzSortOrder]="sortOrders()['organicUnit'] ?? null"
                (nzSortOrderChange)="onSortChange('organicUnit', $event)"
                nzWidth="180px"
              >Unidad orgánica</th>
              <th
                [nzSortFn]="true"
                [nzSortOrder]="sortOrders()['complexity'] ?? null"
                (nzSortOrderChange)="onSortChange('complexity', $event)"
                nzWidth="160px"
              >Complejidad</th>
              <th nzWidth="220px">Estado</th>
              <th>Etiquetas</th>
              <th
                [nzSortFn]="true"
                [nzSortOrder]="sortOrders()['portfolioYear'] ?? null"
                (nzSortOrderChange)="onSortChange('portfolioYear', $event)"
                nzWidth="100px"
              >Año cartera</th>
              <th nzWidth="200px">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (row of projects(); track row.id) {
              <tr>
                <td style="font-weight:600;font-size:14.5px">{{ row.title }}</td>
                <td>{{ row.promoterName ?? '—' }}</td>
                <td>{{ row.organicUnitName ?? '—' }}</td>
                <td><app-complexity-indicator [complexity]="row.complexity" /></td>
                <td>
                  <app-project-status-badge [status]="row.status" />
                </td>
                <td>
                  <nz-select
                    [ngModel]="tagIdsMap()[row.id] ?? []"
                    (ngModelChange)="onTagsChange(row, $event)"
                    nzMode="multiple"
                    nzPlaceHolder="+ Etiqueta"
                    nzAllowClear
                    [nzMaxTagCount]="3"
                    style="width: 100%; min-width: 160px"
                    nzSize="small"
                  >
                    @for (t of allTags(); track t.id) {
                      <nz-option [nzValue]="t.id" [nzLabel]="t.name" />
                    }
                  </nz-select>
                </td>
                <td style="font-variant-numeric:tabular-nums">{{ row.portfolioYear ?? '—' }}</td>
                <td>
                  <nz-space>
                    <button
                      *nzSpaceItem
                      nz-button
                      nzSize="small"
                      (click)="goToDetail(row.id)"
                      title="Ver detalle"
                    >
                      <span nz-icon nzType="eye"></span>
                    </button>
                    @if (canEdit(row.status)) {
                      <button
                        *nzSpaceItem
                        nz-button
                        nzSize="small"
                        (click)="openEdit(row)"
                        title="Editar"
                      >
                        <span nz-icon nzType="edit"></span>
                      </button>
                    }
                    @if (canDelete(row.status)) {
                      <button
                        *nzSpaceItem
                        nz-button
                        nzSize="small"
                        nzDanger
                        nz-popconfirm
                        nzPopconfirmTitle="¿Eliminar este proyecto?"
                        (nzOnConfirm)="deleteProject(row.id)"
                        title="Eliminar"
                      >
                        <span nz-icon nzType="delete"></span>
                      </button>
                    }
                  </nz-space>
                </td>
              </tr>
            }
          </tbody>
        </nz-table>
      } @else {
        <app-kanban-by-status
          [filterQ]="filterQ()"
          [filterComplexity]="filterComplexity()"
          [filterTagIds]="filterTagIds()"
          [filterPromoterId]="filterPromoterId()"
        />
      }
    </div>

    <!-- Modal de creación/edición -->
    <app-project-form
      [visible]="formVisible()"
      [project]="editingProject()"
      (saved)="onSaved()"
      (cancelled)="closeForm()"
    />
  `,
})
export class ProjectsListComponent implements OnInit, OnDestroy {
  private readonly service = inject(ProjectsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly message = inject(NzMessageService);

  // ── filter state (driven from URL) ──────────────────────────────────────────
  filterQ = signal('');          // valor aplicado (desde URL)
  inputQ = signal('');           // valor local del campo de texto (no se reescribe por la URL mientras el usuario escribe)
  filterStatus = signal<ProjectStatus | null>(null);
  filterComplexity = signal<ProjectComplexity | null>(null);
  filterTagIds = signal<number[]>([]);
  filterPromoterId = signal<number | null>(null);
  currentPage = signal(1);
  pageSize = signal(20);
  sortBy = signal<SortField | null>(null);
  sortDir = signal<SortDir>('asc');
  viewMode = signal<'tabla' | 'tablero'>('tabla');

  // Fix #5: pendingState initialized with defaults (no ! assertion).
  // Fix #3 (prev ronda): navigate() writes here synchronously to avoid the
  // pageSize/pageIndex race condition.
  private pendingState: ListState = {
    q: '',
    status: null,
    complexity: null,
    tagIds: [],
    promoterId: null,
    page: DEFAULT_PAGE,
    pageSize: DEFAULT_PAGE_SIZE,
    sortBy: null,
    sortDir: 'asc',
    viewMode: 'tabla',
  };

  // Fix #2: tracks the last q value that THIS component navigated to.
  // inputQ is only synced from the URL when parsed.q differs from this value,
  // which means the change came from outside (browser back/forward, external link)
  // rather than from the user's own typing via onQChange.
  private lastNavigatedQ = '';

  // ── sort order map for nz-table ──────────────────────────────────────────────
  readonly sortOrders = computed<SortOrderMap>(() => {
    const by = this.sortBy();
    const dir = this.sortDir();
    if (!by) return {};
    return { [by]: dir === 'asc' ? 'ascend' : 'descend' } as SortOrderMap;
  });

  allTags = signal<TagDto[]>([]);
  allPromoters = signal<PromoterDto[]>([]);
  projects = signal<Project[]>([]);
  loading = signal(false);
  formVisible = signal(false);
  editingProject = signal<ProjectDetail | null>(null);
  total = signal(0);

  readonly statusOptions = (Object.keys(PROJECT_STATUS_LABELS) as ProjectStatus[]).map(v => ({
    value: v, label: PROJECT_STATUS_LABELS[v],
  }));

  readonly complexityOptions = (Object.keys(PROJECT_COMPLEXITY_LABELS) as ProjectComplexity[]).map(v => ({
    value: v, label: PROJECT_COMPLEXITY_LABELS[v],
  }));

  // ── Fix #2: debounce pipeline for text search ────────────────────────────────
  private readonly qSubject = new Subject<string>();
  // switchMap for HTTP: cancels the previous request when a new one arrives
  private readonly loadSubject = new Subject<ProjectFilters>();
  private readonly subs = new Subscription();

  constructor() {
    this.service.getTags().subscribe({
      next: tags => this.allTags.set(tags),
      error: () => { /* silently ignore */ },
    });
    this.service.getPromoters().subscribe({
      next: result => this.allPromoters.set(result.items),
      error: () => { /* show no options; the list still works */ },
    });
  }

  ngOnInit(): void {
    // Debounce pipeline: each keystroke goes through qSubject; after 300ms of
    // silence we navigate (replaceUrl=true so no extra history entry per char).
    // Fix #3: no distinctUntilChanged — if the user types "ab", presses Back
    // (q="") and retypes "ab", distinctUntilChanged would swallow the second
    // emission. Instead, compare with pendingState.q inside the subscribe.
    this.subs.add(
      this.qSubject.pipe(debounceTime(300)).subscribe(q => {
        if (q === this.pendingState.q) return; // same as current state, skip
        this.lastNavigatedQ = q;
        this.navigateWith({ ...this.pendingState, q, page: 1 }, true);
      }),
    );

    // Fix #1: catchError INSIDE switchMap so a single HTTP error doesn't close
    // the outer subscription. EMPTY completes the inner observable cleanly and
    // lets subsequent loadSubject emissions create a new inner observable.
    this.subs.add(
      this.loadSubject.pipe(
        switchMap(filters => {
          this.loading.set(true);
          return this.service.getProjects(filters).pipe(
            catchError(() => {
              this.loading.set(false);
              this.message.error('Error al cargar los proyectos');
              return EMPTY;
            }),
          );
        }),
      ).subscribe(result => {
        this.projects.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      }),
    );

    // The URL is the source of truth. Subscribe to queryParamMap; parse and
    // load on every change (which includes browser back/forward).
    this.subs.add(
      this.route.queryParamMap.subscribe(paramMap => {
        const params: Record<string, string | string[]> = {};
        for (const key of paramMap.keys) {
          const all = paramMap.getAll(key);
          params[key] = all.length === 1 ? all[0] : all;
        }

        const parsed = parseProjectsListParams(params);

        // Fix #2: only sync inputQ when the URL's q differs from the last q
        // that THIS component navigated to. If parsed.q equals lastNavigatedQ,
        // the change was triggered by the user's own typing (debounce → navigate),
        // so we must NOT overwrite inputQ (which may be ahead of the debounced value).
        // If they differ, it's an external navigation (browser back/forward, direct URL)
        // and we must sync the input.
        if (parsed.q !== this.lastNavigatedQ) {
          this.inputQ.set(parsed.q);
          this.lastNavigatedQ = parsed.q;
        }

        this.filterQ.set(parsed.q);
        this.filterStatus.set(parsed.status);
        this.filterComplexity.set(parsed.complexity);
        this.filterTagIds.set(parsed.tagIds);
        this.filterPromoterId.set(parsed.promoterId);
        this.currentPage.set(parsed.page);
        this.pageSize.set(parsed.pageSize);
        this.sortBy.set(parsed.sortBy);
        this.sortDir.set(parsed.sortDir);
        this.viewMode.set(parsed.viewMode);

        // Snapshot for Fix #3: always keep pendingState in sync with the URL.
        this.pendingState = {
          q: parsed.q,
          status: parsed.status,
          complexity: parsed.complexity,
          tagIds: parsed.tagIds,
          promoterId: parsed.promoterId,
          page: parsed.page,
          pageSize: parsed.pageSize,
          sortBy: parsed.sortBy,
          sortDir: parsed.sortDir,
          viewMode: parsed.viewMode,
        };

        this.loadProjects();
      }),
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  private buildFilters(state: ListState): ProjectFilters {
    return {
      page: state.page,
      pageSize: state.pageSize,
      q: state.q || undefined,
      status: state.status ?? undefined,
      complexity: state.complexity ?? undefined,
      tagIds: state.tagIds.length ? state.tagIds : undefined,
      promoterId: state.promoterId ?? undefined,
      sortBy: state.sortBy ?? undefined,
      sortDir: state.sortBy ? state.sortDir : undefined,
    };
  }

  private loadProjects(): void {
    if (this.viewMode() !== 'tabla') return;
    this.loadSubject.next(this.buildFilters(this.pendingState));
  }

  /**
   * Fix #3: navigate() writes overrides into `pendingState` SYNCHRONOUSLY
   * before calling router.navigate, so any subsequent synchronous call (e.g.
   * nzPageIndexChange fired right after nzPageSizeChange) reads the already-
   * updated state instead of stale signal values.
   */
  private navigateWith(state: ListState, replaceUrl = false): void {
    this.pendingState = { ...state };
    const params = serializeProjectsListParams(state);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: params,
      replaceUrl,
    });
  }

  private navigate(overrides: Partial<ListState>, replaceUrl = false): void {
    this.navigateWith({ ...this.pendingState, ...overrides }, replaceUrl);
  }

  // ── filter change handlers ────────────────────────────────────────────────────

  onQChange(q: string): void {
    // Fix #2: keep local input signal up to date immediately (no URL rewrite),
    // but debounce the actual navigation (and thus the HTTP request).
    this.inputQ.set(q);
    this.qSubject.next(q);
  }

  onFilterChange(field: 'status' | 'complexity' | 'tagIds' | 'promoterId', value: unknown): void {
    const overrides: Partial<ListState> = { page: 1 };
    if (field === 'status') overrides.status = (value as ProjectStatus | null) ?? null;
    if (field === 'complexity') overrides.complexity = (value as ProjectComplexity | null) ?? null;
    if (field === 'tagIds') overrides.tagIds = (value as number[]) ?? [];
    if (field === 'promoterId') overrides.promoterId = (value as number | null) ?? null;
    this.navigate(overrides);
  }

  onPageChange(page: number): void {
    this.navigate({ page });
  }

  onPageSizeChange(size: number): void {
    // Fix #3: write pageSize synchronously into pendingState BEFORE any
    // subsequent nzPageIndexChange fires (which would otherwise read the old value).
    this.navigate({ pageSize: size, page: 1 });
  }

  onSortChange(field: SortField, order: NzTableSortOrder): void {
    if (!order) {
      this.navigate({ sortBy: null, sortDir: 'asc', page: 1 });
    } else {
      const dir: SortDir = order === 'descend' ? 'desc' : 'asc';
      this.navigate({ sortBy: field, sortDir: dir, page: 1 });
    }
  }

  onViewChange(mode: 'tabla' | 'tablero'): void {
    this.navigate({ viewMode: mode });
  }

  // ── tag inline edit ───────────────────────────────────────────────────────────

  tagIdsMap = computed(() => {
    const map: Record<number, number[]> = {};
    for (const p of this.projects()) {
      map[p.id] = p.tags.map(t => t.id);
    }
    return map;
  });

  onTagsChange(row: Project, tagIds: number[]): void {
    this.service.updateProject(row.id, {
      title: row.title,
      complexity: row.complexity,
      tagIds,
    }).subscribe({
      next: () => {
        row.tags = tagIds.map(id => this.allTags().find(t => t.id === id)!).filter(Boolean);
        this.projects.update(list => [...list]);
        this.message.success('Etiquetas actualizadas');
      },
      error: () => this.message.error('Error al actualizar etiquetas'),
    });
  }

  // ── helpers ───────────────────────────────────────────────────────────────────

  canEdit(_status: ProjectStatus): boolean {
    return true;
  }

  canDelete(status: ProjectStatus): boolean {
    return status === 'Stopped' || status === 'PostponedByClient';
  }

  goToDetail(id: number): void {
    this.router.navigate(['/projects', id]);
  }

  openCreate(): void {
    this.editingProject.set(null);
    this.formVisible.set(true);
  }

  openEdit(row: Project): void {
    this.service.getProject(row.id).subscribe({
      next: detail => {
        this.editingProject.set(detail);
        this.formVisible.set(true);
      },
      error: () => this.message.error('Error al cargar el proyecto'),
    });
  }

  closeForm(): void {
    this.formVisible.set(false);
    this.editingProject.set(null);
  }

  onSaved(): void {
    this.closeForm();
    this.loadProjects();
  }

  deleteProject(id: number): void {
    this.service.deleteProject(id).subscribe({
      next: () => {
        this.message.success('Proyecto eliminado');
        this.loadProjects();
      },
      error: () => this.message.error('Error al eliminar el proyecto'),
    });
  }
}
