import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { PROJECT_STATUS_LABELS, ProjectStatus } from '../project.model';

interface StatusConfig {
  label: string;
  color: string;
}

const STATUS_COLORS: Record<ProjectStatus, string> = {
  Stopped: 'default',
  PlanningWithClient: 'blue',
  WaitingForDevelopers: 'gold',
  PlanningSprint: 'cyan',
  InSprint: 'green',
  DevelopmentOutsideSprint: 'geekblue',
  InTesting: 'orange',
  Completed: 'purple',
  PostponedByClient: 'red',
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
    const s = this.status as ProjectStatus;
    return {
      label: PROJECT_STATUS_LABELS[s] ?? this.status,
      color: STATUS_COLORS[s] ?? 'default',
    };
  }
}
