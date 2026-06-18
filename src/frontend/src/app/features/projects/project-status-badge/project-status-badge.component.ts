import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { PROJECT_STATUS_LABELS, PROJECT_STATUS_PILL_COLORS, ProjectStatus } from '../project.model';

interface StatusConfig {
  label: string;
  bg: string;
  fg: string;
  dot: string;
}

@Component({
  selector: 'app-project-status-badge',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      style="display:inline-flex;align-items:center;gap:6px;padding:3px 10px;border-radius:999px;font-size:12.5px;font-weight:600;white-space:nowrap"
      [style.background]="config().bg"
      [style.color]="config().fg"
    >
      <span style="width:6px;height:6px;border-radius:999px;flex:0 0 auto" [style.background]="config().dot"></span>
      {{ config().label }}
    </span>
  `,
})
export class ProjectStatusBadgeComponent {
  @Input({ required: true }) status!: ProjectStatus | string;

  config(): StatusConfig {
    const s = this.status as ProjectStatus;
    const colors = PROJECT_STATUS_PILL_COLORS[s];
    return {
      label: PROJECT_STATUS_LABELS[s] ?? this.status,
      bg: colors?.bg ?? '#ECEAE6',
      fg: colors?.fg ?? '#6B6661',
      dot: colors?.dot ?? '#9A938B',
    };
  }
}
