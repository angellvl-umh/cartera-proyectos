import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  PROJECT_HEALTH_STATUS_COLORS,
  PROJECT_HEALTH_STATUS_LABELS,
  ProjectStatus,
} from '../projects/project.model';
import { ProjectStatusBadgeComponent } from '../projects/project-status-badge/project-status-badge.component';

interface WeeklyPortfolioProjectDto {
  projectId: number;
  title: string;
  primaryTeamName: string | null;
  status: string;
  isAtRisk: boolean;
  latestSummary: string | null;
  latestHealthStatus: string | null;
  latestAuthorName: string | null;
  latestUpdateDate: string | null;
  hasUpdateThisWeek: boolean;
  portfolioYear: number | null;
}

interface WeeklyPortfolioReportDto {
  atRiskProjects: WeeklyPortfolioProjectDto[];
  otherProjects: WeeklyPortfolioProjectDto[];
}

const CURRENT_YEAR = new Date().getFullYear();
const YEARS = Array.from({ length: 5 }, (_, i) => CURRENT_YEAR - 1 + i);

@Component({
  selector: 'app-weekly-portfolio-report',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule,
    NzCardModule, NzButtonModule, NzTagModule, NzIconModule,
    NzSpinModule, NzSelectModule, NzDividerModule,
    ProjectStatusBadgeComponent,
  ],
  template: `
    <div style="max-width:1100px;margin:0 auto">
      <h2 style="margin:0 0 16px">Informe semanal de cartera</h2>

      <!-- Filtros -->
      <nz-card nzTitle="Filtros" style="margin-bottom:16px">
        <div style="display:flex;gap:12px;flex-wrap:wrap;align-items:flex-end">
          <div>
            <div style="font-size:12px;color:#595959;margin-bottom:4px">Año</div>
            <nz-select [(ngModel)]="filterYear" nzAllowClear nzPlaceHolder="Todos los años" style="width:160px">
              @for (y of years; track y) {
                <nz-option [nzValue]="y" [nzLabel]="y.toString()" />
              }
            </nz-select>
          </div>
          <div>
            <div style="font-size:12px;color:#595959;margin-bottom:4px">Equipo</div>
            <nz-select [(ngModel)]="filterTeamId" nzAllowClear nzPlaceHolder="Todos los equipos"
              nzShowSearch style="width:200px">
              @for (t of teams()?.items ?? []; track t.id) {
                <nz-option [nzValue]="t.id" [nzLabel]="t.name" />
              }
            </nz-select>
          </div>
          <div>
            <div style="font-size:12px;color:#595959;margin-bottom:4px">Grupo SIPT</div>
            <nz-select [(ngModel)]="filterSiptGroup" nzAllowClear nzPlaceHolder="Todos los grupos" style="width:220px">
              <nz-option nzValue="WebTransversal" nzLabel="Web Transversal" />
              <nz-option nzValue="RRHH" nzLabel="RRHH" />
              <nz-option nzValue="Academico" nzLabel="Académico" />
              <nz-option nzValue="Sede" nzLabel="Sede" />
              <nz-option nzValue="Observatorio" nzLabel="Observatorio" />
              <nz-option nzValue="InvestigacionEconomico" nzLabel="Investigación / Económico" />
            </nz-select>
          </div>
          <button nz-button nzType="primary" [nzLoading]="loading()" (click)="generate()">
            <span nz-icon nzType="file-text"></span> Generar informe semanal
          </button>
        </div>
      </nz-card>

      <!-- Loading -->
      @if (loading()) {
        <div style="display:flex;justify-content:center;padding:64px">
          <nz-spin nzSize="large" />
        </div>
      }

      <!-- Results -->
      @if (report(); as r) {
        @if (!loading()) {
          <!-- At-risk -->
          <h3 style="margin:0 0 12px">
            <span nz-icon nzType="warning" style="color:#fa8c16;margin-right:6px"></span>
            Proyectos en riesgo ({{ r.atRiskProjects.length }})
          </h3>
          @if (r.atRiskProjects.length === 0) {
            <p style="font-size:15px;margin-bottom:24px">✅ Ningún proyecto en riesgo esta semana</p>
          } @else {
            <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(360px,1fr));gap:12px;margin-bottom:24px">
              @for (p of r.atRiskProjects; track p.projectId) {
                <nz-card [nzBodyStyle]="{ padding: '12px 16px' }"
                  style="border:1px solid #ffccc7;background:#fff1f0">
                  <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:8px;margin-bottom:8px">
                    <a [routerLink]="['/projects', p.projectId]" style="font-weight:600;font-size:14px">{{ p.title }}</a>
                    <app-project-status-badge [status]="asStatus(p.status)" />
                  </div>
                  <div style="font-size:12px;color:#595959;margin-bottom:8px">
                    <span nz-icon nzType="team" style="margin-right:4px"></span>
                    {{ p.primaryTeamName ?? 'Sin equipo' }}
                  </div>
                  @if (p.hasUpdateThisWeek) {
                    <nz-tag [nzColor]="healthColor(p.latestHealthStatus)">{{ healthLabel(p.latestHealthStatus) }}</nz-tag>
                    <p style="margin:8px 0 4px;font-size:13px;white-space:pre-wrap">{{ p.latestSummary }}</p>
                    <div style="font-size:11px;color:#8c8c8c">{{ p.latestAuthorName }} · {{ formatDate(p.latestUpdateDate) }}</div>
                  } @else {
                    <nz-tag nzColor="error">Sin actualización esta semana</nz-tag>
                    @if (p.latestSummary) {
                      <p style="margin:8px 0 4px;font-size:12px;color:#8c8c8c;white-space:pre-wrap">{{ p.latestSummary }}</p>
                      @if (p.latestUpdateDate) {
                        <div style="font-size:11px;color:#bfbfbf">Última: {{ formatDate(p.latestUpdateDate) }}</div>
                      }
                    }
                  }
                </nz-card>
              }
            </div>
          }

          <nz-divider />

          <!-- Other projects -->
          <h3 style="margin:0 0 12px">Resto de proyectos ({{ r.otherProjects.length }})</h3>
          <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(360px,1fr));gap:12px">
            @for (p of r.otherProjects; track p.projectId) {
              <nz-card [nzBodyStyle]="{ padding: '12px 16px' }">
                <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:8px;margin-bottom:8px">
                  <a [routerLink]="['/projects', p.projectId]" style="font-weight:600;font-size:14px">{{ p.title }}</a>
                  <app-project-status-badge [status]="asStatus(p.status)" />
                </div>
                <div style="font-size:12px;color:#595959;margin-bottom:8px">
                  <span nz-icon nzType="team" style="margin-right:4px"></span>
                  {{ p.primaryTeamName ?? 'Sin equipo' }}
                </div>
                @if (p.hasUpdateThisWeek) {
                  <nz-tag [nzColor]="healthColor(p.latestHealthStatus)">{{ healthLabel(p.latestHealthStatus) }}</nz-tag>
                  <p style="margin:8px 0 4px;font-size:13px;white-space:pre-wrap">{{ p.latestSummary }}</p>
                  <div style="font-size:11px;color:#8c8c8c">{{ p.latestAuthorName }} · {{ formatDate(p.latestUpdateDate) }}</div>
                } @else {
                  <nz-tag nzColor="error">Sin actualización esta semana</nz-tag>
                  @if (p.latestSummary) {
                    <p style="margin:8px 0 4px;font-size:12px;color:#8c8c8c;white-space:pre-wrap">{{ p.latestSummary }}</p>
                    @if (p.latestUpdateDate) {
                      <div style="font-size:11px;color:#bfbfbf">Última: {{ formatDate(p.latestUpdateDate) }}</div>
                    }
                  }
                }
              </nz-card>
            }
          </div>
        }
      }

      <!-- Initial empty state -->
      @if (!report() && !loading()) {
        <div style="text-align:center;padding:64px;color:#8c8c8c">
          <span nz-icon nzType="file-text" style="font-size:48px;display:block;margin-bottom:16px"></span>
          Pulsa "Generar informe semanal" para ver el estado de los proyectos.
        </div>
      }
    </div>
  `,
})
export class WeeklyPortfolioReportComponent {
  private readonly http = inject(HttpClient);

  filterYear: number | null = null;
  filterTeamId: number | null = null;
  filterSiptGroup: string | null = null;

  readonly years = YEARS;
  readonly loading = signal(false);
  readonly report = signal<WeeklyPortfolioReportDto | null>(null);

  readonly teams = toSignal(
    this.http.get<{ items: { id: number; name: string }[]; total: number; page: number; pageSize: number }>('/api/teams?pageSize=100')
  );

  generate(): void {
    let params = new HttpParams();
    if (this.filterYear) params = params.set('year', this.filterYear.toString());
    if (this.filterTeamId) params = params.set('teamId', this.filterTeamId.toString());
    if (this.filterSiptGroup) params = params.set('siptGroup', this.filterSiptGroup);

    this.loading.set(true);
    this.http.get<WeeklyPortfolioReportDto>('/api/reports/weekly-portfolio', { params }).subscribe({
      next: r => { this.report.set(r); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  asStatus(s: string): ProjectStatus { return s as ProjectStatus; }
  healthColor(s: string | null): string { return s ? (PROJECT_HEALTH_STATUS_COLORS as Record<string, string>)[s] ?? 'default' : 'default'; }
  healthLabel(s: string | null): string { return s ? (PROJECT_HEALTH_STATUS_LABELS as Record<string, string>)[s] ?? s : '—'; }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
