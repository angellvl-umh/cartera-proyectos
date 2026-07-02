import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzCollapseModule } from 'ng-zorro-antd/collapse';
import { NzTimelineModule } from 'ng-zorro-antd/timeline';
import { WorkItem, WorkItemStatus, WorkItemsService, WorkItemStatusHistoryEntry, WORK_ITEM_TYPE_LABELS } from '../workitems.service';
import { CommentsService, CommentDto } from '../comments.service';

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

const PRIORITY_COLORS: Record<string, string> = {
  Low: 'default',
  Medium: 'blue',
  High: 'orange',
  Critical: 'red',
};

const TERMINAL_STATUSES: WorkItemStatus[] = ['Done', 'Discarded'];
const ALL_STATUSES: WorkItemStatus[] = ['Backlog', 'ToDo', 'InProgress', 'Blocked', 'Done'];

@Component({
  selector: 'app-work-item-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzDrawerModule, NzTagModule, NzIconModule, NzButtonModule, NzSelectModule,
    NzInputModule, NzSpinModule, NzAvatarModule, NzDividerModule,
    NzTooltipModule, NzEmptyModule, NzCollapseModule, NzTimelineModule,
  ],
  styles: [`
    .drawer-section { margin-bottom: 20px; }
    .drawer-label { font-size: 11px; font-weight: 700; letter-spacing: 0.5px; text-transform: uppercase; color: #8c8c8c; margin-bottom: 6px; }
    .meta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 16px; }
    .meta-item { }
    .comment-row { display: flex; gap: 10px; margin-bottom: 14px; }
    .comment-body { flex: 1; }
    .comment-author { font-weight: 600; font-size: 13px; }
    .comment-date { font-size: 11px; color: #8c8c8c; margin-left: 8px; }
    .comment-text { margin: 4px 0 0; font-size: 13px; white-space: pre-wrap; }
    .overdue { color: #f5222d; font-weight: 600; }
  `],
  template: `
    <nz-drawer
      [nzVisible]="workItem() !== null"
      [nzWidth]="480"
      [nzTitle]="drawerTitle"
      [nzClosable]="true"
      (nzOnClose)="closed.emit()"
      nzPlacement="right"
    >
      <ng-template #drawerTitle>
        <div style="display:flex;align-items:center;gap:8px;padding-right:16px">
          <span nz-icon [nzType]="workItem()?.type === 'UserStory' ? 'read' : 'file-text'"
            style="color:#8c8c8c;font-size:14px"></span>
          <span style="font-size:14px;font-weight:600;line-height:1.3">{{ workItem()?.title }}</span>
        </div>
      </ng-template>

      <ng-container *nzDrawerContent>
        @if (workItem(); as wi) {
          <!-- Badges de estado y prioridad -->
          <div style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:16px">
            <nz-tag [nzColor]="statusColor(wi.status)">{{ statusLabel(wi.status) }}</nz-tag>
            <nz-tag [nzColor]="priorityColor(wi.priority)">{{ wi.priority }}</nz-tag>
            @if (wi.isHito) {
              <nz-tag nzColor="magenta">
                <span nz-icon nzType="flag"></span> Hito
              </nz-tag>
            }
          </div>

          <!-- Metadatos -->
          <div class="meta-grid">
            @if (wi.epicTitle) {
              <div class="meta-item">
                <div class="drawer-label">Épica</div>
                <span style="font-size:13px;color:#1890ff">{{ wi.epicTitle }}</span>
              </div>
            }
            @if (wi.sprintName) {
              <div class="meta-item">
                <div class="drawer-label">Sprint</div>
                <span style="font-size:13px;color:#722ed1">{{ wi.sprintName }}</span>
              </div>
            }
            @if (wi.assignees.length > 0) {
              <div class="meta-item" style="grid-column: 1 / -1">
                <div class="drawer-label">Asignados</div>
                <div style="display:flex;gap:6px;flex-wrap:wrap;margin-top:4px">
                  @for (a of wi.assignees; track a.id) {
                    <nz-avatar [nzText]="a.name[0]" nz-tooltip [nzTooltipTitle]="a.name"
                      style="background:#1890ff;font-size:12px"></nz-avatar>
                    <span style="font-size:13px;line-height:32px">{{ a.name }}</span>
                  }
                </div>
              </div>
            }
            @if (wi.estimationPoints || wi.estimationHours) {
              <div class="meta-item">
                <div class="drawer-label">Estimación</div>
                <span style="font-size:13px">
                  @if (wi.estimationPoints) { 🎯 {{ wi.estimationPoints }} pts }
                  @if (wi.estimationHours) { &nbsp;⏱ {{ wi.estimationHours }}h }
                </span>
              </div>
            }
            @if (wi.dueDate) {
              <div class="meta-item">
                <div class="drawer-label">Fecha límite</div>
                <span [class.overdue]="isOverdue(wi.dueDate!)" style="font-size:13px">
                  {{ wi.dueDate }}
                  @if (isOverdue(wi.dueDate!)) {
                    <span nz-icon nzType="warning" style="margin-left:4px"></span>
                  }
                </span>
              </div>
            }
            @if (wi.isHito && wi.hitoDate) {
              <div class="meta-item">
                <div class="drawer-label">Fecha hito</div>
                <span style="font-size:13px">{{ wi.hitoDate }}</span>
              </div>
            }
          </div>

          <!-- Descripción -->
          @if (wi.description) {
            <div class="drawer-section">
              <div class="drawer-label">Descripción</div>
              <p style="font-size:13px;white-space:pre-wrap;margin:0;color:#262626">{{ wi.description }}</p>
            </div>
          }

          <nz-divider style="margin:12px 0"></nz-divider>

          <!-- Cambio de estado -->
          @if (!isTerminal(wi.status)) {
            <div class="drawer-section">
              <div class="drawer-label">Cambiar estado</div>
              <div style="display:flex;gap:8px;align-items:center">
                <nz-select
                  [ngModel]="selectedStatus()"
                  (ngModelChange)="selectedStatus.set($event)"
                  style="flex:1"
                  nzPlaceHolder="Seleccionar estado destino">
                  @for (s of availableStatuses(wi.status); track s) {
                    <nz-option [nzValue]="s" [nzLabel]="statusLabel(s)" />
                  }
                </nz-select>
                <button nz-button nzType="primary" [nzLoading]="transitioningStatus()"
                  [disabled]="!selectedStatus()"
                  (click)="doTransition(wi)">
                  <span nz-icon nzType="swap"></span> Aplicar
                </button>
              </div>
            </div>
            <nz-divider style="margin:12px 0"></nz-divider>
          }

          <!-- Comentarios -->
          <div class="drawer-section">
            <div class="drawer-label" style="margin-bottom:12px">
              Comentarios
              @if (comments().length > 0) {
                <span style="margin-left:6px;background:#f0f0f0;border-radius:10px;padding:1px 8px;font-size:11px">
                  {{ comments().length }}
                </span>
              }
            </div>

            @if (commentsLoading()) {
              <div style="text-align:center;padding:16px"><nz-spin /></div>
            } @else {
              @if (comments().length === 0) {
                <p style="color:#8c8c8c;font-size:13px;text-align:center">Sin comentarios aún.</p>
              }
              @for (c of comments(); track c.id) {
                <div class="comment-row">
                  <nz-avatar [nzText]="c.authorName[0]" style="background:#1890ff;flex-shrink:0;font-size:12px"></nz-avatar>
                  <div class="comment-body">
                    <span class="comment-author">{{ c.authorName }}</span>
                    <span class="comment-date">{{ formatDate(c.createdAt) }}</span>
                    <p class="comment-text">{{ c.text }}</p>
                  </div>
                </div>
              }
              <!-- Añadir comentario -->
              <div style="margin-top:12px">
                <textarea nz-input [(ngModel)]="newCommentText"
                  [nzAutosize]="{ minRows: 2, maxRows: 5 }"
                  placeholder="Escribe un comentario..."
                  style="margin-bottom:8px;font-size:13px"></textarea>
                <button nz-button nzType="primary" nzSize="small"
                  [nzLoading]="addingComment()"
                  [disabled]="!newCommentText.trim()"
                  (click)="addComment(wi)">
                  <span nz-icon nzType="send"></span> Publicar
                </button>
              </div>
            }
          </div>

          <nz-divider style="margin:12px 0"></nz-divider>

          <!-- Histórico plegado -->
          <nz-collapse nzGhost [nzBordered]="false">
            <nz-collapse-panel nzHeader="Histórico de estados" [nzActive]="false">
              @if (historyLoading()) {
                <div style="text-align:center;padding:16px"><nz-spin /></div>
              } @else if (history().length === 0) {
                <p style="color:#8c8c8c;font-size:13px">Sin histórico.</p>
              } @else {
                <nz-timeline>
                  @for (h of history(); track h.id) {
                    <nz-timeline-item>
                      <div style="font-size:13px">
                        @if (h.fromStatus) {
                          <nz-tag [nzColor]="statusColor(h.fromStatus)" style="font-size:11px">{{ statusLabel(h.fromStatus) }}</nz-tag>
                          <span nz-icon nzType="arrow-right" style="margin:0 4px;color:#8c8c8c"></span>
                        }
                        <nz-tag [nzColor]="statusColor(h.toStatus)" style="font-size:11px">{{ statusLabel(h.toStatus) }}</nz-tag>
                      </div>
                      <div style="font-size:12px;color:#8c8c8c;margin-top:2px">
                        {{ h.changedByName }} · {{ formatDate(h.changedAt) }}
                      </div>
                    </nz-timeline-item>
                  }
                </nz-timeline>
              }
            </nz-collapse-panel>
          </nz-collapse>
        }
      </ng-container>
    </nz-drawer>
  `,
})
export class WorkItemDrawerComponent {
  private readonly workItemsService = inject(WorkItemsService);
  private readonly commentsService = inject(CommentsService);
  private readonly message = inject(NzMessageService);

  readonly workItem = input<WorkItem | null>(null);
  readonly closed = output<void>();
  readonly changed = output<void>();

  // State
  selectedStatus = signal<WorkItemStatus | null>(null);
  transitioningStatus = signal(false);

  comments = signal<CommentDto[]>([]);
  commentsLoading = signal(false);
  addingComment = signal(false);
  newCommentText = '';

  history = signal<WorkItemStatusHistoryEntry[]>([]);
  historyLoading = signal(false);

  constructor() {
    // Load data lazily when drawer opens (workItem changes from null to a value)
    effect(() => {
      const wi = this.workItem();
      if (wi) {
        this.selectedStatus.set(null);
        this.newCommentText = '';
        this.loadComments(wi);
        this.loadHistory(wi);
      } else {
        this.comments.set([]);
        this.history.set([]);
      }
    });
  }

  private loadComments(wi: WorkItem): void {
    this.commentsLoading.set(true);
    this.commentsService.getComments(wi.projectId, wi.id).subscribe({
      next: list => { this.comments.set(list); this.commentsLoading.set(false); },
      error: () => { this.commentsLoading.set(false); },
    });
  }

  private loadHistory(wi: WorkItem): void {
    this.historyLoading.set(true);
    this.workItemsService.getStatusHistory(wi.projectId, wi.id).subscribe({
      next: list => { this.history.set(list); this.historyLoading.set(false); },
      error: () => { this.historyLoading.set(false); },
    });
  }

  addComment(wi: WorkItem): void {
    const text = this.newCommentText.trim();
    if (!text) return;
    this.addingComment.set(true);
    this.commentsService.createComment(wi.projectId, wi.id, text).subscribe({
      next: () => {
        this.newCommentText = '';
        this.addingComment.set(false);
        this.loadComments(wi);
      },
      error: () => {
        this.addingComment.set(false);
        this.message.error('Error al publicar el comentario');
      },
    });
  }

  doTransition(wi: WorkItem): void {
    const newStatus = this.selectedStatus();
    if (!newStatus) return;
    this.transitioningStatus.set(true);
    this.workItemsService.transitionStatus(wi.projectId, wi.id, newStatus).subscribe({
      next: () => {
        this.transitioningStatus.set(false);
        this.message.success(`Estado cambiado a "${this.statusLabel(newStatus)}"`);
        this.selectedStatus.set(null);
        this.loadHistory(wi);
        this.changed.emit();
      },
      error: () => {
        this.transitioningStatus.set(false);
        this.message.error('No se pudo cambiar el estado');
      },
    });
  }

  availableStatuses(current: WorkItemStatus): WorkItemStatus[] {
    return ALL_STATUSES.filter(s => s !== current);
  }

  isTerminal(status: WorkItemStatus): boolean {
    return TERMINAL_STATUSES.includes(status);
  }

  statusColor(s: WorkItemStatus): string { return STATUS_COLORS[s] ?? 'default'; }
  statusLabel(s: WorkItemStatus): string { return STATUS_LABELS[s] ?? s; }
  priorityColor(p: string): string { return PRIORITY_COLORS[p] ?? 'default'; }

  isOverdue(dateStr: string): boolean {
    return new Date(dateStr) < new Date();
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('es-ES', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }
}
