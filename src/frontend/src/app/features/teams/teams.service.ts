import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { CreateTeamRequest, PagedResult, Team, TeamDetail, UpdateTeamRequest } from './team.model';

@Injectable({ providedIn: 'root' })
export class TeamsService {
  private readonly api = inject(ApiService);

  getTeams(page = 1, pageSize = 20): Observable<PagedResult<Team>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.api.http.get<PagedResult<Team>>(`${this.api.baseUrl}/teams`, { params });
  }

  getTeam(id: string): Observable<TeamDetail> {
    return this.api.http.get<TeamDetail>(`${this.api.baseUrl}/teams/${id}`);
  }

  createTeam(data: CreateTeamRequest): Observable<{ id: string }> {
    return this.api.http.post<{ id: string }>(`${this.api.baseUrl}/teams`, data);
  }

  updateTeam(id: string, data: UpdateTeamRequest): Observable<void> {
    return this.api.http.put<void>(`${this.api.baseUrl}/teams/${id}`, data);
  }

  deleteTeam(id: string): Observable<void> {
    return this.api.http.delete<void>(`${this.api.baseUrl}/teams/${id}`);
  }

  assignMember(teamId: string, personId: string): Observable<void> {
    return this.api.http.post<void>(`${this.api.baseUrl}/teams/${teamId}/members`, { personId });
  }

  removeMember(teamId: string, personId: string): Observable<void> {
    return this.api.http.delete<void>(`${this.api.baseUrl}/teams/${teamId}/members/${personId}`);
  }
}
