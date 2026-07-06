import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Person {
  id: number;
  name: string;
  email: string;
  role: 'Desarrollador' | 'JefeEquipo' | 'Gestor';
  isActive: boolean;
  hasLoggedIn: boolean;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface PersonUpsertDto {
  name: string;
  email: string;
  role: 'Desarrollador' | 'Gestor';
}

export interface CreatePersonDto extends PersonUpsertDto {
  createLocalCredentials?: boolean;
}

export interface CreatePersonResponse {
  id: number;
  temporaryPassword?: string;
  credentialsWarning?: string;
}

@Injectable({ providedIn: 'root' })
export class PersonsService {
  private readonly http = inject(HttpClient);

  getPersons(page = 1, pageSize = 20, includeInactive = false): Observable<PagedResult<Person>> {
    return this.http.get<PagedResult<Person>>(
      `/api/persons?page=${page}&pageSize=${pageSize}&includeInactive=${includeInactive}`
    );
  }

  createPerson(data: CreatePersonDto): Observable<CreatePersonResponse> {
    return this.http.post<CreatePersonResponse>('/api/persons', data);
  }

  updatePerson(id: number, data: PersonUpsertDto): Observable<void> {
    return this.http.put<void>(`/api/persons/${id}`, data);
  }

  setActive(id: number, isActive: boolean): Observable<void> {
    return this.http.put<void>(`/api/persons/${id}/active`, { isActive });
  }
}
