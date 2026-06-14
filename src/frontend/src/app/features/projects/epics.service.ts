import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Epic {
  id: number;
  projectId: number;
  title: string;
  description?: string;
  priority: number;
  sortOrder: number;
  workItemCount: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateEpicDto {
  title: string;
  description?: string;
  priority: number;
  sortOrder: number;
}

@Injectable({ providedIn: 'root' })
export class EpicsService {
  private readonly http = inject(HttpClient);

  getEpics(projectId: number): Observable<PagedResult<Epic>> {
    return this.http.get<PagedResult<Epic>>(`/api/projects/${projectId}/epics?pageSize=100`);
  }

  createEpic(projectId: number, dto: CreateEpicDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`/api/projects/${projectId}/epics`, dto);
  }

  updateEpic(projectId: number, id: number, dto: CreateEpicDto): Observable<void> {
    return this.http.put<void>(`/api/projects/${projectId}/epics/${id}`, dto);
  }

  deleteEpic(projectId: number, id: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${projectId}/epics/${id}`);
  }
}
