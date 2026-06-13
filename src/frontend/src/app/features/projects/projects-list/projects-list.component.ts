import {
  ChangeDetectionStrategy,
  Component,
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
import { ProjectsService } from '../projects.service';
import { Project, ProjectComplexity, ProjectDetail, ProjectFilters, ProjectStatus } from '../project.model';
import { ProjectStatusBadgeComponent } from '../project-status-badge/project-status-badge.component';
import { ProjectFormComponent } from '../project-form/project-form.component';

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
    ProjectStatusBadgeComponent,
    ProjectFormComponent,
  ],
  template: `
    <div style="padding: 24px">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px">
        <h2 style="margin: 0">Cartera de Proyectos</h2>
        <button nz-button nzType="primary" (click)="openCreate()">
          <span nz-icon nzType="plus"></span>
          Nuevo proyecto
        </button>
      </div>

      <!-- Filtros -->
      <div style="display: flex; gap: 12px; margin-bottom: 16px; flex-wrap: wrap">
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
      </div>

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
            <th>Unidad solicitante</th>
            <th>Complejidad</th>
            <th>Estado</th>
            <th>Año cartera</th>
            <th nzWidth="200px">Acciones</th>
          </tr>
        </thead>
        <tbody>
          @for (row of projects(); track row.id) {
            <tr>
              <td>{{ row.title }}</td>
              <td>{{ row.requestingUnit }}</td>
              <td>{{ complexityLabel(row.complexity) }}</td>
              <td>
                <app-project-status-badge [status]="row.status" />
              </td>
              <td>{{ row.portfolioYear ?? '—' }}</td>
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

  projects = signal<Project[]>([]);
  loading = signal(false);
  formVisible = signal(false);
  editingProject = signal<ProjectDetail | null>(null);
  currentPage = signal(1);
  pageSize = signal(20);
  total = signal(0);

  readonly statusOptions: { value: ProjectStatus; label: string }[] = [
    { value: 'Proposed', label: 'Propuesto' },
    { value: 'Approved', label: 'Aprobado' },
    { value: 'InProgress', label: 'En ejecución' },
    { value: 'Paused', label: 'Pausado' },
    { value: 'Completed', label: 'Completado' },
    { value: 'Cancelled', label: 'Cancelado' },
  ];

  readonly complexityOptions: { value: ProjectComplexity; label: string }[] = [
    { value: 'Low', label: 'Baja' },
    { value: 'Medium', label: 'Media' },
    { value: 'High', label: 'Alta' },
    { value: 'VeryHigh', label: 'Muy alta' },
  ];

  constructor() {
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

  complexityLabel(c: ProjectComplexity): string {
    const map: Record<ProjectComplexity, string> = {
      Low: 'Baja',
      Medium: 'Media',
      High: 'Alta',
      VeryHigh: 'Muy alta',
    };
    return map[c] ?? c;
  }

  canEdit(status: ProjectStatus): boolean {
    return status === 'Proposed' || status === 'Approved';
  }

  canDelete(status: ProjectStatus): boolean {
    return status === 'Proposed' || status === 'Cancelled';
  }

  goToDetail(id: string): void {
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

  deleteProject(id: string): void {
    this.service.deleteProject(id).subscribe({
      next: () => {
        this.message.success('Proyecto eliminado');
        this.loadProjects();
      },
      error: () => this.message.error('Error al eliminar el proyecto'),
    });
  }
}
