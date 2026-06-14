import {
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzDividerModule } from 'ng-zorro-antd/divider';

interface MemberCapacity {
  personId: number; name: string; role: string;
  activeTasks: number; pendingTasks: number; doneTasks: number;
  loadLevel: 'Green' | 'Yellow' | 'Red';
}
interface TeamCapacity {
  teamId: number; teamName: string; leadName?: string;
  activeProjectCount: number;
  members: MemberCapacity[];
}

const LOAD_COLORS = { Green: '#52c41a', Yellow: '#faad14', Red: '#ff4d4f' };
const LOAD_LABELS = { Green: 'Carga baja', Yellow: 'Carga media', Red: 'Sobrecargado' };
const ROLE_LABELS: Record<string, string> = {
  Gestor: 'Gestor', JefeEquipo: 'Jefe equipo', Desarrollador: 'Desarrollador',
};

@Component({
  selector: 'app-capacity',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NzCardModule, NzTagModule, NzIconModule, NzSpinModule, NzEmptyModule,
    NzProgressModule, NzTooltipModule, NzBadgeModule, NzDividerModule,
  ],
  styles: [`
    .header { margin-bottom: 24px; }
    .header h2 { margin: 0 0 4px; font-size: 22px; }
    .header p  { margin: 0; color: #8c8c8c; font-size: 13px; }

    .teams-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(360px, 1fr)); gap: 16px; }

    .team-card-header {
      display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 16px;
    }
    .team-name { font-size: 15px; font-weight: 700; margin: 0 0 2px; }
    .team-meta { font-size: 12px; color: #8c8c8c; }

    .member-row {
      display: flex; align-items: center; gap: 10px; padding: 8px 0;
      border-bottom: 1px solid #f5f5f5;
    }
    .member-row:last-child { border-bottom: none; }
    .member-name { font-weight: 600; font-size: 13px; }
    .member-role { font-size: 11px; color: #8c8c8c; }
    .member-stats { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .load-indicator {
      width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0;
    }

    .legend { display: flex; gap: 16px; margin-bottom: 16px; font-size: 12px; color: #595959; align-items: center; }
    .legend-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; margin-right: 4px; }
  `],
  template: `
    <div class="header">
      <h2><span nz-icon nzType="team" style="color:#1890ff;margin-right:8px"></span>Capacidad de equipos</h2>
      <p>Carga de trabajo actual por equipo y persona</p>
    </div>

    <!-- Leyenda -->
    <div class="legend">
      <span><span class="legend-dot" style="background:#52c41a"></span> Carga baja (≤3 tareas activas)</span>
      <span><span class="legend-dot" style="background:#faad14"></span> Carga media (4–6 tareas activas)</span>
      <span><span class="legend-dot" style="background:#ff4d4f"></span> Sobrecargado (≥7 tareas activas)</span>
    </div>

    @if (!teams()) {
      <div style="display:flex;justify-content:center;padding:80px"><nz-spin nzSize="large" /></div>
    } @else if (teams()!.length === 0) {
      <nz-empty nzNotFoundContent="Sin equipos" />
    } @else {
      <div class="teams-grid">
        @for (team of teams()!; track team.teamId) {
          <nz-card [nzBordered]="true">
            <div class="team-card-header">
              <div>
                <div class="team-name">
                  <span nz-icon nzType="team" style="color:#1890ff;margin-right:6px"></span>
                  {{ team.teamName }}
                </div>
                <div class="team-meta">
                  @if (team.leadName) { 👤 {{ team.leadName }} · }
                  {{ team.members.length }} miembro{{ team.members.length !== 1 ? 's' : '' }} ·
                  {{ team.activeProjectCount }} proyecto{{ team.activeProjectCount !== 1 ? 's' : '' }} activo{{ team.activeProjectCount !== 1 ? 's' : '' }}
                </div>
              </div>
              <div style="text-align:right">
                <div style="font-size:11px;color:#8c8c8c;margin-bottom:4px">Carga global</div>
                <nz-progress
                  nzType="circle"
                  [nzPercent]="teamLoadPct(team)"
                  [nzWidth]="48"
                  [nzStrokeColor]="teamLoadColor(team)"
                  [nzFormat]="fmtPct">
                </nz-progress>
              </div>
            </div>

            @if (team.members.length === 0) {
              <nz-empty nzNotFoundContent="Sin miembros" />
            } @else {
              @for (m of team.members; track m.personId) {
                <div class="member-row">
                  <div [style.background]="LOAD_COLORS[m.loadLevel]" class="load-indicator"
                    nz-tooltip [nzTooltipTitle]="LOAD_LABELS[m.loadLevel]"></div>
                  <span style="width:32px;height:32px;border-radius:50%;display:flex;align-items:center;justify-content:center;color:#fff;font-weight:700;font-size:14px;flex-shrink:0"
                    [style.background]="avatarBg(m.loadLevel)">
                    {{ m.name[0] }}
                  </span>
                  <div style="flex:1;min-width:0">
                    <div class="member-name">{{ m.name }}</div>
                    <div class="member-role">{{ ROLE_LABELS[m.role] ?? m.role }}</div>
                  </div>
                  <div class="member-stats">
                    <span nz-tooltip nzTooltipTitle="Tareas activas (InProgress + Bloqueadas)"
                      [style.color]="LOAD_COLORS[m.loadLevel]" style="font-weight:700;font-size:13px">
                      {{ m.activeTasks }}
                    </span>
                    <span style="color:#bfbfbf">|</span>
                    <span nz-tooltip nzTooltipTitle="Pendientes (ToDo + Backlog)"
                      style="font-size:12px;color:#595959">
                      {{ m.pendingTasks }} pend.
                    </span>
                    <span style="color:#bfbfbf">|</span>
                    <span nz-tooltip nzTooltipTitle="Completadas"
                      style="font-size:12px;color:#52c41a">
                      {{ m.doneTasks }} ✓
                    </span>
                  </div>
                </div>
              }
            }
          </nz-card>
        }
      </div>
    }
  `,
})
export class CapacityComponent {
  private readonly http = inject(HttpClient);

  readonly teams = toSignal(this.http.get<TeamCapacity[]>('/api/capacity'));

  readonly LOAD_COLORS = LOAD_COLORS;
  readonly LOAD_LABELS = LOAD_LABELS;
  readonly ROLE_LABELS = ROLE_LABELS;
  readonly fmtPct = (p: number) => `${p}%`;

  teamLoadPct(team: TeamCapacity): number {
    if (team.members.length === 0) return 0;
    const red    = team.members.filter(m => m.loadLevel === 'Red').length;
    const yellow = team.members.filter(m => m.loadLevel === 'Yellow').length;
    return Math.round(((red * 2 + yellow) / (team.members.length * 2)) * 100);
  }

  teamLoadColor(team: TeamCapacity): string {
    const pct = this.teamLoadPct(team);
    return pct >= 60 ? '#ff4d4f' : pct >= 30 ? '#faad14' : '#52c41a';
  }

  avatarBg(loadLevel: string): string {
    const map: Record<string, string> = { Green: '#52c41a', Yellow: '#faad14', Red: '#ff4d4f' };
    return map[loadLevel] ?? '#1890ff';
  }
}
