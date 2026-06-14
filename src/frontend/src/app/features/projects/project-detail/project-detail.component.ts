import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, Subject, startWith } from 'rxjs';
import { FormsModule } from '@angular/forms';
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
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { HttpClient } from '@angular/common/http';
import { ProjectsService } from '../projects.service';
import { EpicsService, Epic, CreateEpicDto } from '../epics.service';
import { WorkItemsService, WorkItem, WorkItemStatus, WorkItemPriority } from '../workitems.service';
import { SprintService, Sprint, CreateSprintDto } from '../sprint.service';
import { CommentsService, CommentDto } from '../comments.service';
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

const STATUS_COLORS: Record<WorkItemStatus, string> = {
  Backlog: 'default',
  ToDo: 'blue',
  InProgress: 'processing',
  Blocked: 'error',
  Done: 'success',
};

const STATUS_LABELS: Record<WorkItemStatus, string> = {
  Backlog: 'Backlog',
  ToDo: 'Por hacer',
  InProgress: 'En curso',
  Blocked: 'Bloqueada',
  Done: 'Hecho',
};

const PRIORITY_COLORS: Record<WorkItemPriority, string> = {
  Low: 'default',
  Medium: 'blue',
  High: 'orange',
  Critical: 'red',
};

const SPRINT_STATUS_COLORS: Record<string, string> = {
  Planning: 'default',
  Active: 'processing',
  Completed: 'success',
};

@Component({
  selector: 'app-project-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzCardModule, NzButtonModule, NzDescriptionsModule, NzTableModule,
    NzPopconfirmModule, NzSpaceModule, NzDividerModule, NzIconModule,
    NzSpinModule, NzTabsModule, NzTagModule, NzModalModule, NzFormModule,
    NzInputModule, NzInputNumberModule, NzSelectModule, NzDatePickerModule,
    NzListModule, NzAvatarModule,
    RouterLink, ProjectStatusBadgeComponent, ProjectFormComponent,
  ],
  template: `
    @if (project() === undefined) {
      <div style="display:flex;justify-content:center;padding:64px">
        <nz-spin nzSize="large" />
      </div>
    } @else if (project() === null) {
      <div>
        <p>Proyecto no encontrado.</p>
        <button nz-button (click)="goBack()">Volver</button>
      </div>
    } @else {
      <div style="max-width:1100px;margin:0 auto">

        <!-- Encabezado -->
        <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:16px">
          <div>
            <button nz-button nzType="text" (click)="goBack()">
              <span nz-icon nzType="arrow-left"></span> Volver
            </button>
            <h2 style="margin:8px 0 4px">{{ project()!.title }}</h2>
            <app-project-status-badge [status]="project()!.status" />
          </div>
          <nz-space>
            @for (t of transitions(); track t.status) {
              <button *nzSpaceItem nz-button [nzDanger]="t.danger === true"
                [nzType]="t.danger ? 'default' : 'primary'"
                nz-popconfirm [nzPopconfirmTitle]="'¿Confirmar: ' + t.label + '?'"
                (nzOnConfirm)="transition(t.status)">
                {{ t.label }}
              </button>
            }
            <a *nzSpaceItem nz-button [routerLink]="['/projects', projectId, 'kanban']">
              <span nz-icon nzType="project"></span> Kanban
            </a>
            <a *nzSpaceItem nz-button [routerLink]="['/projects', projectId, 'report']">
              <span nz-icon nzType="bar-chart"></span> Informe
            </a>
            @if (canEdit()) {
              <button *nzSpaceItem nz-button (click)="openEdit()">
                <span nz-icon nzType="edit"></span> Editar
              </button>
            }
          </nz-space>
        </div>

        <!-- Tabs -->
        <nz-tabs>

          <!-- TAB: Info -->
          <nz-tab nzTitle="Información">
            <nz-card nzTitle="Datos del proyecto" style="margin-bottom:16px">
              <nz-descriptions nzBordered [nzColumn]="2">
                <nz-descriptions-item nzTitle="Unidad solicitante">{{ project()!.requestingUnit }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Complejidad">{{ complexityLabel(project()!.complexity) }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Año de cartera">{{ project()!.portfolioYear ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Estado"><app-project-status-badge [status]="project()!.status" /></nz-descriptions-item>
                <nz-descriptions-item nzTitle="Fecha de inicio">{{ project()!.startDate ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Fecha de fin">{{ project()!.endDate ?? '—' }}</nz-descriptions-item>
                @if (project()!.description) {
                  <nz-descriptions-item nzTitle="Descripción" [nzSpan]="2">{{ project()!.description }}</nz-descriptions-item>
                }
              </nz-descriptions>
            </nz-card>

            <nz-card nzTitle="Equipos asignados">
              <nz-table [nzData]="project()!.teams" nzBordered nzSize="small" [nzShowPagination]="false">
                <thead><tr><th>Equipo</th><th>Primario</th><th>Acción</th></tr></thead>
                <tbody>
                  @for (team of project()!.teams; track team.teamId) {
                    <tr>
                      <td>{{ team.teamName }}</td>
                      <td>{{ team.isPrimary ? 'Sí' : 'No' }}</td>
                      <td>
                        <button nz-button nzSize="small" nzDanger nz-popconfirm
                          nzPopconfirmTitle="¿Desasignar este equipo?" (nzOnConfirm)="removeTeam(team)">
                          <span nz-icon nzType="delete"></span>
                        </button>
                      </td>
                    </tr>
                  } @empty {
                    <tr><td colspan="3" style="text-align:center;color:#999">Sin equipos asignados</td></tr>
                  }
                </tbody>
              </nz-table>
            </nz-card>
          </nz-tab>

          <!-- TAB: Épicas -->
          <nz-tab nzTitle="Épicas">
            <div style="margin-bottom:12px;text-align:right">
              <button nz-button nzType="primary" (click)="openEpicForm()">
                <span nz-icon nzType="plus"></span> Nueva épica
              </button>
            </div>
            <nz-table [nzData]="epics()?.items ?? []" nzBordered nzSize="small"
              [nzLoading]="!epics()" [nzShowPagination]="false">
              <thead><tr><th>Título</th><th>Prioridad</th><th>Tareas</th><th>Orden</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (epic of epics()?.items ?? []; track epic.id) {
                  <tr>
                    <td>{{ epic.title }}</td>
                    <td>{{ epic.priority }}</td>
                    <td>{{ epic.workItemCount }}</td>
                    <td>{{ epic.sortOrder }}</td>
                    <td>
                      <nz-space nzSize="small">
                        <button *nzSpaceItem nz-button nzSize="small" (click)="openEpicForm(epic)">
                          <span nz-icon nzType="edit"></span>
                        </button>
                        <button *nzSpaceItem nz-button nzSize="small" nzDanger nz-popconfirm
                          nzPopconfirmTitle="¿Eliminar épica?" (nzOnConfirm)="deleteEpic(epic)">
                          <span nz-icon nzType="delete"></span>
                        </button>
                      </nz-space>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="5" style="text-align:center;color:#999">Sin épicas</td></tr>
                }
              </tbody>
            </nz-table>
          </nz-tab>

          <!-- TAB: Sprints -->
          <nz-tab nzTitle="Sprints">
            <div style="margin-bottom:12px;text-align:right">
              <button nz-button nzType="primary" (click)="openSprintForm()">
                <span nz-icon nzType="plus"></span> Nuevo sprint
              </button>
            </div>
            <nz-table [nzData]="sprints()?.items ?? []" nzBordered nzSize="small"
              [nzLoading]="!sprints()" [nzShowPagination]="false">
              <thead><tr><th>Nombre</th><th>Estado</th><th>Fechas</th><th>Capacidad</th><th>Tareas</th><th>Horas</th><th>Puntos</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (sprint of sprints()?.items ?? []; track sprint.id) {
                  <tr>
                    <td>{{ sprint.name }}</td>
                    <td><nz-tag [nzColor]="SPRINT_STATUS_COLORS[sprint.status]">{{ sprint.status }}</nz-tag></td>
                    <td>{{ sprint.startDate ?? '—' }} / {{ sprint.endDate ?? '—' }}</td>
                    <td>{{ sprint.capacity ?? '—' }}</td>
                    <td>{{ sprint.workItemCount }}</td>
                    <td>{{ sprint.totalEstimationHours }}</td>
                    <td>{{ sprint.totalEstimationPoints }}</td>
                    <td>
                      <nz-space nzSize="small">
                        @if (sprint.status === 'Planning') {
                          <button *nzSpaceItem nz-button nzSize="small" (click)="openSprintForm(sprint)">
                            <span nz-icon nzType="edit"></span>
                          </button>
                          <button *nzSpaceItem nz-button nzSize="small" nzDanger nz-popconfirm
                            nzPopconfirmTitle="¿Eliminar sprint?" (nzOnConfirm)="deleteSprint(sprint)">
                            <span nz-icon nzType="delete"></span>
                          </button>
                        }
                        @if (sprint.status === 'Planning') {
                          <button *nzSpaceItem nz-button nzSize="small" nzType="primary"
                            (click)="transitionSprint(sprint, 'Active')">
                            Iniciar
                          </button>
                        }
                        @if (sprint.status === 'Active') {
                          <button *nzSpaceItem nz-button nzSize="small" nzType="primary"
                            (click)="transitionSprint(sprint, 'Completed')">
                            Completar
                          </button>
                        }
                        <a *nzSpaceItem [routerLink]="['/projects', projectId, 'sprints', sprint.id, 'kanban']"
                          nz-button nzSize="small">
                          <span nz-icon nzType="eye"></span> Kanban
                        </a>
                      </nz-space>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="8" style="text-align:center;color:#999">Sin sprints</td></tr>
                }
              </tbody>
            </nz-table>
          </nz-tab>

          <!-- TAB: Product Backlog -->
          <nz-tab nzTitle="Product Backlog">
            <div style="margin-bottom:12px;text-align:right">
              <button nz-button nzType="primary" (click)="openWorkItemForm()">
                <span nz-icon nzType="plus"></span> Nueva tarea
              </button>
            </div>
            <nz-table [nzData]="backlog()?.items ?? []" nzBordered nzSize="small"
              [nzLoading]="!backlog()" [nzShowPagination]="false">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>Épica</th>
                  <th>Estado</th>
                  <th>Prioridad</th>
                  <th>Asignado a</th>
                  <th>Est. h</th>
                  <th>Est. pts</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (wi of backlog()?.items ?? []; track wi.id) {
                  <tr>
                    <td>{{ wi.title }}</td>
                    <td>{{ wi.epicTitle ?? '—' }}</td>
                    <td><nz-tag [nzColor]="statusColor(wi.status)">{{ statusLabel(wi.status) }}</nz-tag></td>
                    <td><nz-tag [nzColor]="priorityColor(wi.priority)">{{ wi.priority }}</nz-tag></td>
                    <td>{{ wi.assignees.length > 0 ? wi.assignees.map(a => a.name).join(', ') : '—' }}</td>
                    <td>{{ wi.estimationHours ?? '—' }}</td>
                    <td>{{ wi.estimationPoints ?? '—' }}</td>
                    <td>
                      <nz-space nzSize="small">
                        <button *nzSpaceItem nz-button nzSize="small" (click)="openAssignSprintModal(wi)">
                          <span nz-icon nzType="link"></span> Sprint
                        </button>
                        <button *nzSpaceItem nz-button nzSize="small" (click)="openComments(wi)">
                          <span nz-icon nzType="comment"></span>
                        </button>
                        <button *nzSpaceItem nz-button nzSize="small" (click)="openWorkItemForm(wi)">
                          <span nz-icon nzType="edit"></span>
                        </button>
                        <button *nzSpaceItem nz-button nzSize="small" nzDanger nz-popconfirm
                          nzPopconfirmTitle="¿Eliminar tarea?" (nzOnConfirm)="deleteWorkItem(wi)">
                          <span nz-icon nzType="delete"></span>
                        </button>
                      </nz-space>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="8" style="text-align:center;color:#999">Sin tareas en backlog</td></tr>
                }
              </tbody>
            </nz-table>
          </nz-tab>

        </nz-tabs>
      </div>
    }

    <!-- Modal editar proyecto -->
    <app-project-form
      [visible]="formVisible()"
      [project]="project() ?? null"
      (saved)="onSaved()"
      (cancelled)="formVisible.set(false)"
    />

    <!-- Modal épica -->
    <nz-modal
      [nzVisible]="epicModalVisible()"
      [nzTitle]="editingEpic() ? 'Editar épica' : 'Nueva épica'"
      (nzOnCancel)="epicModalVisible.set(false)"
      (nzOnOk)="saveEpic()"
    >
      <ng-container *nzModalContent>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Título</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <input nz-input [(ngModel)]="epicForm.title" placeholder="Título de la épica" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Descripción</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <textarea nz-input [(ngModel)]="epicForm.description" [nzAutosize]="{ minRows: 2 }"></textarea>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Prioridad</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-input-number [(ngModel)]="epicForm.priority" [nzMin]="0" style="width:100%"></nz-input-number>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Orden</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-input-number [(ngModel)]="epicForm.sortOrder" [nzMin]="0" style="width:100%"></nz-input-number>
          </nz-form-control>
        </nz-form-item>
      </ng-container>
    </nz-modal>

    <!-- Modal sprint -->
    <nz-modal
      [nzVisible]="sprintModalVisible()"
      [nzTitle]="editingSprint() ? 'Editar sprint' : 'Nuevo sprint'"
      (nzOnCancel)="sprintModalVisible.set(false)"
      (nzOnOk)="saveSprint()"
    >
      <ng-container *nzModalContent>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Nombre</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <input nz-input [(ngModel)]="sprintForm.name" placeholder="Nombre del sprint" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Objetivo</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <textarea nz-input [(ngModel)]="sprintForm.goal" [nzAutosize]="{ minRows: 2 }"></textarea>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Fecha inicio</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-date-picker [(ngModel)]="sprintForm.startDate" nzFormat="yyyy-MM-dd" style="width:100%"></nz-date-picker>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Fecha fin</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-date-picker [(ngModel)]="sprintForm.endDate" nzFormat="yyyy-MM-dd" style="width:100%"></nz-date-picker>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Capacidad</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-input-number [(ngModel)]="sprintForm.capacity" [nzMin]="1" style="width:100%"></nz-input-number>
          </nz-form-control>
        </nz-form-item>
      </ng-container>
    </nz-modal>

    <!-- Modal tarea -->
    <nz-modal
      [nzVisible]="workItemModalVisible()"
      [nzTitle]="editingWorkItem() ? 'Editar tarea' : 'Nueva tarea'"
      (nzOnCancel)="workItemModalVisible.set(false)"
      (nzOnOk)="saveWorkItem()"
    >
      <ng-container *nzModalContent>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Título</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <input nz-input [(ngModel)]="workItemForm.title" placeholder="Título de la tarea" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Descripción</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <textarea nz-input [(ngModel)]="workItemForm.description" [nzAutosize]="{ minRows: 2 }"></textarea>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Épica</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="workItemForm.epicId" nzAllowClear nzPlaceHolder="Sin épica" style="width:100%">
              @for (epic of epics()?.items ?? []; track epic.id) {
                <nz-option [nzValue]="epic.id" [nzLabel]="epic.title"></nz-option>
              }
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Sprint</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="workItemForm.sprintId" nzAllowClear nzPlaceHolder="Sin sprint" style="width:100%">
              @for (sprint of sprints()?.items ?? []; track sprint.id) {
                <nz-option [nzValue]="sprint.id" [nzLabel]="sprint.name"></nz-option>
              }
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Asignados</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="workItemForm.assigneeIds" nzMode="multiple"
              nzAllowClear nzPlaceHolder="Sin asignar" style="width:100%">
              @for (p of persons()?.items ?? []; track p.id) {
                <nz-option [nzValue]="p.id" [nzLabel]="p.name"></nz-option>
              }
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Prioridad</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="workItemForm.priority" style="width:100%">
              <nz-option nzValue="Low" nzLabel="Baja"></nz-option>
              <nz-option nzValue="Medium" nzLabel="Media"></nz-option>
              <nz-option nzValue="High" nzLabel="Alta"></nz-option>
              <nz-option nzValue="Critical" nzLabel="Crítica"></nz-option>
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Fecha fin</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-date-picker [(ngModel)]="workItemForm.dueDate" nzFormat="yyyy-MM-dd" style="width:100%"></nz-date-picker>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Estimación (h)</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-input-number [(ngModel)]="workItemForm.estimationHours" [nzMin]="1" style="width:100%"></nz-input-number>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Estimación (pts)</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-input-number [(ngModel)]="workItemForm.estimationPoints" [nzMin]="1" style="width:100%"></nz-input-number>
          </nz-form-control>
        </nz-form-item>
      </ng-container>
    </nz-modal>

    <!-- Modal asignar sprint -->
    <nz-modal
      [nzVisible]="assignSprintModalVisible()"
      nzTitle="Asignar a Sprint"
      (nzOnCancel)="assignSprintModalVisible.set(false)"
      (nzOnOk)="saveAssignSprint()"
    >
      <ng-container *nzModalContent>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Sprint</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="assignSprintId" nzAllowClear nzPlaceHolder="Selecciona sprint" style="width:100%">
              @for (sprint of sprints()?.items ?? []; track sprint.id) {
                <nz-option [nzValue]="sprint.id" [nzLabel]="sprint.name"></nz-option>
              }
            </nz-select>
          </nz-form-control>
        </nz-form-item>
      </ng-container>
    </nz-modal>

    <!-- Modal comentarios -->
    <nz-modal
      [nzVisible]="commentsModalVisible()"
      [nzTitle]="'Comentarios: ' + (commentsWorkItem()?.title ?? '')"
      [nzFooter]="null"
      nzWidth="600px"
      (nzOnCancel)="commentsModalVisible.set(false)"
    >
      <ng-container *nzModalContent>
        @if (commentsLoading()) {
          <div style="text-align:center;padding:24px"><nz-spin /></div>
        } @else {
          @if (comments().length === 0) {
            <p style="color:#999;text-align:center">Sin comentarios aún.</p>
          }
          @for (c of comments(); track c.id) {
            <div style="display:flex;gap:12px;margin-bottom:12px">
              <nz-avatar [nzText]="c.authorName[0]" style="background:#1890ff;flex-shrink:0"></nz-avatar>
              <div style="flex:1">
                <div style="display:flex;align-items:baseline;gap:8px;margin-bottom:4px">
                  <span style="font-weight:600;font-size:13px">{{ c.authorName }}</span>
                  <span style="font-size:12px;color:#8c8c8c">{{ formatCommentDate(c.createdAt) }}</span>
                  @if (currentPerson()?.id === c.authorId) {
                    <span style="font-size:12px;color:#ff4d4f;cursor:pointer;margin-left:auto"
                      nz-popconfirm nzPopconfirmTitle="¿Eliminar comentario?"
                      (nzOnConfirm)="deleteComment(c)">Eliminar</span>
                  }
                </div>
                <p style="margin:0;font-size:13px;white-space:pre-wrap">{{ c.text }}</p>
              </div>
            </div>
            <nz-divider style="margin:8px 0"></nz-divider>
          }
        }

        <!-- Añadir comentario -->
        <div style="margin-top:16px">
          <textarea nz-input [(ngModel)]="newCommentText" [nzAutosize]="{ minRows: 2, maxRows: 5 }"
            placeholder="Escribe un comentario..." style="margin-bottom:8px"></textarea>
          <button nz-button nzType="primary" [nzLoading]="addingComment()"
            [disabled]="!newCommentText.trim()" (click)="addComment()">
            Publicar
          </button>
        </div>
      </ng-container>
    </nz-modal>
  `,
})
export class ProjectDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(ProjectsService);
  private readonly epicsService = inject(EpicsService);
  private readonly workItemsService = inject(WorkItemsService);
  private readonly sprintService = inject(SprintService);
  private readonly commentsService = inject(CommentsService);
  private readonly http = inject(HttpClient);
  private readonly message = inject(NzMessageService);

  readonly persons = toSignal(
    this.http.get<{ items: { id: number; name: string }[] }>('/api/persons?pageSize=100')
  );

  readonly currentPerson = toSignal(
    this.http.get<{ id: number; name: string; role: string }>('/api/me')
  );

  formVisible = signal(false);
  epicModalVisible = signal(false);
  sprintModalVisible = signal(false);
  workItemModalVisible = signal(false);
  assignSprintModalVisible = signal(false);
  commentsModalVisible = signal(false);

  editingEpic = signal<Epic | null>(null);
  editingSprint = signal<Sprint | null>(null);
  editingWorkItem = signal<WorkItem | null>(null);
  commentsWorkItem = signal<WorkItem | null>(null);

  assignSprintId: number | null = null;
  selectedWorkItemForAssign: WorkItem | null = null;

  comments = signal<CommentDto[]>([]);
  commentsLoading = signal(false);
  addingComment = signal(false);
  newCommentText = '';

  epicForm: CreateEpicDto = { title: '', priority: 0, sortOrder: 0 };
  sprintForm: CreateSprintDto = { name: '' };
  workItemForm: {
    title: string;
    description?: string;
    priority: WorkItemPriority;
    epicId?: number;
    sprintId?: number;
    assigneeIds: number[];
    estimationHours?: number;
    estimationPoints?: number;
    sortOrder: number;
    isHito: boolean;
    hitoDate?: string;
    dueDate?: string;
  } = {
    title: '',
    priority: 'Medium',
    sortOrder: 0,
    isHito: false,
    assigneeIds: [],
  };

  private readonly refresh$ = new Subject<void>();
  private readonly epicsRefresh$ = new Subject<void>();
  private readonly sprintsRefresh$ = new Subject<void>();
  private readonly backlogRefresh$ = new Subject<void>();

  readonly SPRINT_STATUS_COLORS = SPRINT_STATUS_COLORS;

  get projectId(): number {
    return +this.route.snapshot.paramMap.get('id')!;
  }

  private get projectIdStr(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  project = toSignal<ProjectDetail | null>(
    this.refresh$.pipe(
      startWith(undefined),
      switchMap(() => this.service.getProject(this.projectIdStr))
    )
  );

  epics = toSignal(
    this.epicsRefresh$.pipe(
      startWith(null),
      switchMap(() => this.epicsService.getEpics(this.projectId))
    )
  );

  sprints = toSignal(
    this.sprintsRefresh$.pipe(
      startWith(null),
      switchMap(() => this.sprintService.getSprints(this.projectId))
    )
  );

  backlog = toSignal(
    this.backlogRefresh$.pipe(
      startWith(null),
      switchMap(() => this.workItemsService.getBacklog(this.projectId))
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
    return ({ Low: 'Baja', Medium: 'Media', High: 'Alta', VeryHigh: 'Muy alta' } as Record<string, string>)[c] ?? c;
  }

  statusColor(s: WorkItemStatus): string { return STATUS_COLORS[s]; }
  statusLabel(s: WorkItemStatus): string { return STATUS_LABELS[s]; }
  priorityColor(p: WorkItemPriority): string { return PRIORITY_COLORS[p]; }

  goBack(): void { this.router.navigate(['/projects']); }

  transition(status: ProjectStatus): void {
    this.service.transitionStatus(this.projectIdStr, status).subscribe({
      next: () => { this.message.success('Estado actualizado'); this.refresh$.next(); },
      error: () => this.message.error('No se pudo cambiar el estado'),
    });
  }

  removeTeam(team: ProjectTeam): void {
    this.service.removeTeam(this.projectIdStr, String(team.teamId)).subscribe({
      next: () => { this.message.success(`Equipo "${team.teamName}" desasignado`); this.refresh$.next(); },
      error: () => this.message.error('Error al desasignar el equipo'),
    });
  }

  openEdit(): void { this.formVisible.set(true); }
  onSaved(): void { this.formVisible.set(false); this.refresh$.next(); }

  openEpicForm(epic?: Epic): void {
    this.editingEpic.set(epic ?? null);
    this.epicForm = epic
      ? { title: epic.title, description: epic.description, priority: epic.priority, sortOrder: epic.sortOrder }
      : { title: '', priority: 0, sortOrder: 0 };
    this.epicModalVisible.set(true);
  }

  saveEpic(): void {
    const editing = this.editingEpic();
    const onSuccess = () => {
      this.epicModalVisible.set(false);
      this.message.success(editing ? 'Épica actualizada' : 'Épica creada');
      this.epicsRefresh$.next();
    };
    const onError = () => this.message.error('Error al guardar la épica');

    if (editing) {
      this.epicsService.updateEpic(this.projectId, editing.id, this.epicForm).subscribe({ next: onSuccess, error: onError });
    } else {
      this.epicsService.createEpic(this.projectId, this.epicForm).subscribe({ next: onSuccess, error: onError });
    }
  }

  deleteEpic(epic: Epic): void {
    this.epicsService.deleteEpic(this.projectId, epic.id).subscribe({
      next: () => { this.message.success('Épica eliminada'); this.epicsRefresh$.next(); },
      error: () => this.message.error('Error al eliminar la épica'),
    });
  }

  openSprintForm(sprint?: Sprint): void {
    this.editingSprint.set(sprint ?? null);
    this.sprintForm = sprint
      ? { name: sprint.name, goal: sprint.goal, startDate: sprint.startDate, endDate: sprint.endDate, capacity: sprint.capacity }
      : { name: '' };
    this.sprintModalVisible.set(true);
  }

  private formatDate(d: unknown): string | undefined {
    if (!d) return undefined;
    if (d instanceof Date) return d.toISOString().split('T')[0];
    return d as string;
  }

  formatCommentDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  saveSprint(): void {
    const editing = this.editingSprint();
    const dto: CreateSprintDto = {
      ...this.sprintForm,
      startDate: this.formatDate(this.sprintForm.startDate),
      endDate: this.formatDate(this.sprintForm.endDate),
    };
    const onSuccess = () => {
      this.sprintModalVisible.set(false);
      this.message.success(editing ? 'Sprint actualizado' : 'Sprint creado');
      this.sprintsRefresh$.next();
    };
    const onError = () => this.message.error('Error al guardar el sprint');

    if (editing) {
      this.sprintService.updateSprint(this.projectId, editing.id, dto).subscribe({ next: onSuccess, error: onError });
    } else {
      this.sprintService.createSprint(this.projectId, dto).subscribe({ next: onSuccess, error: onError });
    }
  }

  deleteSprint(sprint: Sprint): void {
    this.sprintService.deleteSprint(this.projectId, sprint.id).subscribe({
      next: () => { this.message.success('Sprint eliminado'); this.sprintsRefresh$.next(); },
      error: () => this.message.error('Error al eliminar el sprint'),
    });
  }

  transitionSprint(sprint: Sprint, status: 'Active' | 'Completed'): void {
    this.sprintService.transitionStatus(this.projectId, sprint.id, status).subscribe({
      next: () => { this.message.success('Estado del sprint actualizado'); this.sprintsRefresh$.next(); },
      error: () => this.message.error('Error al actualizar el sprint'),
    });
  }

  openWorkItemForm(wi?: WorkItem): void {
    this.editingWorkItem.set(wi ?? null);
    this.workItemForm = wi
      ? { title: wi.title, description: wi.description, priority: wi.priority, epicId: wi.epicId, sprintId: wi.sprintId, assigneeIds: wi.assignees.map(a => a.id), estimationHours: wi.estimationHours, estimationPoints: wi.estimationPoints, sortOrder: wi.sortOrder, isHito: wi.isHito, hitoDate: wi.hitoDate, dueDate: wi.dueDate }
      : { title: '', priority: 'Medium', sortOrder: 0, isHito: false, assigneeIds: [] };
    this.workItemModalVisible.set(true);
  }

  saveWorkItem(): void {
    const editing = this.editingWorkItem();
    const dto = {
      ...this.workItemForm,
      dueDate: this.formatDate(this.workItemForm.dueDate),
      hitoDate: this.formatDate(this.workItemForm.hitoDate),
    };
    const onSuccess = () => {
      this.workItemModalVisible.set(false);
      this.message.success(editing ? 'Tarea actualizada' : 'Tarea creada');
      this.backlogRefresh$.next();
    };
    const onError = () => this.message.error('Error al guardar la tarea');

    if (editing) {
      this.workItemsService.updateWorkItem(this.projectId, editing.id, dto).subscribe({ next: onSuccess, error: onError });
    } else {
      this.workItemsService.createWorkItem(this.projectId, dto).subscribe({ next: onSuccess, error: onError });
    }
  }

  deleteWorkItem(wi: WorkItem): void {
    this.workItemsService.deleteWorkItem(this.projectId, wi.id).subscribe({
      next: () => { this.message.success('Tarea eliminada'); this.backlogRefresh$.next(); },
      error: () => this.message.error('Error al eliminar la tarea'),
    });
  }

  openAssignSprintModal(wi: WorkItem): void {
    this.selectedWorkItemForAssign = wi;
    this.assignSprintId = wi.sprintId ?? null;
    this.assignSprintModalVisible.set(true);
  }

  saveAssignSprint(): void {
    if (!this.selectedWorkItemForAssign) return;
    this.sprintService.assignToSprint(this.projectId, this.selectedWorkItemForAssign.id, this.assignSprintId).subscribe({
      next: () => {
        this.assignSprintModalVisible.set(false);
        this.message.success('Tarea asignada a sprint');
        this.backlogRefresh$.next();
      },
      error: () => this.message.error('Error al asignar tarea a sprint'),
    });
  }

  openComments(wi: WorkItem): void {
    this.commentsWorkItem.set(wi);
    this.newCommentText = '';
    this.commentsModalVisible.set(true);
    this.loadComments(wi);
  }

  private loadComments(wi: WorkItem): void {
    this.commentsLoading.set(true);
    this.commentsService.getComments(this.projectId, wi.id).subscribe({
      next: (list) => { this.comments.set(list); this.commentsLoading.set(false); },
      error: () => { this.commentsLoading.set(false); this.message.error('Error al cargar comentarios'); },
    });
  }

  addComment(): void {
    const wi = this.commentsWorkItem();
    if (!wi || !this.newCommentText.trim()) return;
    this.addingComment.set(true);
    this.commentsService.createComment(this.projectId, wi.id, this.newCommentText.trim()).subscribe({
      next: () => {
        this.newCommentText = '';
        this.addingComment.set(false);
        this.loadComments(wi);
      },
      error: () => { this.addingComment.set(false); this.message.error('Error al publicar comentario'); },
    });
  }

  deleteComment(comment: CommentDto): void {
    const wi = this.commentsWorkItem();
    if (!wi) return;
    this.commentsService.deleteComment(this.projectId, wi.id, comment.id).subscribe({
      next: () => this.loadComments(wi),
      error: () => this.message.error('Error al eliminar comentario'),
    });
  }
}
