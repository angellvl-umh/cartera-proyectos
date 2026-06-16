import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateProjectDto,
  OrganicUnitDto,
  PagedResult,
  Project,
  ProjectDetail,
  ProjectFilters,
  ProjectNoteDto,
  ProjectStatus,
  PromoterDto,
  TagDto,
  Team,
  UpdateProjectDto,
} from './project.model';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/projects';

  getProjects(filters?: ProjectFilters): Observable<PagedResult<Project>> {
    let params = new HttpParams()
      .set('page', (filters?.page ?? 1).toString())
      .set('pageSize', (filters?.pageSize ?? 20).toString());
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.year) params = params.set('year', filters.year.toString());
    if (filters?.teamId) params = params.set('teamId', filters.teamId.toString());
    if (filters?.complexity) params = params.set('complexity', filters.complexity);
    if (filters?.q) params = params.set('q', filters.q);
    if (filters?.tagId) params = params.set('tagId', filters.tagId.toString());
    if (filters?.siptGroup) params = params.set('siptGroup', filters.siptGroup);
    if (filters?.promoterId) params = params.set('promoterId', filters.promoterId.toString());
    return this.http.get<PagedResult<Project>>(this.base, { params });
  }

  getProject(id: number): Observable<ProjectDetail> {
    return this.http.get<ProjectDetail>(`${this.base}/${id}`);
  }

  createProject(data: CreateProjectDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.base, data);
  }

  updateProject(id: number, data: UpdateProjectDto): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, data);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  transitionStatus(id: number, status: ProjectStatus): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/status`, { status });
  }

  assignTeam(id: number, teamId: number, isPrimary: boolean): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/teams`, { teamId, isPrimary });
  }

  removeTeam(id: number, teamId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/teams/${teamId}`);
  }

  getTeams(): Observable<PagedResult<Team>> {
    return this.http.get<PagedResult<Team>>('/api/teams');
  }

  // Notes
  getNotes(projectId: number): Observable<ProjectNoteDto[]> {
    return this.http.get<ProjectNoteDto[]>(`${this.base}/${projectId}/notes`);
  }

  createNote(projectId: number, text: string): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/${projectId}/notes`, { text });
  }

  deleteNote(projectId: number, noteId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${projectId}/notes/${noteId}`);
  }

  // Catalog lookups
  getPromoters(): Observable<PagedResult<PromoterDto>> {
    return this.http.get<PagedResult<PromoterDto>>('/api/promoters?pageSize=100');
  }

  getOrganicUnits(q?: string): Observable<PagedResult<OrganicUnitDto>> {
    let params = new HttpParams().set('pageSize', '100');
    if (q) params = params.set('q', q);
    return this.http.get<PagedResult<OrganicUnitDto>>('/api/organic-units', { params });
  }

  getTags(): Observable<TagDto[]> {
    return this.http.get<TagDto[]>('/api/tags');
  }
}
