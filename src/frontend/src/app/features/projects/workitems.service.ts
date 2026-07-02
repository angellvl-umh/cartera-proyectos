import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type WorkItemStatus = 'Backlog' | 'ToDo' | 'InProgress' | 'Blocked' | 'Done' | 'Discarded';
export type WorkItemPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type WorkItemType = 'Task' | 'UserStory';
export const WORK_ITEM_TYPE_LABELS: Record<WorkItemType, string> = { Task: 'Tarea', UserStory: 'Historia de usuario' };

export interface Assignee {
  id: number;
  name: string;
}

export interface WorkItem {
  id: number;
  projectId: number;
  epicId?: number;
  epicTitle?: string;
  sprintId?: number;
  sprintName?: string;
  title: string;
  description?: string;
  status: WorkItemStatus;
  type: WorkItemType;
  priority: WorkItemPriority;
  assignees: Assignee[];
  sortOrder: number;
  estimationHours?: number;
  estimationPoints?: number;
  isHito: boolean;
  hitoDate?: string;
  dueDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface WorkItemStatusHistoryEntry {
  id: number;
  fromStatus: WorkItemStatus | null;
  toStatus: WorkItemStatus;
  changedById: number;
  changedByName: string;
  changedAt: string;
}

export interface CreateWorkItemDto {
  title: string;
  description?: string;
  type: WorkItemType;
  priority: WorkItemPriority;
  epicId?: number;
  assigneeIds: number[];
  sortOrder: number;
  estimationHours?: number;
  estimationPoints?: number;
  isHito: boolean;
  hitoDate?: string;
  dueDate?: string;
  sprintId?: number;
}

export interface WorkItemSearchOpts {
  q?: string;
  epicId?: number | null;
  priority?: WorkItemPriority | null;
  type?: WorkItemType | null;
  assigneeId?: number | null;
  page?: number;
  pageSize?: number;
  backlogOnly?: boolean;
  sprintId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class WorkItemsService {
  private readonly http = inject(HttpClient);

  getWorkItems(projectId: number, sprintId?: number): Observable<PagedResult<WorkItem>> {
    let url = `/api/projects/${projectId}/workitems?pageSize=100`;
    if (sprintId) url += `&sprintId=${sprintId}`;
    return this.http.get<PagedResult<WorkItem>>(url);
  }

  searchWorkItems(projectId: number, opts: WorkItemSearchOpts = {}): Observable<PagedResult<WorkItem>> {
    let params = new HttpParams()
      .set('page', (opts.page ?? 1).toString())
      .set('pageSize', (opts.pageSize ?? 25).toString());

    if (opts.backlogOnly) params = params.set('backlogOnly', 'true');
    if (opts.q?.trim()) params = params.set('q', opts.q.trim());
    if (opts.epicId != null) params = params.set('epicId', opts.epicId.toString());
    if (opts.priority != null) params = params.set('priority', opts.priority);
    if (opts.type != null) params = params.set('type', opts.type);
    if (opts.assigneeId != null) params = params.set('assigneeId', opts.assigneeId.toString());
    if (opts.sprintId != null) params = params.set('sprintId', opts.sprintId.toString());

    return this.http.get<PagedResult<WorkItem>>(`/api/projects/${projectId}/workitems`, { params });
  }

  getBacklog(projectId: number): Observable<PagedResult<WorkItem>> {
    return this.http.get<PagedResult<WorkItem>>(`/api/projects/${projectId}/workitems?pageSize=100&backlogOnly=true`);
  }

  createWorkItem(projectId: number, dto: CreateWorkItemDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`/api/projects/${projectId}/workitems`, dto);
  }

  updateWorkItem(projectId: number, id: number, dto: CreateWorkItemDto): Observable<void> {
    return this.http.put<void>(`/api/projects/${projectId}/workitems/${id}`, dto);
  }

  deleteWorkItem(projectId: number, id: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${projectId}/workitems/${id}`);
  }

  transitionStatus(projectId: number, id: number, status: WorkItemStatus): Observable<void> {
    return this.http.post<void>(`/api/projects/${projectId}/workitems/${id}/status`, { status });
  }

  getStatusHistory(projectId: number, id: number): Observable<WorkItemStatusHistoryEntry[]> {
    return this.http.get<WorkItemStatusHistoryEntry[]>(`/api/projects/${projectId}/workitems/${id}/status-history`);
  }

  reorder(projectId: number, orderedIds: number[]): Observable<void> {
    return this.http.post<void>(`/api/projects/${projectId}/workitems/reorder`, { orderedIds });
  }

  bulkAssignSprint(projectId: number, workItemIds: number[], sprintId: number | null): Observable<void> {
    return this.http.post<void>(`/api/projects/${projectId}/workitems/bulk-sprint`, { workItemIds, sprintId });
  }
}
