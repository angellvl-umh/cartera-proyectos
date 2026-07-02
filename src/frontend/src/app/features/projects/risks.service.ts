import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateRiskDto,
  DependencyItemDto,
  PagedResult,
  ProjectDependenciesDto,
  ProjectRiskDto,
  UpdateRiskDto,
} from './project.model';

@Injectable({ providedIn: 'root' })
export class RisksService {
  private readonly http = inject(HttpClient);

  // ── Risks ──────────────────────────────────────────────────────────────────

  getRisks(projectId: number, page = 1, pageSize = 100): Observable<PagedResult<ProjectRiskDto>> {
    return this.http.get<PagedResult<ProjectRiskDto>>(
      `/api/projects/${projectId}/risks?page=${page}&pageSize=${pageSize}`
    );
  }

  createRisk(projectId: number, dto: CreateRiskDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`/api/projects/${projectId}/risks`, dto);
  }

  updateRisk(projectId: number, riskId: number, dto: UpdateRiskDto): Observable<void> {
    return this.http.put<void>(`/api/projects/${projectId}/risks/${riskId}`, dto);
  }

  deleteRisk(projectId: number, riskId: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${projectId}/risks/${riskId}`);
  }

  // ── Dependencies ───────────────────────────────────────────────────────────

  getDependencies(projectId: number): Observable<ProjectDependenciesDto> {
    return this.http.get<ProjectDependenciesDto>(`/api/projects/${projectId}/dependencies`);
  }

  createDependency(
    projectId: number,
    dependsOnProjectId: number,
    description?: string | null
  ): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`/api/projects/${projectId}/dependencies`, {
      dependsOnProjectId,
      description: description ?? null,
    });
  }

  deleteDependency(projectId: number, dependencyId: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${projectId}/dependencies/${dependencyId}`);
  }
}
