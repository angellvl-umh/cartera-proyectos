import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Person {
  id: number;
  name: string;
  email: string;
  role: 'Desarrollador' | 'JefeEquipo' | 'Gestor';
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class PersonsService {
  private readonly http = inject(HttpClient);

  getPersons(page = 1, pageSize = 20): Observable<PagedResult<Person>> {
    return this.http.get<PagedResult<Person>>(`/api/persons?page=${page}&pageSize=${pageSize}`);
  }

  updateRole(id: number, role: string): Observable<void> {
    return this.http.put<void>(`/api/persons/${id}/role`, { role });
  }
}
