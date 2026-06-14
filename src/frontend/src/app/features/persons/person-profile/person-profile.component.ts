import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzBadgeModule } from 'ng-zorro-antd/badge';

interface PersonTeamDto { teamId: number; teamName: string; isLead: boolean; }
interface PersonProjectSummaryDto { projectId: number; projectTitle: string; projectStatus: string; assignedTasks: number; doneTasks: number; }
interface PersonWorkloadDto { total: number; backlog: number; toDo: number; inProgress: number; blocked: number; done: number; projects: PersonProjectSummaryDto[]; }
interface PersonTaskDto { id: number; title: string; status: string; priority: string; projectId: number; projectTitle: string; sprintId?: number; sprintName?: string; epicTitle?: string; dueDate?: string; }
interface PersonProfileDto { id: number; name: string; email: string; role: string; teams: PersonTeamDto[]; workload: PersonWorkloadDto; activeTasks: PersonTaskDto[]; }

const ROLE_LABELS: Record<string, string> = { Gestor: 'Gestor de cartera', JefeEquipo: 'Jefe de equipo', Desarrollador: 'Desarrollador' };
const ROLE_COLORS: Record<string, string> = { Gestor: 'purple', JefeEquipo: 'blue', Desarrollador: 'green' };
const STATUS_COLORS: Record<string, string> = { Backlog: 'default', ToDo: 'processing', InProgress: 'warning', Blocked: 'error', Done: 'success' };
const STATUS_LABELS: Record<string, string> = { Backlog: 'Backlog', ToDo: 'Por hacer', InProgress: 'En progreso', Blocked: 'Bloqueada', Done: 'Hecho' };
const PRIORITY_COLORS: Record<string, string> = { Low: 'default', Medium: 'processing', High: 'warning', Critical: 'error' };
const PROJECT_STATUS_LABELS: Record<string, string> = { Proposed: 'Propuesto', Approved: 'Aprobado', InProgress: 'En ejecución', Paused: 'Pausado', Completed: 'Completado', Cancelled: 'Cancelado' };

@Component({
  selector: 'app-person-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, NzCardModule, NzTableModule, NzTagModule, NzButtonModule,
    NzIconModule, NzSpinModule, NzProgressModule, NzEmptyModule,
    NzDividerModule, NzTooltipModule, NzBadgeModule,
  ],
  styles: [`
    .profile-wrapper { max-width: 1000px; margin: 0 auto; }
    .avatar-circle { width: 72px; height: 72px; border-radius: 50%; display: flex; align-items: center;
                     justify-content: center; font-size: 28px; font-weight: 700; color: #fff;
                     background: #1890ff; flex-shrink: 0; }
    .info-header { display: flex; align-items: center; gap: 20px; padding: 24px;
                   background: #fff; border-radius: 8px; border: 1px solid #f0f0f0;
                   box-shadow: 0 1px 4px rgba(0,0,0,.06); margin-bottom: 20px; }
    .workload-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 12px; margin-bottom: 20px; }
    @media (max-width: 700px) { .workload-grid { grid-template-columns: repeat(3, 1fr); } }
    .wl-box { background: #fff; border-radius: 8px; padding: 12px 14px; text-align: center;
               border: 1px solid #f0f0f0; }
    .wl-label { font-size: 11px; color: #8c8c8c; margin-bottom: 4px; }
    .wl-value { font-size: 22px; font-weight: 700; }
    .content-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px; }
    @media (max-width: 700px) { .content-grid { grid-template-columns: 1fr; } }
    .overdue { color: #ff4d4f; font-weight: 600; }
  `],
  template: `
    @if (!profile()) {
      <div style="display:flex;justify-content:center;padding:80px"><nz-spin nzSize="large" /></div>
    } @else {
      <div class="profile-wrapper">

        <!-- Back + header -->
        <a [routerLink]="['/persons']"
          style="display:inline-flex;align-items:center;gap:4px;color:#595959;font-size:13px;text-decoration:none;margin-bottom:12px">
          <span nz-icon nzType="arrow-left"></span> Volver a personas
        </a>

        <div class="info-header">
          <div class="avatar-circle">{{ profile()!.name[0] }}</div>
          <div style="flex:1">
            <h2 style="margin:0 0 4px;font-size:20px">{{ profile()!.name }}</h2>
            <p style="margin:0 0 8px;color:#8c8c8c;font-size:13px">{{ profile()!.email }}</p>
            <nz-tag [nzColor]="ROLE_COLORS[profile()!.role]">{{ ROLE_LABELS[profile()!.role] ?? profile()!.role }}</nz-tag>
          </div>
          <!-- Carga global -->
          <div style="text-align:center">
            <div style="font-size:12px;color:#8c8c8c;margin-bottom:6px">Carga activa</div>
            <nz-progress
              nzType="circle"
              [nzPercent]="donePct()"
              [nzWidth]="64"
              [nzStrokeColor]="donePct() >= 80 ? '#52c41a' : donePct() >= 40 ? '#faad14' : '#1890ff'"
              [nzFormat]="fmtPct">
            </nz-progress>
            <div style="font-size:11px;color:#8c8c8c;margin-top:4px">completado</div>
          </div>
        </div>

        <!-- Workload bars -->
        <div class="workload-grid">
          <div class="wl-box">
            <div class="wl-label">Total</div>
            <div class="wl-value" style="color:#1890ff">{{ profile()!.workload.total }}</div>
          </div>
          <div class="wl-box">
            <div class="wl-label">En progreso</div>
            <div class="wl-value" style="color:#faad14">{{ profile()!.workload.inProgress }}</div>
          </div>
          <div class="wl-box">
            <div class="wl-label">Bloqueadas</div>
            <div class="wl-value" style="color:#ff4d4f">{{ profile()!.workload.blocked }}</div>
          </div>
          <div class="wl-box">
            <div class="wl-label">Por hacer</div>
            <div class="wl-value" style="color:#595959">{{ profile()!.workload.toDo }}</div>
          </div>
          <div class="wl-box">
            <div class="wl-label">Completadas</div>
            <div class="wl-value" style="color:#52c41a">{{ profile()!.workload.done }}</div>
          </div>
        </div>

        <div class="content-grid">

          <!-- Equipos -->
          <nz-card nzTitle="Equipos">
            @if (profile()!.teams.length === 0) {
              <nz-empty nzNotFoundContent="Sin equipos asignados" />
            } @else {
              @for (t of profile()!.teams; track t.teamId) {
                <div style="display:flex;align-items:center;justify-content:space-between;padding:8px 0;border-bottom:1px solid #f5f5f5">
                  <a [routerLink]="['/teams', t.teamId]" style="font-weight:600;font-size:13px">{{ t.teamName }}</a>
                  @if (t.isLead) { <nz-tag nzColor="gold">Líder</nz-tag> }
                </div>
              }
            }
          </nz-card>

          <!-- Proyectos -->
          <nz-card nzTitle="Proyectos con tareas asignadas">
            @if (profile()!.workload.projects.length === 0) {
              <nz-empty nzNotFoundContent="Sin proyectos" />
            } @else {
              @for (p of profile()!.workload.projects; track p.projectId) {
                <div style="margin-bottom:10px">
                  <div style="display:flex;justify-content:space-between;align-items:baseline;margin-bottom:4px">
                    <a [routerLink]="['/projects', p.projectId]" style="font-size:13px;font-weight:600">{{ p.projectTitle }}</a>
                    <span style="font-size:12px;color:#8c8c8c">{{ p.doneTasks }}/{{ p.assignedTasks }}</span>
                  </div>
                  <nz-progress
                    [nzPercent]="projectPct(p)"
                    nzSize="small"
                    [nzShowInfo]="false"
                    [nzStrokeColor]="projectPct(p) >= 80 ? '#52c41a' : '#1890ff'">
                  </nz-progress>
                </div>
              }
            }
          </nz-card>

        </div>

        <!-- Tareas activas -->
        <nz-card nzTitle="Tareas activas (sin completar)">
          @if (profile()!.activeTasks.length === 0) {
            <nz-empty nzNotFoundContent="Sin tareas activas" />
          } @else {
            <nz-table
              [nzData]="profile()!.activeTasks"
              nzBordered nzSize="small"
              [nzShowPagination]="profile()!.activeTasks.length > 10">
              <thead>
                <tr>
                  <th>Tarea</th>
                  <th>Proyecto</th>
                  <th>Sprint</th>
                  <th>Estado</th>
                  <th>Prioridad</th>
                  <th>Fecha límite</th>
                </tr>
              </thead>
              <tbody>
                @for (t of profile()!.activeTasks; track t.id) {
                  <tr>
                    <td style="font-size:13px;font-weight:600">{{ t.title }}</td>
                    <td>
                      <a [routerLink]="['/projects', t.projectId]" style="font-size:13px">{{ t.projectTitle }}</a>
                    </td>
                    <td style="font-size:12px">
                      @if (t.sprintName) {
                        <a [routerLink]="['/projects', t.projectId, 'sprints', t.sprintId, 'kanban']" style="color:#722ed1">
                          ⚡ {{ t.sprintName }}
                        </a>
                      } @else { <span style="color:#bfbfbf">Backlog</span> }
                    </td>
                    <td><nz-tag [nzColor]="STATUS_COLORS[t.status]">{{ STATUS_LABELS[t.status] }}</nz-tag></td>
                    <td><nz-tag [nzColor]="PRIORITY_COLORS[t.priority]">{{ t.priority }}</nz-tag></td>
                    <td>
                      @if (t.dueDate) {
                        <span [class.overdue]="isOverdue(t.dueDate)">{{ t.dueDate }}</span>
                      } @else { <span style="color:#bfbfbf">—</span> }
                    </td>
                  </tr>
                }
              </tbody>
            </nz-table>
          }
        </nz-card>

      </div>
    }
  `,
})
export class PersonProfileComponent {
  private readonly http = inject(HttpClient);
  private readonly personId = +inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  readonly profile = toSignal(
    this.http.get<PersonProfileDto>(`/api/persons/${this.personId}/profile`)
  );

  readonly donePct = computed(() => {
    const w = this.profile()?.workload;
    if (!w || w.total === 0) return 0;
    return Math.round((w.done / w.total) * 100);
  });

  readonly fmtPct = (p: number) => `${p}%`;

  readonly ROLE_COLORS    = ROLE_COLORS;
  readonly ROLE_LABELS    = ROLE_LABELS;
  readonly STATUS_COLORS  = STATUS_COLORS;
  readonly STATUS_LABELS  = STATUS_LABELS;
  readonly PRIORITY_COLORS = PRIORITY_COLORS;

  projectPct(p: PersonProjectSummaryDto): number {
    if (p.assignedTasks === 0) return 0;
    return Math.round((p.doneTasks / p.assignedTasks) * 100);
  }

  isOverdue(d: string): boolean { return new Date(d) < new Date(); }
}
