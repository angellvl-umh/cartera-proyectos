import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, Subject, startWith } from 'rxjs';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { ProjectsService } from '../projects.service';
import { ProjectDetail, ProjectStatus, ProjectTeam } from '../project.model';
import { ProjectStatusBadgeComponent } from '../project-status-badge/project-status-badge.component';
import { ProjectFormComponent } from '../project-form/project-form.component';

interface StatusTransition {
  label: string;
  status: ProjectStatus;
  danger?: boolean;
}

const TRANSITIONS: Record<ProjectStatus, StatusTransition[]> = {
  Proposed: [
    { label: 'Aprobar', status: 'Approved' },
    { label: 'Cancelar', status: 'Cancelled', danger: true },
  ],
  Approved: [
    { label: 'Iniciar ejecución', status: 'InProgress' },
    { label: 'Cancelar', status: 'Cancelled', danger: true },
  ],
  InProgress: [
    { label: 'Pausar', status: 'Paused' },
    { label: 'Completar', status: 'Completed' },
    { label: 'Cancelar', status: 'Cancelled', danger: true },
  ],
  Paused: [
    { label: 'Reanudar', status: 'InProgress' },
    { label: 'Cancelar', status: 'Cancelled', danger: true },
  ],
  Completed: [],
  Cancelled: [],
};

@Component({
  selector: 'app-project-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NzCardModule,
    NzButtonModule,
    NzDescriptionsModule,
    NzTableModule,
    NzPopconfirmModule,
    NzSpaceModule,
    NzDividerModule,
    NzIconModule,
    NzSpinModule,
    ProjectStatusBadgeComponent,
    ProjectFormComponent,
  ],
  template: `
    @if (project() === undefined) {
      <div style="display: flex; justify-content: center; padding: 64px">
        <nz-spin nzSize="large" />
      </div>
    } @else if (project() === null) {
      <div style="padding: 24px">
        <p>Proyecto no encontrado.</p>
        <button nz-button (click)="goBack()">Volver</button>
      </div>
    } @else {
      <div style="padding: 24px; max-width: 960px; margin: 0 auto">
        <!-- Encabezado -->
        <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 16px">
          <div>
            <button nz-button nzType="text" (click)="goBack()">
              <span nz-icon nzType="arrow-left"></span>
              Volver
            </button>
            <h2 style="margin: 8px 0 4px">{{ project()!.title }}</h2>
            <app-project-status-badge [status]="project()!.status" />
          </div>

          <!-- Transiciones de estado -->
          <nz-space>
            @for (t of transitions(); track t.status) {
              <button
                *nzSpaceItem
                nz-button
                [nzDanger]="t.danger === true"
                [nzType]="t.danger ? 'default' : 'primary'"
                nz-popconfirm
                [nzPopconfirmTitle]="'¿Confirmar: ' + t.label + '?'"
                (nzOnConfirm)="transition(t.status)"
              >
                {{ t.label }}
              </button>
            }
            @if (canEdit()) {
              <button *nzSpaceItem nz-button (click)="openEdit()">
                <span nz-icon nzType="edit"></span>
                Editar
              </button>
            }
          </nz-space>
        </div>

        <!-- Datos del proyecto -->
        <nz-card nzTitle="Datos del proyecto" style="margin-bottom: 16px">
          <nz-descriptions nzBordered [nzColumn]="2">
            <nz-descriptions-item nzTitle="Unidad solicitante">
              {{ project()!.requestingUnit }}
            </nz-descriptions-item>
            <nz-descriptions-item nzTitle="Complejidad">
              {{ complexityLabel(project()!.complexity) }}
            </nz-descriptions-item>
            <nz-descriptions-item nzTitle="Año de cartera">
              {{ project()!.portfolioYear ?? '—' }}
            </nz-descriptions-item>
            <nz-descriptions-item nzTitle="Estado">
              <app-project-status-badge [status]="project()!.status" />
            </nz-descriptions-item>
            <nz-descriptions-item nzTitle="Fecha de inicio">
              {{ project()!.startDate ?? '—' }}
            </nz-descriptions-item>
            <nz-descriptions-item nzTitle="Fecha de fin">
              {{ project()!.endDate ?? '—' }}
            </nz-descriptions-item>
            @if (project()!.description) {
              <nz-descriptions-item nzTitle="Descripción" [nzSpan]="2">
                {{ project()!.description }}
              </nz-descriptions-item>
            }
          </nz-descriptions>
        </nz-card>

        <!-- Equipos asignados -->
        <nz-card nzTitle="Equipos asignados">
          <nz-table
            [nzData]="project()!.teams"
            nzBordered
            nzSize="small"
            [nzShowPagination]="false"
          >
            <thead>
              <tr>
                <th>Equipo</th>
                <th>Primario</th>
                <th nzWidth="100px">Acción</th>
              </tr>
            </thead>
            <tbody>
              @for (team of project()!.teams; track team.teamId) {
                <tr>
                  <td>{{ team.teamName }}</td>
                  <td>{{ team.isPrimary ? 'Sí' : 'No' }}</td>
                  <td>
                    <button
                      nz-button
                      nzSize="small"
                      nzDanger
                      nz-popconfirm
                      nzPopconfirmTitle="¿Desasignar este equipo?"
                      (nzOnConfirm)="removeTeam(team)"
                    >
                      <span nz-icon nzType="delete"></span>
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="3" style="text-align: center; color: #999">Sin equipos asignados</td>
                </tr>
              }
            </tbody>
          </nz-table>
        </nz-card>
      </div>
    }

    <!-- Modal de edición -->
    <app-project-form
      [visible]="formVisible()"
      [project]="project() ?? null"
      (saved)="onSaved()"
      (cancelled)="formVisible.set(false)"
    />
  `,
})
export class ProjectDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(ProjectsService);
  private readonly message = inject(NzMessageService);

  formVisible = signal(false);

  private readonly refresh$ = new Subject<void>();

  project = toSignal<ProjectDetail | null>(
    this.refresh$.pipe(
      startWith(undefined),
      switchMap(() => {
        const id = this.route.snapshot.paramMap.get('id')!;
        return this.service.getProject(id);
      })
    )
  );

  transitions(): StatusTransition[] {
    const p = this.project();
    if (!p) return [];
    return TRANSITIONS[p.status] ?? [];
  }

  canEdit(): boolean {
    const s = this.project()?.status;
    return s === 'Proposed' || s === 'Approved';
  }

  complexityLabel(c: string): string {
    const map: Record<string, string> = {
      Low: 'Baja',
      Medium: 'Media',
      High: 'Alta',
      VeryHigh: 'Muy alta',
    };
    return map[c] ?? c;
  }

  goBack(): void {
    this.router.navigate(['/projects']);
  }

  transition(status: ProjectStatus): void {
    const id = this.project()!.id;
    this.service.transitionStatus(id, status).subscribe({
      next: () => {
        this.message.success('Estado actualizado');
        this.refresh$.next();
      },
      error: () => this.message.error('No se pudo cambiar el estado'),
    });
  }

  removeTeam(team: ProjectTeam): void {
    const id = this.project()!.id;
    this.service.removeTeam(id, team.teamId).subscribe({
      next: () => {
        this.message.success(`Equipo "${team.teamName}" desasignado`);
        this.refresh$.next();
      },
      error: () => this.message.error('Error al desasignar el equipo'),
    });
  }

  openEdit(): void {
    this.formVisible.set(true);
  }

  onSaved(): void {
    this.formVisible.set(false);
    this.refresh$.next();
  }
}
