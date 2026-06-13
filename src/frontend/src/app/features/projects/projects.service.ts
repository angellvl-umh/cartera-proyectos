import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateProjectDto,
  PagedResult,
  Project,
  ProjectDetail,
  ProjectFilters,
  ProjectStatus,
  Team,
  UpdateProjectDto,
} from './project.model';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/projects';

  getProjects(filters?: ProjectFilters, page = 1, pageSize = 20): Observable<PagedResult<Project>> {
    let params = new HttpParams()
      .set('page', (filters?.page ?? page).toString())
      .set('pageSize', (filters?.pageSize ?? pageSize).toString());
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.year) params = params.set('year', filters.year.toString());
    if (filters?.teamId) params = params.set('teamId', filters.teamId);
    if (filters?.complexity) params = params.set('complexity', filters.complexity);
    if (filters?.q) params = params.set('q', filters.q);
    return this.http.get<PagedResult<Project>>(this.base, { params });
  }

  getProject(id: string): Observable<ProjectDetail> {
    return this.http.get<ProjectDetail>(`${this.base}/${id}`);
  }

  createProject(data: CreateProjectDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, data);
  }

  updateProject(id: string, data: UpdateProjectDto): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, data);
  }

  deleteProject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  transitionStatus(id: string, status: ProjectStatus): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/status`, { status });
  }

  assignTeam(id: string, teamId: string, isPrimary: boolean): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/teams`, { teamId, isPrimary });
  }

  removeTeam(id: string, teamId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/teams/${teamId}`);
  }

  getTeams(): Observable<Team[]> {
    return this.http.get<Team[]>('/api/teams');
  }
}
