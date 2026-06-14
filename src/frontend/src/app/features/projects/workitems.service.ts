import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type WorkItemStatus = 'Backlog' | 'ToDo' | 'InProgress' | 'Blocked' | 'Done';
export type WorkItemPriority = 'Low' | 'Medium' | 'High' | 'Critical';

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

export interface CreateWorkItemDto {
  title: string;
  description?: string;
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

@Injectable({ providedIn: 'root' })
export class WorkItemsService {
  private readonly http = inject(HttpClient);

  getWorkItems(projectId: number, sprintId?: number): Observable<PagedResult<WorkItem>> {
    let url = `/api/projects/${projectId}/workitems?pageSize=100`;
    if (sprintId) url += `&sprintId=${sprintId}`;
    return this.http.get<PagedResult<WorkItem>>(url);
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
}
