import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, Subject, startWith } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
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
import { NzModalService } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { HttpClient } from '@angular/common/http';
import { ProjectsService, ProjectStatusHistoryEntry } from '../projects.service';
import { EpicsService, Epic, CreateEpicDto } from '../epics.service';
import { WorkItemsService, WorkItem, WorkItemStatus, WorkItemPriority, WorkItemType, WORK_ITEM_TYPE_LABELS, WorkItemStatusHistoryEntry } from '../workitems.service';
import { SprintService, Sprint, CreateSprintDto, SprintStatusHistoryEntry } from '../sprint.service';
import { CommentsService, CommentDto } from '../comments.service';
import { RisksService } from '../risks.service';
import {
  PROJECT_HEALTH_STATUS_COLORS,
  PROJECT_HEALTH_STATUS_LABELS,
  PROJECT_STATUS_LABELS,
  ProjectDetail,
  ProjectHealthStatus,
  ProjectNoteDto,
  ProjectStatus,
  ProjectTeam,
  ProjectWeeklyUpdateDto,
  UpsertWeeklyUpdateDto,
  ProjectRiskDto,
  ProjectDependenciesDto,
  DependencyItemDto,
  RiskLevel,
  RiskStatus,
  RISK_LEVEL_LABELS,
  RISK_STATUS_LABELS,
  RISK_STATUS_COLORS,
  CreateRiskDto,
  UpdateRiskDto,
} from '../project.model';
import { ProjectStatusBadgeComponent } from '../project-status-badge/project-status-badge.component';
import { ComplexityIndicatorComponent } from '../complexity-indicator/complexity-indicator.component';
import { ProjectFormComponent } from '../project-form/project-form.component';
import { ProductBacklogComponent } from './product-backlog.component';
import { WorkItemDrawerComponent } from '../work-item-drawer/work-item-drawer.component';

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
    FormsModule, DecimalPipe,
    NzCardModule, NzButtonModule, NzDescriptionsModule, NzTableModule,
    NzPopconfirmModule, NzSpaceModule, NzDividerModule, NzIconModule,
    NzSpinModule, NzTabsModule, NzTagModule, NzModalModule, NzFormModule,
    NzInputModule, NzInputNumberModule, NzSelectModule, NzDatePickerModule,
    NzListModule, NzAvatarModule, NzRadioModule, NzEmptyModule, NzProgressModule,
    RouterLink, ProjectStatusBadgeComponent, ComplexityIndicatorComponent, ProjectFormComponent,
    ProductBacklogComponent, WorkItemDrawerComponent,
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
            @for (t of project()!.tags; track t.id) {
              <nz-tag [nzColor]="t.color ?? 'default'" style="margin-left:4px">{{ t.name }}</nz-tag>
            }
          </div>
          <nz-space>
            <nz-select *nzSpaceItem [ngModel]="project()!.status"
              (ngModelChange)="transition($event)"
              style="width:220px" nzPlaceHolder="Cambiar estado"
              [nzDisabled]="project()!.allowedNextStatuses.length === 0">
              <nz-option [nzValue]="project()!.status" [nzLabel]="statusOptionLabel(project()!.status)" [nzDisabled]="true" />
              @for (opt of allowedStatusOptions(); track opt.value) {
                <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
              }
            </nz-select>
            <a *nzSpaceItem nz-button [routerLink]="['/projects', projectId, 'kanban']">
              <span nz-icon nzType="project"></span> Kanban
            </a>
            <a *nzSpaceItem nz-button [routerLink]="['/projects', projectId, 'report']">
              <span nz-icon nzType="bar-chart"></span> Informe
            </a>
            <button *nzSpaceItem nz-button nzType="text" nzSize="small" (click)="openProjectHistory()"
              nz-tooltip nzTooltipTitle="Histórico de estados">
              <span nz-icon nzType="history"></span>
            </button>
            <button *nzSpaceItem nz-button (click)="openEdit()">
              <span nz-icon nzType="edit"></span> Editar
            </button>
          </nz-space>
        </div>

        <!-- Tabs -->
        <nz-tabs>

          <!-- TAB: Info -->
          <nz-tab nzTitle="Información">
            <nz-card nzTitle="Datos del proyecto" style="margin-bottom:16px">
              <nz-descriptions nzBordered [nzColumn]="2">
                <nz-descriptions-item nzTitle="Complejidad"><app-complexity-indicator [complexity]="project()!.complexity" size="card" /></nz-descriptions-item>
                <nz-descriptions-item nzTitle="Estado"><app-project-status-badge [status]="project()!.status" /></nz-descriptions-item>
                <nz-descriptions-item nzTitle="Año de cartera">{{ project()!.portfolioYear ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Ref. anterior">{{ project()!.previousReferenceId ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Fecha de inicio">{{ project()!.startDate ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Fecha de fin">{{ project()!.endDate ?? '—' }}</nz-descriptions-item>
                @if (project()!.description) {
                  <nz-descriptions-item nzTitle="Descripción" [nzSpan]="2">{{ project()!.description }}</nz-descriptions-item>
                }
              </nz-descriptions>
            </nz-card>

            <nz-card nzTitle="Clasificación y gobernanza" style="margin-bottom:16px">
              <nz-descriptions nzBordered [nzColumn]="2">
                <nz-descriptions-item nzTitle="Promotor">{{ project()!.promoterName ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Unidad orgánica">{{ project()!.organicUnitName ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Grupo SIPT">{{ project()!.siptGroup ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Prioridad estratégica">{{ project()!.groupPriority ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Orden UOR">{{ project()!.uorOrder ?? '—' }}</nz-descriptions-item>
                <nz-descriptions-item nzTitle="Nº beneficiarios">{{ project()!.beneficiaryCount ?? '—' }}</nz-descriptions-item>
                @if (project()!.estimatedBudget !== null && project()!.estimatedBudget !== undefined) {
                  <nz-descriptions-item nzTitle="Presupuesto estimado">{{ project()!.estimatedBudget | number }} €</nz-descriptions-item>
                }
                @if (project()!.specificationsUrl) {
                  <nz-descriptions-item nzTitle="Especificaciones" [nzSpan]="2">
                    <a [href]="project()!.specificationsUrl" target="_blank">{{ project()!.specificationsUrl }}</a>
                  </nz-descriptions-item>
                }
                @if (project()!.epicUrl) {
                  <nz-descriptions-item nzTitle="Épica (Jira)" [nzSpan]="2">
                    <a [href]="project()!.epicUrl" target="_blank">{{ project()!.epicUrl }}</a>
                  </nz-descriptions-item>
                }
                @if (project()!.desiredDeploymentDate) {
                  <nz-descriptions-item nzTitle="Fecha deseable implantación">{{ project()!.desiredDeploymentDate }}</nz-descriptions-item>
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
              <thead><tr><th>Título</th><th>Prioridad</th><th>Tareas</th><th>Progreso</th><th>Orden</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (epic of epics()?.items ?? []; track epic.id) {
                  <tr>
                    <td>{{ epic.title }}</td>
                    <td>{{ epic.priority }}</td>
                    <td>{{ epic.workItemCount }}</td>
                    <td style="min-width:120px">
                      @if (epic.workItemCount > 0) {
                        <div style="display:flex;align-items:center;gap:8px">
                          <nz-progress
                            style="flex:1"
                            [nzPercent]="epicProgress(epic)"
                            nzSize="small"
                            [nzShowInfo]="false"
                            nzStrokeColor="#1C7A4B">
                          </nz-progress>
                          <span style="font-size:12px;color:#8c8c8c;white-space:nowrap">
                            {{ epic.doneWorkItemCount }}/{{ epic.workItemCount }}
                          </span>
                        </div>
                      } @else {
                        <span style="color:#bfbfbf;font-size:12px">—</span>
                      }
                    </td>
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
                  <tr><td colspan="6" style="text-align:center;color:#999">Sin épicas</td></tr>
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
              <thead><tr><th>Nombre</th><th>Estado</th><th>Fechas</th><th>Pts / Capacidad</th><th>Tareas</th><th>Horas</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (sprint of sprints()?.items ?? []; track sprint.id) {
                  <tr>
                    <td>{{ sprint.name }}</td>
                    <td><nz-tag [nzColor]="SPRINT_STATUS_COLORS[sprint.status]">{{ sprint.status }}</nz-tag></td>
                    <td>{{ sprint.startDate ?? '—' }} / {{ sprint.endDate ?? '—' }}</td>
                    <td>
                      @if (sprint.status === 'Completed' && sprint.committedPoints !== undefined && sprint.deliveredPoints !== undefined) {
                        <span style="font-size:12px;color:#595959">
                          comprometidos <strong>{{ sprint.committedPoints }}</strong>
                          &nbsp;·&nbsp; entregados <strong style="color:#1C7A4B">{{ sprint.deliveredPoints }}</strong>
                        </span>
                      } @else if (sprint.status !== 'Completed') {
                        <span
                          [style.color]="sprint.capacity && sprint.totalEstimationPoints > sprint.capacity ? '#ff4d4f' : 'inherit'"
                          style="font-size:13px;font-weight:500">
                          {{ sprint.totalEstimationPoints }} pts
                          @if (sprint.capacity) {
                            <span style="font-weight:400;color:#8c8c8c"> / cap {{ sprint.capacity }}</span>
                          }
                        </span>
                      } @else {
                        <span style="color:#8c8c8c">{{ sprint.totalEstimationPoints }} pts</span>
                      }
                    </td>
                    <td>{{ sprint.workItemCount }}</td>
                    <td>{{ sprint.totalEstimationHours }}</td>
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
                          <button *nzSpaceItem nz-button nzSize="small" nzType="primary"
                            (click)="transitionSprint(sprint, 'Active')">
                            Iniciar
                          </button>
                        }
                        @if (sprint.status === 'Active') {
                          <button *nzSpaceItem nz-button nzSize="small" nzType="primary"
                            (click)="completeSprint(sprint)">
                            Completar
                          </button>
                        }
                        <a *nzSpaceItem [routerLink]="['/projects', projectId, 'sprints', sprint.id, 'kanban']"
                          nz-button nzSize="small">
                          <span nz-icon nzType="eye"></span> Kanban
                        </a>
                        <button *nzSpaceItem nz-button nzSize="small" (click)="openSprintHistory(sprint)">
                          <span nz-icon nzType="clock-circle"></span> Histórico
                        </button>
                      </nz-space>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="7" style="text-align:center;color:#999">Sin sprints</td></tr>
                }
              </tbody>
            </nz-table>
          </nz-tab>

          <!-- TAB: Notas -->
          <nz-tab nzTitle="Notas" (nzSelect)="loadNotes()">
            <div style="max-width:700px;margin:16px auto">
              @if (notesLoading()) {
                <div style="text-align:center;padding:32px"><nz-spin /></div>
              } @else {
                @for (note of notes(); track note.id) {
                  <div style="display:flex;gap:12px;margin-bottom:16px">
                    <nz-avatar [nzText]="note.authorName[0]" style="background:#1890ff;flex-shrink:0"></nz-avatar>
                    <div style="flex:1">
                      <div style="display:flex;align-items:baseline;gap:8px;margin-bottom:4px">
                        <span style="font-weight:600">{{ note.authorName }}</span>
                        <span style="font-size:12px;color:#8c8c8c">{{ formatCommentDate(note.createdAt) }}</span>
                        <span style="font-size:12px;color:#ff4d4f;cursor:pointer;margin-left:auto"
                          nz-popconfirm nzPopconfirmTitle="¿Eliminar esta nota?"
                          (nzOnConfirm)="deleteNote(note)">Eliminar</span>
                      </div>
                      <p style="margin:0;white-space:pre-wrap">{{ note.text }}</p>
                    </div>
                  </div>
                  <nz-divider style="margin:8px 0"></nz-divider>
                } @empty {
                  <p style="color:#999;text-align:center">Sin notas aún.</p>
                }
                <div style="margin-top:16px">
                  <textarea nz-input [(ngModel)]="newNoteText" [nzAutosize]="{ minRows: 3, maxRows: 6 }"
                    placeholder="Añadir una nota..." style="margin-bottom:8px"></textarea>
                  <button nz-button nzType="primary" [nzLoading]="addingNote()"
                    [disabled]="!newNoteText.trim()" (click)="addNote()">
                    Publicar nota
                  </button>
                </div>
              }
            </div>
          </nz-tab>

          <!-- TAB: Avance Semanal -->
          <nz-tab nzTitle="Avance Semanal" (nzSelect)="loadWeeklyUpdates()">
            <div style="max-width:700px;margin:16px auto">
              @if (weeklyUpdatesLoading()) {
                <div style="text-align:center;padding:32px"><nz-spin /></div>
              } @else {
                @for (wu of weeklyUpdates(); track wu.id) {
                  <div style="display:flex;gap:12px;margin-bottom:16px">
                    <nz-avatar [nzText]="wu.authorName[0]" style="background:#1890ff;flex-shrink:0"></nz-avatar>
                    <div style="flex:1">
                      <div style="display:flex;align-items:baseline;gap:8px;margin-bottom:4px;flex-wrap:wrap">
                        <span style="font-weight:600">{{ wu.authorName }}</span>
                        <span style="font-size:12px;color:#8c8c8c">Semana del {{ formatWeekOf(wu.weekOf) }}</span>
                        <nz-tag [nzColor]="HEALTH_STATUS_COLORS[wu.healthStatus]">{{ HEALTH_STATUS_LABELS[wu.healthStatus] }}</nz-tag>
                      </div>
                      <p style="margin:0;white-space:pre-wrap">{{ wu.summary }}</p>
                    </div>
                  </div>
                  <nz-divider style="margin:8px 0"></nz-divider>
                } @empty {
                  <p style="color:#999;text-align:center">Sin actualizaciones aún.</p>
                }
                <div style="margin-top:16px">
                  <div style="margin-bottom:8px">
                    <nz-select [(ngModel)]="newWeeklyUpdateHealthStatus" style="width:200px">
                      <nz-option nzValue="OnTrack" nzLabel="🟢 En curso"></nz-option>
                      <nz-option nzValue="AtRisk" nzLabel="🟡 En riesgo"></nz-option>
                      <nz-option nzValue="Blocked" nzLabel="🔴 Bloqueado"></nz-option>
                    </nz-select>
                  </div>
                  <textarea nz-input [(ngModel)]="newWeeklyUpdateSummary" [nzAutosize]="{ minRows: 3, maxRows: 6 }"
                    [maxlength]="1000" placeholder="Describe el avance de esta semana..." style="margin-bottom:4px"></textarea>
                  <div style="text-align:right;font-size:12px;color:#8c8c8c;margin-bottom:8px">
                    {{ newWeeklyUpdateSummary.length }}/1000
                  </div>
                  <button nz-button nzType="primary" [nzLoading]="savingWeeklyUpdate()"
                    [disabled]="!newWeeklyUpdateSummary.trim()" (click)="saveWeeklyUpdate()">
                    Guardar avance
                  </button>
                </div>
              }
            </div>
          </nz-tab>

          <!-- TAB: Riesgos -->
          <nz-tab nzTitle="Riesgos" (nzSelect)="loadRisks()">
            <div style="margin-bottom:12px;text-align:right">
              <button nz-button nzType="primary" (click)="openRiskForm()">
                <span nz-icon nzType="plus"></span> Añadir riesgo
              </button>
            </div>
            @if (risksLoading()) {
              <div style="text-align:center;padding:32px"><nz-spin /></div>
            } @else {
              <nz-table [nzData]="risks()" nzBordered nzSize="small" [nzShowPagination]="false">
                <thead>
                  <tr>
                    <th>Descripción</th>
                    <th>Probabilidad</th>
                    <th>Impacto</th>
                    <th>Severidad</th>
                    <th>Estado</th>
                    <th>Plan de mitigación</th>
                    <th>Autor</th>
                    <th>Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  @for (risk of risks(); track risk.id) {
                    <tr>
                      <td>{{ risk.description }}</td>
                      <td>{{ RISK_LEVEL_LABELS[risk.probability] }}</td>
                      <td>{{ RISK_LEVEL_LABELS[risk.impact] }}</td>
                      <td>
                        <nz-tag [nzColor]="severityColor(risk.severity)">{{ risk.severity }}</nz-tag>
                      </td>
                      <td>
                        <nz-tag [nzColor]="RISK_STATUS_COLORS[risk.status]">{{ RISK_STATUS_LABELS[risk.status] }}</nz-tag>
                      </td>
                      <td style="max-width:200px;white-space:pre-wrap;font-size:12px;color:#595959">
                        {{ risk.mitigationPlan ?? '—' }}
                      </td>
                      <td style="font-size:12px">{{ risk.createdByName }}</td>
                      <td>
                        <nz-space nzSize="small">
                          <button *nzSpaceItem nz-button nzSize="small" (click)="openRiskForm(risk)">
                            <span nz-icon nzType="edit"></span>
                          </button>
                          <button *nzSpaceItem nz-button nzSize="small" nzDanger nz-popconfirm
                            nzPopconfirmTitle="¿Eliminar este riesgo?" (nzOnConfirm)="deleteRisk(risk)">
                            <span nz-icon nzType="delete"></span>
                          </button>
                        </nz-space>
                      </td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="8" style="text-align:center;padding:24px">
                        <nz-empty nzNotFoundContent="Sin riesgos registrados" />
                      </td>
                    </tr>
                  }
                </tbody>
              </nz-table>
            }
          </nz-tab>

          <!-- TAB: Dependencias -->
          <nz-tab nzTitle="Dependencias" (nzSelect)="loadDependencies()">
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-top:8px">

              <!-- Depende de -->
              <div>
                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px">
                  <span style="font-weight:600;font-size:14px">Este proyecto depende de:</span>
                  <button nz-button nzSize="small" nzType="primary" (click)="openAddDependencyModal()">
                    <span nz-icon nzType="plus"></span> Añadir
                  </button>
                </div>
                @if (dependenciesLoading()) {
                  <div style="text-align:center;padding:16px"><nz-spin /></div>
                } @else if (dependencies()?.dependsOn?.length === 0) {
                  <nz-empty nzNotFoundContent="Sin dependencias" style="padding:16px" />
                } @else {
                  @for (dep of dependencies()?.dependsOn ?? []; track dep.dependencyId) {
                    <div style="display:flex;align-items:center;justify-content:space-between;padding:10px 12px;background:#fafafa;border:1px solid #f0f0f0;border-radius:8px;margin-bottom:8px">
                      <div style="flex:1;min-width:0">
                        <a [routerLink]="['/projects', dep.projectId]" style="font-weight:600;font-size:13px">{{ dep.projectTitle }}</a>
                        <div style="margin-top:4px"><app-project-status-badge [status]="dep.projectStatus" /></div>
                        @if (dep.description) {
                          <p style="font-size:12px;color:#8c8c8c;margin:4px 0 0">{{ dep.description }}</p>
                        }
                      </div>
                      <button nz-button nzSize="small" nzDanger nz-popconfirm
                        nzPopconfirmTitle="¿Eliminar esta dependencia?" (nzOnConfirm)="deleteDependency(dep)"
                        style="margin-left:8px;flex-shrink:0">
                        <span nz-icon nzType="delete"></span>
                      </button>
                    </div>
                  }
                }
              </div>

              <!-- Dependientes -->
              <div>
                <div style="font-weight:600;font-size:14px;margin-bottom:8px">Proyectos que dependen de este:</div>
                @if (dependenciesLoading()) {
                  <div style="text-align:center;padding:16px"><nz-spin /></div>
                } @else if (dependencies()?.dependents?.length === 0) {
                  <nz-empty nzNotFoundContent="Ningún proyecto depende de este" style="padding:16px" />
                } @else {
                  @for (dep of dependencies()?.dependents ?? []; track dep.dependencyId) {
                    <div style="padding:10px 12px;background:#fafafa;border:1px solid #f0f0f0;border-radius:8px;margin-bottom:8px">
                      <a [routerLink]="['/projects', dep.projectId]" style="font-weight:600;font-size:13px">{{ dep.projectTitle }}</a>
                      <div style="margin-top:4px"><app-project-status-badge [status]="dep.projectStatus" /></div>
                      @if (dep.description) {
                        <p style="font-size:12px;color:#8c8c8c;margin:4px 0 0">{{ dep.description }}</p>
                      }
                    </div>
                  }
                }
              </div>
            </div>
          </nz-tab>

          <!-- TAB: Product Backlog -->
          <nz-tab nzTitle="Product Backlog">
            <div style="margin-bottom:12px;text-align:right">
              <button nz-button nzType="primary" (click)="openWorkItemForm()">
                <span nz-icon nzType="plus"></span> Nueva tarea
              </button>
            </div>
            <app-product-backlog
              [projectId]="projectId"
              [epics]="epics()?.items ?? []"
              [sprints]="sprints()?.items ?? []"
              [persons]="persons()?.items ?? []"
              (openForm)="openWorkItemForm($event)"
              (openComments)="openComments($event)"
              (openAssignSprint)="openAssignSprintModal($event)"
              (openDrawer)="openDrawerItem($event)"
              (refreshRequested)="backlogRefresh$.next()"
            />
          </nz-tab>

        </nz-tabs>
      </div>
    }

    <!-- Work-item drawer -->
    <app-work-item-drawer
      [workItem]="drawerWorkItem()"
      (closed)="drawerWorkItem.set(null)"
      (changed)="onDrawerChanged()"
    />

    <!-- Modal editar proyecto -->
    <app-project-form
      [visible]="formVisible()"
      [project]="project() ?? null"
      (saved)="onSaved()"
      (cancelled)="formVisible.set(false)"
    />

    <!-- Modal riesgo -->
    <nz-modal
      [nzVisible]="riskModalVisible()"
      [nzTitle]="editingRisk() ? 'Editar riesgo' : 'Nuevo riesgo'"
      (nzOnCancel)="riskModalVisible.set(false)"
      (nzOnOk)="saveRisk()"
    >
      <ng-container *nzModalContent>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Descripción</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <textarea nz-input [(ngModel)]="riskForm.description" [nzAutosize]="{ minRows: 2 }" placeholder="Descripción del riesgo"></textarea>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Probabilidad</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="riskForm.probability" style="width:100%">
              <nz-option nzValue="Low" nzLabel="Baja"></nz-option>
              <nz-option nzValue="Medium" nzLabel="Media"></nz-option>
              <nz-option nzValue="High" nzLabel="Alta"></nz-option>
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Impacto</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="riskForm.impact" style="width:100%">
              <nz-option nzValue="Low" nzLabel="Bajo"></nz-option>
              <nz-option nzValue="Medium" nzLabel="Medio"></nz-option>
              <nz-option nzValue="High" nzLabel="Alto"></nz-option>
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        @if (editingRisk()) {
          <nz-form-item>
            <nz-form-label [nzSpan]="6">Estado</nz-form-label>
            <nz-form-control [nzSpan]="18">
              <nz-select [(ngModel)]="riskFormStatus" style="width:100%">
                <nz-option nzValue="Open" nzLabel="Abierto"></nz-option>
                <nz-option nzValue="Mitigated" nzLabel="Mitigado"></nz-option>
                <nz-option nzValue="Closed" nzLabel="Cerrado"></nz-option>
              </nz-select>
            </nz-form-control>
          </nz-form-item>
        }
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Mitigación</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <textarea nz-input [(ngModel)]="riskForm.mitigationPlan" [nzAutosize]="{ minRows: 2 }" placeholder="Plan de mitigación (opcional)"></textarea>
          </nz-form-control>
        </nz-form-item>
      </ng-container>
    </nz-modal>

    <!-- Modal añadir dependencia -->
    <nz-modal
      [nzVisible]="addDependencyModalVisible()"
      nzTitle="Añadir dependencia"
      (nzOnCancel)="addDependencyModalVisible.set(false)"
      (nzOnOk)="saveAddDependency()"
    >
      <ng-container *nzModalContent>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Proyecto</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select
              [(ngModel)]="addDepProjectId"
              nzShowSearch
              nzServerSearch
              (nzOnSearch)="searchDependencyProjects($event)"
              nzPlaceHolder="Buscar proyecto..."
              style="width:100%">
              @for (p of dependencyProjectSearch(); track p.id) {
                <nz-option [nzValue]="p.id" [nzLabel]="p.title"></nz-option>
              }
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label [nzSpan]="6">Descripción</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <textarea nz-input [(ngModel)]="addDepDescription" [nzAutosize]="{ minRows: 2 }" placeholder="Descripción opcional"></textarea>
          </nz-form-control>
        </nz-form-item>
      </ng-container>
    </nz-modal>

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
          <nz-form-label [nzSpan]="6">Tipo</nz-form-label>
          <nz-form-control [nzSpan]="18">
            <nz-select [(ngModel)]="workItemForm.type" style="width:100%">
              <nz-option nzValue="Task" nzLabel="Tarea"></nz-option>
              <nz-option nzValue="UserStory" nzLabel="Historia de usuario"></nz-option>
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
        @if (editingWorkItem()) {
          <nz-form-item>
            <nz-form-label [nzSpan]="6">Estado</nz-form-label>
            <nz-form-control [nzSpan]="18">
              <div style="display:flex;gap:8px;align-items:center">
                <nz-select [(ngModel)]="workItemForm.status"
                  [nzDisabled]="editingWorkItem()!.status === 'Done' || editingWorkItem()!.status === 'Discarded'" style="flex:1">
                  <nz-option nzValue="Backlog" nzLabel="Backlog"></nz-option>
                  <nz-option nzValue="ToDo" nzLabel="Por hacer"></nz-option>
                  <nz-option nzValue="InProgress" nzLabel="En curso"></nz-option>
                  <nz-option nzValue="Blocked" nzLabel="Bloqueada"></nz-option>
                  <nz-option nzValue="Done" nzLabel="Hecho"></nz-option>
                </nz-select>
                <button nz-button nzSize="small" (click)="openWorkItemHistory(editingWorkItem()!)">
                  <span nz-icon nzType="clock-circle"></span>
                </button>
              </div>
            </nz-form-control>
          </nz-form-item>
        }
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

    <!-- Modal histórico de estado de tarea -->
    <nz-modal
      [nzVisible]="workItemHistoryModalVisible()"
      nzTitle="Histórico de estados"
      [nzFooter]="null"
      (nzOnCancel)="workItemHistoryModalVisible.set(false)"
    >
      <ng-container *nzModalContent>
        @if (workItemHistoryLoading()) {
          <div style="text-align:center;padding:24px"><nz-spin /></div>
        } @else if (workItemHistory().length === 0) {
          <p style="color:#999;text-align:center">Sin histórico.</p>
        } @else {
          <nz-table [nzData]="workItemHistory()" nzSize="small" [nzShowPagination]="false">
            <thead><tr><th>De</th><th>A</th><th>Quién</th><th>Cuándo</th></tr></thead>
            <tbody>
              @for (h of workItemHistory(); track h.id) {
                <tr>
                  <td>{{ h.fromStatus ? statusLabel(h.fromStatus) : '—' }}</td>
                  <td><nz-tag [nzColor]="statusColor(h.toStatus)">{{ statusLabel(h.toStatus) }}</nz-tag></td>
                  <td>{{ h.changedByName }}</td>
                  <td>{{ formatCommentDate(h.changedAt) }}</td>
                </tr>
              }
            </tbody>
          </nz-table>
        }
      </ng-container>
    </nz-modal>

    <!-- Modal histórico de estado de sprint -->
    <nz-modal
      [nzVisible]="sprintHistoryModalVisible()"
      nzTitle="Histórico de estados del sprint"
      [nzFooter]="null"
      (nzOnCancel)="sprintHistoryModalVisible.set(false)"
    >
      <ng-container *nzModalContent>
        @if (sprintHistoryLoading()) {
          <div style="text-align:center;padding:24px"><nz-spin /></div>
        } @else if (sprintHistory().length === 0) {
          <p style="color:#999;text-align:center">Sin histórico.</p>
        } @else {
          <nz-table [nzData]="sprintHistory()" nzSize="small" [nzShowPagination]="false">
            <thead><tr><th>De</th><th>A</th><th>Quién</th><th>Cuándo</th></tr></thead>
            <tbody>
              @for (h of sprintHistory(); track h.id) {
                <tr>
                  <td>{{ h.fromStatus ?? '—' }}</td>
                  <td><nz-tag [nzColor]="SPRINT_STATUS_COLORS[h.toStatus]">{{ h.toStatus }}</nz-tag></td>
                  <td>{{ h.changedByName }}</td>
                  <td>{{ formatCommentDate(h.changedAt) }}</td>
                </tr>
              }
            </tbody>
          </nz-table>
        }
      </ng-container>
    </nz-modal>

    <!-- Modal histórico de estado del proyecto -->
    <nz-modal
      [nzVisible]="projectHistoryModalVisible()"
      nzTitle="Histórico de estados del proyecto"
      [nzFooter]="null"
      (nzOnCancel)="projectHistoryModalVisible.set(false)"
    >
      <ng-container *nzModalContent>
        @if (projectHistoryLoading()) {
          <div style="text-align:center;padding:24px"><nz-spin /></div>
        } @else if (projectHistory().length === 0) {
          <p style="color:#999;text-align:center">Sin histórico.</p>
        } @else {
          <nz-table [nzData]="projectHistory()" nzSize="small" [nzShowPagination]="false">
            <thead><tr><th>De</th><th>A</th><th>Quién</th><th>Cuándo</th></tr></thead>
            <tbody>
              @for (h of projectHistory(); track h.id) {
                <tr>
                  <td>{{ h.fromStatus ? PROJECT_STATUS_LABELS[h.fromStatus] : '—' }}</td>
                  <td><nz-tag>{{ PROJECT_STATUS_LABELS[h.toStatus] }}</nz-tag></td>
                  <td>{{ h.changedByName }}</td>
                  <td>{{ formatCommentDate(h.changedAt) }}</td>
                </tr>
              }
            </tbody>
          </nz-table>
        }
      </ng-container>
    </nz-modal>

    <!-- Modal cerrar sprint (carry-over) -->
    <nz-modal
      [nzVisible]="carryOverModalVisible()"
      nzTitle="Cerrar sprint"
      [nzOkText]="'Completar'"
      [nzOkLoading]="carryOverLoading()"
      (nzOnOk)="confirmCompleteSprint()"
      (nzOnCancel)="carryOverModalVisible.set(false)"
    >
      <ng-container *nzModalContent>
        <p style="margin-bottom:16px">
          El sprint tiene <strong>{{ unfinishedItemsCount() }}</strong> tarea(s) sin terminar.
          ¿Qué deseas hacer con ellas?
        </p>
        <nz-radio-group [(ngModel)]="carryOverChoice" nzButtonStyle="solid">
          <label nz-radio nzValue="Backlog" style="display:block;margin-bottom:12px">
            Devolver al backlog del proyecto
          </label>
          <label nz-radio nzValue="Sprint"
            [nzDisabled]="planningSprintsForCarryOver().length === 0"
            style="display:block;margin-bottom:8px">
            Mover al sprint...
            @if (planningSprintsForCarryOver().length === 0) {
              <span style="font-size:11px;color:#8c8c8c;margin-left:6px">(no hay sprints en Planning)</span>
            }
          </label>
        </nz-radio-group>
        @if (carryOverChoice === 'Sprint') {
          <div style="margin-top:8px;margin-left:24px">
            <nz-select [(ngModel)]="carryOverTargetSprintId" style="width:100%"
              nzPlaceHolder="Seleccionar sprint destino">
              @for (s of planningSprintsForCarryOver(); track s.id) {
                <nz-option [nzValue]="s.id" [nzLabel]="s.name" />
              }
            </nz-select>
          </div>
        }
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
  private readonly risksService = inject(RisksService);
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
  workItemHistoryModalVisible = signal(false);
  sprintHistoryModalVisible = signal(false);
  projectHistoryModalVisible = signal(false);

  // ── Work-item drawer ──────────────────────────────────────────────────────
  drawerWorkItem = signal<WorkItem | null>(null);

  openDrawerItem(wi: WorkItem): void {
    this.drawerWorkItem.set(wi);
  }

  onDrawerChanged(): void {
    this.backlogRefresh$.next();
  }
  // ─────────────────────────────────────────────────────────────────────────

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

  notes = signal<ProjectNoteDto[]>([]);
  notesLoading = signal(false);
  addingNote = signal(false);
  newNoteText = '';

  workItemHistory = signal<WorkItemStatusHistoryEntry[]>([]);
  workItemHistoryLoading = signal(false);

  sprintHistory = signal<SprintStatusHistoryEntry[]>([]);
  sprintHistoryLoading = signal(false);

  projectHistory = signal<ProjectStatusHistoryEntry[]>([]);
  projectHistoryLoading = signal(false);

  // ── Carry-over sprint modal ───────────────────────────────────────────────
  carryOverModalVisible = signal(false);
  carryOverLoading = signal(false);
  carryOverChoice: 'Backlog' | 'Sprint' = 'Backlog';
  carryOverTargetSprintId: number | null = null;
  private sprintToComplete: Sprint | null = null;

  unfinishedItemsCount = signal(0);

  planningSprintsForCarryOver = computed(() =>
    (this.sprints()?.items ?? []).filter(s => s.status === 'Planning')
  );
  // ─────────────────────────────────────────────────────────────────────────

  weeklyUpdates = signal<ProjectWeeklyUpdateDto[]>([]);
  weeklyUpdatesLoading = signal(false);
  savingWeeklyUpdate = signal(false);
  newWeeklyUpdateSummary = '';
  newWeeklyUpdateHealthStatus: ProjectHealthStatus = 'OnTrack';

  readonly HEALTH_STATUS_LABELS = PROJECT_HEALTH_STATUS_LABELS;
  readonly HEALTH_STATUS_COLORS = PROJECT_HEALTH_STATUS_COLORS;

  readonly RISK_LEVEL_LABELS = RISK_LEVEL_LABELS;
  readonly RISK_STATUS_LABELS = RISK_STATUS_LABELS;
  readonly RISK_STATUS_COLORS = RISK_STATUS_COLORS;

  readonly PROJECT_STATUS_LABELS = PROJECT_STATUS_LABELS;

  // ── Risks ─────────────────────────────────────────────────────────────────

  risks = signal<ProjectRiskDto[]>([]);
  risksLoading = signal(false);
  riskModalVisible = signal(false);
  editingRisk = signal<ProjectRiskDto | null>(null);
  riskForm: { description: string; probability: RiskLevel; impact: RiskLevel; mitigationPlan?: string | null } = {
    description: '',
    probability: 'Medium',
    impact: 'Medium',
    mitigationPlan: null,
  };
  riskFormStatus: RiskStatus = 'Open';

  severityColor(severity: number): string {
    if (severity <= 2) return 'success';
    if (severity <= 4) return 'warning';
    return 'error';
  }

  loadRisks(): void {
    this.risksLoading.set(true);
    this.risksService.getRisks(this.projectId).subscribe({
      next: r => { this.risks.set(r.items); this.risksLoading.set(false); },
      error: () => { this.risksLoading.set(false); },
    });
  }

  openRiskForm(risk?: ProjectRiskDto): void {
    this.editingRisk.set(risk ?? null);
    this.riskForm = risk
      ? { description: risk.description, probability: risk.probability, impact: risk.impact, mitigationPlan: risk.mitigationPlan }
      : { description: '', probability: 'Medium', impact: 'Medium', mitigationPlan: null };
    this.riskFormStatus = risk ? risk.status : 'Open';
    this.riskModalVisible.set(true);
  }

  saveRisk(): void {
    const editing = this.editingRisk();
    const onError = (err: unknown) => {
      const msg = (err as { error?: { message?: string } })?.error?.message;
      this.message.error(msg ?? 'Error al guardar el riesgo');
    };
    if (editing) {
      const dto: UpdateRiskDto = { ...this.riskForm, description: this.riskForm.description, status: this.riskFormStatus };
      this.risksService.updateRisk(this.projectId, editing.id, dto).subscribe({
        next: () => { this.riskModalVisible.set(false); this.message.success('Riesgo actualizado'); this.loadRisks(); },
        error: onError,
      });
    } else {
      this.risksService.createRisk(this.projectId, this.riskForm as CreateRiskDto).subscribe({
        next: () => { this.riskModalVisible.set(false); this.message.success('Riesgo añadido'); this.loadRisks(); },
        error: onError,
      });
    }
  }

  deleteRisk(risk: ProjectRiskDto): void {
    this.risksService.deleteRisk(this.projectId, risk.id).subscribe({
      next: () => { this.message.success('Riesgo eliminado'); this.loadRisks(); },
      error: () => this.message.error('Error al eliminar el riesgo'),
    });
  }

  // ── Dependencies ──────────────────────────────────────────────────────────
  dependencies = signal<ProjectDependenciesDto | null>(null);
  dependenciesLoading = signal(false);
  addDependencyModalVisible = signal(false);
  addDepProjectId: number | null = null;
  addDepDescription = '';
  dependencyProjectSearch = signal<{ id: number; title: string }[]>([]);

  loadDependencies(): void {
    this.dependenciesLoading.set(true);
    this.risksService.getDependencies(this.projectId).subscribe({
      next: d => { this.dependencies.set(d); this.dependenciesLoading.set(false); },
      error: () => { this.dependenciesLoading.set(false); },
    });
  }

  openAddDependencyModal(): void {
    this.addDepProjectId = null;
    this.addDepDescription = '';
    this.dependencyProjectSearch.set([]);
    this.addDependencyModalVisible.set(true);
  }

  searchDependencyProjects(q: string): void {
    if (!q.trim()) { this.dependencyProjectSearch.set([]); return; }
    this.http.get<{ items: { id: number; title: string }[] }>(`/api/projects?q=${encodeURIComponent(q)}&pageSize=20`).subscribe({
      next: r => this.dependencyProjectSearch.set(r.items),
      error: () => {},
    });
  }

  saveAddDependency(): void {
    if (!this.addDepProjectId) { this.message.warning('Selecciona un proyecto'); return; }
    this.risksService.createDependency(this.projectId, this.addDepProjectId, this.addDepDescription || null).subscribe({
      next: () => {
        this.addDependencyModalVisible.set(false);
        this.message.success('Dependencia añadida');
        this.loadDependencies();
      },
      error: (err) => {
        const msg = (err as { error?: string | { message?: string } })?.error;
        this.message.error(typeof msg === 'string' ? msg : 'Error al añadir la dependencia');
      },
    });
  }

  deleteDependency(dep: DependencyItemDto): void {
    this.risksService.deleteDependency(this.projectId, dep.dependencyId).subscribe({
      next: () => { this.message.success('Dependencia eliminada'); this.loadDependencies(); },
      error: () => this.message.error('Error al eliminar la dependencia'),
    });
  }

  readonly statusOptions = (Object.keys(PROJECT_STATUS_LABELS) as ProjectStatus[]).map(v => ({
    value: v, label: PROJECT_STATUS_LABELS[v],
  }));

  allowedStatusOptions(): { value: ProjectStatus; label: string }[] {
    const allowed = this.project()?.allowedNextStatuses ?? [];
    return allowed.map(v => ({ value: v, label: PROJECT_STATUS_LABELS[v] }));
  }

  statusOptionLabel(status: ProjectStatus): string {
    return PROJECT_STATUS_LABELS[status] ?? status;
  }

  epicForm: CreateEpicDto = { title: '', priority: 0, sortOrder: 0 };
  sprintForm: CreateSprintDto = { name: '' };
  workItemForm: {
    title: string;
    description?: string;
    type: WorkItemType;
    priority: WorkItemPriority;
    status?: WorkItemStatus;
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
    type: 'Task',
    priority: 'Medium',
    sortOrder: 0,
    isHito: false,
    assigneeIds: [],
  };

  private readonly refresh$ = new Subject<void>();
  private readonly epicsRefresh$ = new Subject<void>();
  private readonly sprintsRefresh$ = new Subject<void>();
  readonly backlogRefresh$ = new Subject<void>();

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
      switchMap(() => this.service.getProject(this.projectId))
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

  statusColor(s: WorkItemStatus): string { return STATUS_COLORS[s]; }
  statusLabel(s: WorkItemStatus): string { return STATUS_LABELS[s]; }
  priorityColor(p: WorkItemPriority): string { return PRIORITY_COLORS[p]; }
  workItemTypeLabel(t: WorkItemType): string { return WORK_ITEM_TYPE_LABELS[t]; }

  epicProgress(epic: Epic): number {
    if (!epic.workItemCount) return 0;
    return Math.round((epic.doneWorkItemCount / epic.workItemCount) * 100);
  }

  goBack(): void { this.router.navigate(['/projects']); }

  transition(status: ProjectStatus): void {
    this.service.transitionStatus(this.projectId, status).subscribe({
      next: () => { this.message.success('Estado actualizado'); this.refresh$.next(); },
      error: () => this.message.error('No se pudo cambiar el estado'),
    });
  }

  removeTeam(team: ProjectTeam): void {
    this.service.removeTeam(this.projectId, team.teamId).subscribe({
      next: () => { this.message.success(`Equipo "${team.teamName}" desasignado`); this.refresh$.next(); },
      error: () => this.message.error('Error al desasignar el equipo'),
    });
  }

  loadNotes(): void {
    this.notesLoading.set(true);
    this.service.getNotes(this.projectId).subscribe({
      next: n => { this.notes.set(n); this.notesLoading.set(false); },
      error: () => { this.notesLoading.set(false); },
    });
  }

  addNote(): void {
    if (!this.newNoteText.trim()) return;
    this.addingNote.set(true);
    this.service.createNote(this.projectId, this.newNoteText).subscribe({
      next: () => {
        this.addingNote.set(false);
        this.newNoteText = '';
        this.loadNotes();
        this.message.success('Nota añadida');
      },
      error: () => { this.addingNote.set(false); this.message.error('Error al añadir nota'); },
    });
  }

  deleteNote(note: ProjectNoteDto): void {
    this.service.deleteNote(this.projectId, note.id).subscribe({
      next: () => { this.message.success('Nota eliminada'); this.loadNotes(); },
      error: () => this.message.error('Error al eliminar nota'),
    });
  }

  loadWeeklyUpdates(): void {
    this.weeklyUpdatesLoading.set(true);
    this.service.getWeeklyUpdates(this.projectId).subscribe({
      next: list => { this.weeklyUpdates.set(list); this.weeklyUpdatesLoading.set(false); },
      error: () => { this.weeklyUpdatesLoading.set(false); },
    });
  }

  saveWeeklyUpdate(): void {
    if (!this.newWeeklyUpdateSummary.trim()) return;
    this.savingWeeklyUpdate.set(true);
    this.service.upsertWeeklyUpdate(this.projectId, {
      summary: this.newWeeklyUpdateSummary.trim(),
      healthStatus: this.newWeeklyUpdateHealthStatus,
    }).subscribe({
      next: () => {
        this.savingWeeklyUpdate.set(false);
        this.newWeeklyUpdateSummary = '';
        this.loadWeeklyUpdates();
        this.message.success('Avance guardado');
      },
      error: () => { this.savingWeeklyUpdate.set(false); this.message.error('Error al guardar el avance'); },
    });
  }

  formatWeekOf(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
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

  /** Called when the user clicks "Completar" on an Active sprint. */
  completeSprint(sprint: Sprint): void {
    // Count unfinished items in the current backlog for this sprint
    const unfinished = (this.backlog()?.items ?? [])
      .filter(wi => wi.sprintId === sprint.id && wi.status !== 'Done' && wi.status !== 'Discarded');

    if (unfinished.length === 0) {
      // No carry-over needed — complete directly
      this.transitionSprint(sprint, 'Completed');
      return;
    }

    // Open carry-over modal
    this.sprintToComplete = sprint;
    this.unfinishedItemsCount.set(unfinished.length);
    this.carryOverChoice = 'Backlog';
    this.carryOverTargetSprintId = null;
    this.carryOverModalVisible.set(true);
  }

  confirmCompleteSprint(): void {
    if (!this.sprintToComplete) return;

    if (this.carryOverChoice === 'Sprint' && !this.carryOverTargetSprintId) {
      this.message.warning('Selecciona un sprint destino');
      return;
    }

    this.carryOverLoading.set(true);
    this.sprintService.transitionStatus(
      this.projectId,
      this.sprintToComplete.id,
      'Completed',
      this.carryOverChoice,
      this.carryOverChoice === 'Sprint' ? (this.carryOverTargetSprintId ?? undefined) : undefined,
    ).subscribe({
      next: () => {
        this.carryOverLoading.set(false);
        this.carryOverModalVisible.set(false);
        this.sprintToComplete = null;
        this.message.success('Sprint completado');
        this.sprintsRefresh$.next();
        this.backlogRefresh$.next();
      },
      error: () => {
        this.carryOverLoading.set(false);
        this.message.error('Error al completar el sprint');
      },
    });
  }

  openWorkItemForm(wi?: WorkItem): void {
    this.editingWorkItem.set(wi ?? null);
    this.workItemForm = wi
      ? { title: wi.title, description: wi.description, type: wi.type, priority: wi.priority, status: wi.status, epicId: wi.epicId, sprintId: wi.sprintId, assigneeIds: wi.assignees.map(a => a.id), estimationHours: wi.estimationHours, estimationPoints: wi.estimationPoints, sortOrder: wi.sortOrder, isHito: wi.isHito, hitoDate: wi.hitoDate, dueDate: wi.dueDate }
      : { title: '', type: 'Task', priority: 'Medium', sortOrder: 0, isHito: false, assigneeIds: [] };
    this.workItemModalVisible.set(true);
  }

  saveWorkItem(): void {
    const editing = this.editingWorkItem();
    const { status: newStatus, ...rest } = this.workItemForm;
    const dto = {
      ...rest,
      dueDate: this.formatDate(this.workItemForm.dueDate),
      hitoDate: this.formatDate(this.workItemForm.hitoDate),
    };
    const onError = () => this.message.error('Error al guardar la tarea');

    const afterSave = (id: number) => {
      if (editing && newStatus && newStatus !== editing.status) {
        this.workItemsService.transitionStatus(this.projectId, id, newStatus).subscribe({
          next: () => {
            this.workItemModalVisible.set(false);
            this.message.success('Tarea actualizada');
            this.backlogRefresh$.next();
          },
          error: () => {
            this.message.error('Tarea guardada, pero no se pudo cambiar el estado');
            this.backlogRefresh$.next();
          },
        });
      } else {
        this.workItemModalVisible.set(false);
        this.message.success(editing ? 'Tarea actualizada' : 'Tarea creada');
        this.backlogRefresh$.next();
      }
    };

    if (editing) {
      this.workItemsService.updateWorkItem(this.projectId, editing.id, dto).subscribe({ next: () => afterSave(editing.id), error: onError });
    } else {
      this.workItemsService.createWorkItem(this.projectId, dto).subscribe({ next: (r) => afterSave(r.id), error: onError });
    }
  }

  openWorkItemHistory(wi: WorkItem): void {
    this.workItemHistoryModalVisible.set(true);
    this.workItemHistoryLoading.set(true);
    this.workItemsService.getStatusHistory(this.projectId, wi.id).subscribe({
      next: (h) => { this.workItemHistory.set(h); this.workItemHistoryLoading.set(false); },
      error: () => { this.workItemHistoryLoading.set(false); this.message.error('Error al cargar el histórico'); },
    });
  }

  openSprintHistory(sprint: Sprint): void {
    this.sprintHistoryModalVisible.set(true);
    this.sprintHistoryLoading.set(true);
    this.sprintService.getStatusHistory(this.projectId, sprint.id).subscribe({
      next: (h) => { this.sprintHistory.set(h); this.sprintHistoryLoading.set(false); },
      error: () => { this.sprintHistoryLoading.set(false); this.message.error('Error al cargar el histórico'); },
    });
  }

  openProjectHistory(): void {
    this.projectHistoryModalVisible.set(true);
    this.projectHistoryLoading.set(true);
    this.service.getStatusHistory(this.projectId).subscribe({
      next: (h) => { this.projectHistory.set(h); this.projectHistoryLoading.set(false); },
      error: () => { this.projectHistoryLoading.set(false); this.message.error('Error al cargar el histórico'); },
    });
  }

  deleteWorkItem(wi: WorkItem): void {
    this.workItemsService.deleteWorkItem(this.projectId, wi.id).subscribe({
      next: () => { this.message.success('Tarea eliminada'); this.backlogRefresh$.next(); },
      error: () => this.message.error('Error al eliminar la tarea'),
    });
  }

  discardWorkItem(wi: WorkItem): void {
    this.workItemsService.transitionStatus(this.projectId, wi.id, 'Discarded').subscribe({
      next: () => { this.message.success('Tarea descartada'); this.backlogRefresh$.next(); },
      error: () => this.message.error('Error al descartar la tarea'),
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
