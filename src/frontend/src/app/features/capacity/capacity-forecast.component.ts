import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';

export interface ForecastQuarterDto {
  quarter: number;
  demandPersonMonths: number;
  capacityPersonMonths: number;
  loadPercent: number;
  level: 'Green' | 'Yellow' | 'Red';
  projectTitles: string[];
}

export interface ForecastTeamDto {
  teamId: number;
  teamName: string;
  memberCount: number;
  quarters: ForecastQuarterDto[];
}

export interface CapacityForecastDto {
  year: number;
  methodologyNote: string;
  teams: ForecastTeamDto[];
}

const LEVEL_BG: Record<string, string> = {
  Green: '#E7F2EC',
  Yellow: '#F6F0D6',
  Red: '#FBE9E7',
};
const LEVEL_FG: Record<string, string> = {
  Green: '#1C7A4B',
  Yellow: '#8A6B10',
  Red: '#A8401F',
};

@Component({
  selector: 'app-capacity-forecast',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    FormsModule,
    DecimalPipe,
    NzSelectModule,
    NzSpinModule,
    NzEmptyModule,
    NzAlertModule,
    NzTableModule,
    NzTooltipModule,
    NzTagModule,
    NzIconModule,
    NzButtonModule,
  ],
  styles: [`
    .header { margin-bottom: 20px; }
    .header h2 { margin: 0 0 4px; font-size: 22px; }

    .top-actions {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 16px;
    }

    .cell-inner {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 6px 8px;
      border-radius: 6px;
      min-width: 90px;
    }
    .load-pct {
      font-size: 17px;
      font-weight: 700;
      line-height: 1.2;
    }
    .pm-label {
      font-size: 11px;
      margin-top: 3px;
      opacity: 0.8;
    }
  `],
  template: `
    <div class="header">
      <h2>
        <span nz-icon nzType="line-chart" style="color:#1890ff;margin-right:8px"></span>
        Previsión de capacidad anual
      </h2>
      <p style="margin:0;color:#8c8c8c;font-size:13px">
        Demanda estimada vs. capacidad disponible por equipo y trimestre
      </p>
    </div>

    <!-- Controls -->
    <div class="top-actions">
      <nz-select [(ngModel)]="selectedYear" (ngModelChange)="loadForecast($event)" style="width:120px">
        @for (y of availableYears; track y) {
          <nz-option [nzValue]="y" [nzLabel]="y.toString()" />
        }
      </nz-select>

      <a routerLink="/capacity" nz-button nzType="default" style="margin-left:auto">
        <span nz-icon nzType="arrow-left"></span> Carga actual
      </a>
    </div>

    @if (forecast()?.methodologyNote; as note) {
      <nz-alert
        nzType="info"
        [nzMessage]="note"
        nzShowIcon
        style="margin-bottom:16px;font-size:12px">
      </nz-alert>
    }

    @if (loading()) {
      <div style="display:flex;justify-content:center;padding:80px">
        <nz-spin nzSize="large" />
      </div>
    } @else if (!forecast() || forecast()!.teams.length === 0) {
      <nz-empty nzNotFoundContent="Sin datos de previsión" />
    } @else {

      <nz-table
        [nzData]="forecast()!.teams"
        nzBordered
        nzSize="middle"
        [nzShowPagination]="false">
        <thead>
          <tr>
            <th style="width:200px">Equipo</th>
            <th style="text-align:center">Q1 · Ene–Mar</th>
            <th style="text-align:center">Q2 · Abr–Jun</th>
            <th style="text-align:center">Q3 · Jul–Sep</th>
            <th style="text-align:center">Q4 · Oct–Dic</th>
          </tr>
        </thead>
        <tbody>
          @for (team of forecast()!.teams; track team.teamId) {
            <tr>
              <td>
                <div style="font-weight:700;font-size:13px">{{ team.teamName }}</div>
                <div style="font-size:12px;color:#8c8c8c">{{ team.memberCount }} miembro{{ team.memberCount !== 1 ? 's' : '' }}</div>
              </td>
              @for (q of team.quarters; track q.quarter) {
                <td style="padding:6px 8px;text-align:center">
                  <div class="cell-inner"
                    [style.background]="levelBg(q.level)"
                    nz-tooltip
                    [nzTooltipTitle]="q.projectTitles.length ? q.projectTitles.join(', ') : 'Sin proyectos asignados'"
                    [nzTooltipPlacement]="'top'">
                    <span class="load-pct" [style.color]="levelFg(q.level)">
                      {{ q.loadPercent >= 999 ? '∞' : q.loadPercent + '%' }}
                    </span>
                    <span class="pm-label" [style.color]="levelFg(q.level)">
                      {{ q.demandPersonMonths | number:'1.1-1' }} / {{ q.capacityPersonMonths | number:'1.1-1' }} p·m
                    </span>
                  </div>
                </td>
              }
            </tr>
          }
        </tbody>
      </nz-table>

      <!-- Legend -->
      <div style="display:flex;gap:16px;margin-top:12px;font-size:12px;color:#595959;flex-wrap:wrap">
        <span>
          <span style="display:inline-block;width:12px;height:12px;border-radius:3px;background:#E7F2EC;border:1px solid #1C7A4B;margin-right:4px;vertical-align:middle"></span>
          Verde &lt; 70%
        </span>
        <span>
          <span style="display:inline-block;width:12px;height:12px;border-radius:3px;background:#F6F0D6;border:1px solid #8A6B10;margin-right:4px;vertical-align:middle"></span>
          Amarillo 70–100%
        </span>
        <span>
          <span style="display:inline-block;width:12px;height:12px;border-radius:3px;background:#FBE9E7;border:1px solid #A8401F;margin-right:4px;vertical-align:middle"></span>
          Rojo &gt; 100%
        </span>
        <span style="color:#8c8c8c">· p·m = persona-mes</span>
      </div>
    }
  `,
})
export class CapacityForecastComponent {
  private readonly http = inject(HttpClient);

  loading = signal(false);
  forecast = signal<CapacityForecastDto | null>(null);
  selectedYear = new Date().getFullYear();

  readonly availableYears: number[] = (() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1, current + 2];
  })();

  constructor() {
    this.loadForecast(this.selectedYear);
  }

  loadForecast(year: number): void {
    this.selectedYear = year;
    this.loading.set(true);
    this.http.get<CapacityForecastDto>(`/api/capacity/forecast?year=${year}`).subscribe({
      next: data => { this.forecast.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  levelBg(level: string): string {
    return LEVEL_BG[level] ?? '#fafafa';
  }

  levelFg(level: string): string {
    return LEVEL_FG[level] ?? '#595959';
  }
}
