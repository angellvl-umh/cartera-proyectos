import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { ProjectStatus } from '../project.model';

interface StatusConfig {
  label: string;
  color: string;
}

const STATUS_CONFIG: Record<string, StatusConfig> = {
  Proposed: { label: 'Propuesto', color: 'blue' },
  Approved: { label: 'Aprobado', color: 'cyan' },
  InProgress: { label: 'En ejecución', color: 'green' },
  Paused: { label: 'Pausado', color: 'orange' },
  Completed: { label: 'Completado', color: 'purple' },
  Cancelled: { label: 'Cancelado', color: 'red' },
};

@Component({
  selector: 'app-project-status-badge',
  standalone: true,
  imports: [NzTagModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nz-tag [nzColor]="config().color">{{ config().label }}</nz-tag>
  `,
})
export class ProjectStatusBadgeComponent {
  @Input({ required: true }) status!: ProjectStatus | string;

  config(): StatusConfig {
    return STATUS_CONFIG[this.status] ?? { label: this.status, color: 'default' };
  }
}
