import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzEmptyModule } from 'ng-zorro-antd/empty';

interface ActivityEventDto {
  type: string;
  occurredAt: string;
  projectId: number;
  projectTitle: string;
  actorId: number;
  actorName: string;
  summary: string;
}

interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

interface RefItem {
  id: number;
  name?: string;
  title?: string;
}

/** Configuración visual por tipo de evento: icono, color de tag y etiqueta. */
const EVENT_META: Record<string, { icon: string; color: string; label: string }> = {
  ProjectStatusChanged: { icon: 'sync', color: 'blue', label: 'Cambio de estado' },
  WorkItemCreated: { icon: 'plus-circle', color: 'geekblue', label: 'Tarea creada' },
  WorkItemCompleted: { icon: 'check-circle', color: 'green', label: 'Tarea completada' },
  CommentAdded: { icon: 'message', color: 'gold', label: 'Comentario' },
  WeeklyUpdateRegistered: { icon: 'file-text', color: 'purple', label: 'Avance semanal' },
};

@Component({
  selector: 'app-activity-feed',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule,
    NzCardModule, NzButtonModule, NzTagModule, NzIconModule,
    NzTableModule, NzSelectModule, NzEmptyModule,
  ],
  template: `
    <div style="max-width:1100px;margin:0 auto">
      <h2 style="margin:0 0 16px">Feed de actividad</h2>

      <!-- Filtros -->
      <nz-card nzTitle="Filtros" style="margin-bottom:16px">
        <div style="display:flex;gap:12px;flex-wrap:wrap;align-items:flex-end">
          <div>
            <div style="font-size:12px;color:#595959;margin-bottom:4px">Proyecto</div>
            <nz-select [(ngModel)]="filterProjectId" nzAllowClear nzShowSearch
              nzPlaceHolder="Todos los proyectos" style="width:240px">
              @for (p of projects()?.items ?? []; track p.id) {
                <nz-option [nzValue]="p.id" [nzLabel]="p.title ?? ''" />
              }
            </nz-select>
          </div>
          <div>
            <div style="font-size:12px;color:#595959;margin-bottom:4px">Equipo</div>
            <nz-select [(ngModel)]="filterTeamId" nzAllowClear nzShowSearch
              nzPlaceHolder="Todos los equipos" style="width:200px">
              @for (t of teams()?.items ?? []; track t.id) {
                <nz-option [nzValue]="t.id" [nzLabel]="t.name ?? ''" />
              }
            </nz-select>
          </div>
          <div>
            <div style="font-size:12px;color:#595959;margin-bottom:4px">Persona</div>
            <nz-select [(ngModel)]="filterPersonId" nzAllowClear nzShowSearch
              nzPlaceHolder="Todas las personas" style="width:200px">
              @for (p of persons()?.items ?? []; track p.id) {
                <nz-option [nzValue]="p.id" [nzLabel]="p.name ?? ''" />
              }
            </nz-select>
          </div>
          <button nz-button nzType="primary" [nzLoading]="loading()" (click)="applyFilters()">
            <span nz-icon nzType="filter"></span> Filtrar
          </button>
          <button nz-button (click)="clearFilters()">Limpiar</button>
        </div>
      </nz-card>

      <!-- Tabla de eventos -->
      <nz-table
        #table
        [nzData]="events()"
        [nzLoading]="loading()"
        [nzFrontPagination]="false"
        [nzTotal]="total()"
        [nzPageIndex]="page()"
        [nzPageSize]="pageSize()"
        [nzShowSizeChanger]="true"
        [nzPageSizeOptions]="[20, 50, 100]"
        (nzPageIndexChange)="onPageChange($event)"
        (nzPageSizeChange)="onPageSizeChange($event)"
        nzBordered>
        <thead>
          <tr>
            <th nzWidth="170px">Tipo</th>
            <th>Detalle</th>
            <th nzWidth="200px">Proyecto</th>
            <th nzWidth="150px">Persona</th>
            <th nzWidth="170px">Fecha</th>
          </tr>
        </thead>
        <tbody>
          @for (e of table.data; track $index) {
            <tr>
              <td>
                <nz-tag [nzColor]="meta(e.type).color">
                  <span nz-icon [nzType]="meta(e.type).icon"></span>
                  {{ meta(e.type).label }}
                </nz-tag>
              </td>
              <td style="white-space:pre-wrap">{{ e.summary }}</td>
              <td><a [routerLink]="['/projects', e.projectId]">{{ e.projectTitle }}</a></td>
              <td>{{ e.actorName }}</td>
              <td>{{ formatDate(e.occurredAt) }}</td>
            </tr>
          }
          @if (!loading() && events().length === 0) {
            <tr>
              <td colspan="5">
                <nz-empty nzNotFoundContent="No hay actividad para los filtros seleccionados" />
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    </div>
  `,
})
export class ActivityFeedComponent {
  private readonly http = inject(HttpClient);

  filterProjectId: number | null = null;
  filterTeamId: number | null = null;
  filterPersonId: number | null = null;

  readonly loading = signal(false);
  readonly result = signal<PagedResult<ActivityEventDto> | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(20);

  readonly events = computed(() => this.result()?.items ?? []);
  readonly total = computed(() => this.result()?.total ?? 0);

  readonly projects = toSignal(
    this.http.get<PagedResult<RefItem>>('/api/projects?pageSize=100')
  );
  readonly teams = toSignal(
    this.http.get<PagedResult<RefItem>>('/api/teams?pageSize=100')
  );
  readonly persons = toSignal(
    this.http.get<PagedResult<RefItem>>('/api/persons?pageSize=100')
  );

  constructor() {
    this.load();
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.filterProjectId = null;
    this.filterTeamId = null;
    this.filterPersonId = null;
    this.page.set(1);
    this.load();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.load();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  private load(): void {
    let params = new HttpParams()
      .set('page', this.page().toString())
      .set('pageSize', this.pageSize().toString());
    if (this.filterProjectId) params = params.set('projectId', this.filterProjectId.toString());
    if (this.filterTeamId) params = params.set('teamId', this.filterTeamId.toString());
    if (this.filterPersonId) params = params.set('personId', this.filterPersonId.toString());

    this.loading.set(true);
    this.http.get<PagedResult<ActivityEventDto>>('/api/activity', { params }).subscribe({
      next: r => { this.result.set(r); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  meta(type: string): { icon: string; color: string; label: string } {
    return EVENT_META[type] ?? { icon: 'question-circle', color: 'default', label: type };
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleString('es-ES', {
      day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }
}
