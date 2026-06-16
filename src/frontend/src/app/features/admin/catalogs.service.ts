import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OrganicUnitDto, PagedResult, PromoterDto, TagDto } from '../projects/project.model';

@Injectable({ providedIn: 'root' })
export class CatalogsService {
  private readonly http = inject(HttpClient);

  // Promoters
  getPromoters(page = 1, pageSize = 20): Observable<PagedResult<PromoterDto>> {
    return this.http.get<PagedResult<PromoterDto>>('/api/promoters', {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  createPromoter(name: string): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/promoters', { name });
  }

  updatePromoter(id: number, name: string): Observable<void> {
    return this.http.put<void>(`/api/promoters/${id}`, { name });
  }

  deletePromoter(id: number): Observable<void> {
    return this.http.delete<void>(`/api/promoters/${id}`);
  }

  // Organic Units
  getOrganicUnits(q?: string, page = 1, pageSize = 20): Observable<PagedResult<OrganicUnitDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (q) params = params.set('q', q);
    return this.http.get<PagedResult<OrganicUnitDto>>('/api/organic-units', { params });
  }

  createOrganicUnit(name: string, code?: string | null): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/organic-units', { name, code });
  }

  updateOrganicUnit(id: number, name: string, code?: string | null): Observable<void> {
    return this.http.put<void>(`/api/organic-units/${id}`, { name, code });
  }

  deleteOrganicUnit(id: number): Observable<void> {
    return this.http.delete<void>(`/api/organic-units/${id}`);
  }

  // Tags
  getTags(): Observable<TagDto[]> {
    return this.http.get<TagDto[]>('/api/tags');
  }

  createTag(name: string, color?: string | null): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/tags', { name, color });
  }

  updateTag(id: number, name: string, color?: string | null): Observable<void> {
    return this.http.put<void>(`/api/tags/${id}`, { name, color });
  }

  deleteTag(id: number): Observable<void> {
    return this.http.delete<void>(`/api/tags/${id}`);
  }
}
