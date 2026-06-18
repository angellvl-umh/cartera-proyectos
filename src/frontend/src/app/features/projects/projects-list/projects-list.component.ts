import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { ProjectsService } from '../projects.service';
import {
  PROJECT_COMPLEXITY_LABELS,
  PROJECT_STATUS_LABELS,
  Project,
  ProjectComplexity,
  ProjectDetail,
  ProjectFilters,
  ProjectStatus,
  TagDto,
} from '../project.model';
import { ProjectStatusBadgeComponent } from '../project-status-badge/project-status-badge.component';
import { ProjectFormComponent } from '../project-form/project-form.component';
import { ComplexityIndicatorComponent } from '../complexity-indicator/complexity-indicator.component';
import { KanbanByStatusComponent } from '../kanban-by-status/kanban-by-status.component';

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
        <input
          nz-input
          placeholder="Buscar por título..."
          [(ngModel)]="filterQ"
          (ngModelChange)="applyFilters()"
          style="width: 240px"
        />
        <nz-select
          [(ngModel)]="filterStatus"
          (ngModelChange)="applyFilters()"
          nzPlaceHolder="Estado"
          nzAllowClear
          style="width: 180px"
        >
          @for (opt of statusOptions; track opt.value) {
            <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
          }
        </nz-select>
        <nz-select
          [(ngModel)]="filterComplexity"
          (ngModelChange)="applyFilters()"
          nzPlaceHolder="Complejidad"
          nzAllowClear
          style="width: 160px"
        >
          @for (opt of complexityOptions; track opt.value) {
            <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
          }
        </nz-select>
        <nz-select
          [(ngModel)]="filterTagIds"
          (ngModelChange)="applyFilters()"
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

        <div class="view-toggle">
          <button type="button" [class.active]="viewMode() === 'tabla'" (click)="viewMode.set('tabla')">Tabla</button>
          <button type="button" [class.active]="viewMode() === 'tablero'" (click)="viewMode.set('tablero')">Tablero</button>
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
          (nzPageIndexChange)="onPageChange($event)"
          (nzPageSizeChange)="onPageSizeChange($event)"
          nzBordered
          nzSize="middle"
        >
          <thead>
            <tr>
              <th>Título</th>
              <th nzWidth="160px">Complejidad</th>
              <th nzWidth="220px">Estado</th>
              <th>Etiquetas</th>
              <th nzWidth="100px">Año cartera</th>
              <th nzWidth="200px">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (row of projects(); track row.id) {
              <tr>
                <td style="font-weight:600;font-size:14.5px">{{ row.title }}</td>
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
          [filterQ]="filterQ"
          [filterComplexity]="filterComplexity"
          [filterTagIds]="filterTagIds"
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
export class ProjectsListComponent {
  private readonly service = inject(ProjectsService);
  private readonly router = inject(Router);
  private readonly message = inject(NzMessageService);

  filterQ = '';
  filterStatus: ProjectStatus | null = null;
  filterComplexity: ProjectComplexity | null = null;
  filterTagIds: number[] = [];

  allTags = signal<TagDto[]>([]);
  projects = signal<Project[]>([]);
  loading = signal(false);
  formVisible = signal(false);
  editingProject = signal<ProjectDetail | null>(null);
  currentPage = signal(1);
  pageSize = signal(20);
  total = signal(0);
  viewMode = signal<'tabla' | 'tablero'>('tabla');

  readonly statusOptions = (Object.keys(PROJECT_STATUS_LABELS) as ProjectStatus[]).map(v => ({
    value: v, label: PROJECT_STATUS_LABELS[v],
  }));

  readonly complexityOptions = (Object.keys(PROJECT_COMPLEXITY_LABELS) as ProjectComplexity[]).map(v => ({
    value: v, label: PROJECT_COMPLEXITY_LABELS[v],
  }));

  constructor() {
    this.service.getTags().subscribe(tags => this.allTags.set(tags));
    this.loadProjects();
  }

  private buildFilters(): ProjectFilters {
    const f: ProjectFilters = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
    };
    if (this.filterQ) f.q = this.filterQ;
    if (this.filterStatus) f.status = this.filterStatus;
    if (this.filterComplexity) f.complexity = this.filterComplexity;
    if (this.filterTagIds.length) f.tagIds = this.filterTagIds;
    return f;
  }

  private loadProjects(): void {
    this.loading.set(true);
    this.service.getProjects(this.buildFilters()).subscribe({
      next: (result) => {
        this.projects.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.message.error('Error al cargar los proyectos');
      },
    });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadProjects();
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
    this.loadProjects();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadProjects();
  }

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

  complexityLabel(c: ProjectComplexity): string {
    return PROJECT_COMPLEXITY_LABELS[c] ?? c;
  }

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
