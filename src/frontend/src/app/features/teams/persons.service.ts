import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { PagedResult, Person, PersonRole } from './team.model';

@Injectable({ providedIn: 'root' })
export class PersonsService {
  private readonly api = inject(ApiService);

  getPersons(page = 1, pageSize = 100): Observable<PagedResult<Person>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.api.http.get<PagedResult<Person>>(`${this.api.baseUrl}/persons`, { params });
  }
}
