import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  readonly http = inject(HttpClient);
  readonly baseUrl = '/api';
}
