import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type SprintStatus = 'Planning' | 'Active' | 'Completed';
export type CarryOverTarget = 'Backlog' | 'Sprint';

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
  committedPoints?: number;
  deliveredPoints?: number;
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

// ── Velocity ──────────────────────────────────────────────────────────────────

export interface SprintVelocityDto {
  sprintId: number;
  name: string;
  startDate?: string;
  endDate?: string;
  committedPoints: number;
  deliveredPoints: number;
  capacity?: number;
}

export interface ProjectVelocityDto {
  projectId: number;
  averageVelocity: number | null;
  sprints: SprintVelocityDto[];
}

// ── Burndown ──────────────────────────────────────────────────────────────────

export interface BurndownDayDto {
  date: string;
  idealPoints: number;
  remainingPoints: number | null;
}

export interface SprintBurndownDto {
  sprintId: number;
  name: string;
  status: string;
  startDate: string;
  endDate: string;
  totalPoints: number;
  days: BurndownDayDto[];
}

// ── Cycle time ────────────────────────────────────────────────────────────────

export interface WorkItemCycleTimeDto {
  workItemId: number;
  title: string;
  cycleTimeDays: number | null;
  leadTimeDays: number | null;
  doneAt: string;
}

export interface ProjectCycleTimeDto {
  projectId: number;
  averageCycleTimeDays: number | null;
  averageLeadTimeDays: number | null;
  completedItemsCount: number;
  items: WorkItemCycleTimeDto[];
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

  transitionStatus(
    projectId: number,
    id: number,
    status: SprintStatus,
    carryOver?: CarryOverTarget,
    targetSprintId?: number,
  ): Observable<void> {
    const body: Record<string, unknown> = { status };
    if (carryOver !== undefined) body['carryOver'] = carryOver;
    if (targetSprintId !== undefined) body['targetSprintId'] = targetSprintId;
    return this.http.post<void>(`/api/projects/${projectId}/sprints/${id}/status`, body);
  }

  assignToSprint(projectId: number, workItemId: number, sprintId: number | null): Observable<void> {
    return this.http.post<void>(`/api/projects/${projectId}/workitems/${workItemId}/sprint`, { sprintId });
  }

  getStatusHistory(projectId: number, id: number): Observable<SprintStatusHistoryEntry[]> {
    return this.http.get<SprintStatusHistoryEntry[]>(`/api/projects/${projectId}/sprints/${id}/status-history`);
  }

  getVelocity(projectId: number): Observable<ProjectVelocityDto> {
    return this.http.get<ProjectVelocityDto>(`/api/projects/${projectId}/velocity`);
  }

  getBurndown(projectId: number, sprintId: number): Observable<SprintBurndownDto> {
    return this.http.get<SprintBurndownDto>(`/api/projects/${projectId}/sprints/${sprintId}/burndown`);
  }

  getCycleTime(projectId: number): Observable<ProjectCycleTimeDto> {
    return this.http.get<ProjectCycleTimeDto>(`/api/projects/${projectId}/cycle-time`);
  }
}
