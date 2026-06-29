import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type SprintStatus = 'Planning' | 'Active' | 'Completed';

export interface Sprint {
  id: number;
  projectId: number;
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
  status: SprintStatus;
  capacity?: number;
  workItemCount: number;
  totalEstimationHours: number;
  totalEstimationPoints: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface SprintStatusHistoryEntry {
  id: number;
  fromStatus: SprintStatus | null;
  toStatus: SprintStatus;
  changedById: number;
  changedByName: string;
  changedAt: string;
}

export interface CreateSprintDto {
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
}

@Injectable({ providedIn: 'root' })
export class SprintService {
  private readonly http = inject(HttpClient);

  getSprints(projectId: number): Observable<PagedResult<Sprint>> {
    return this.http.get<PagedResult<Sprint>>(`/api/projects/${projectId}/sprints?pageSize=100`);
  }

  getSprintById(projectId: number, sprintId: number): Observable<Sprint> {
    return this.http.get<Sprint>(`/api/projects/${projectId}/sprints/${sprintId}`);
  }

  createSprint(projectId: number, dto: CreateSprintDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`/api/projects/${projectId}/sprints`, dto);
  }

  updateSprint(projectId: number, id: number, dto: CreateSprintDto): Observable<void> {
    return this.http.put<void>(`/api/projects/${projectId}/sprints/${id}`, dto);
  }

  deleteSprint(projectId: number, id: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${projectId}/sprints/${id}`);
  }

  transitionStatus(projectId: number, id: number, status: SprintStatus): Observable<void> {
    return this.http.post<void>(`/api/projects/${projectId}/sprints/${id}/status`, { status });
  }

  assignToSprint(projectId: number, workItemId: number, sprintId: number | null): Observable<void> {
    return this.http.post<void>(`/api/projects/${projectId}/workitems/${workItemId}/sprint`, { sprintId });
  }

  getStatusHistory(projectId: number, id: number): Observable<SprintStatusHistoryEntry[]> {
    return this.http.get<SprintStatusHistoryEntry[]>(`/api/projects/${projectId}/sprints/${id}/status-history`);
  }
}
