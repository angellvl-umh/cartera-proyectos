import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { PROJECT_COMPLEXITY_LABELS, PROJECT_COMPLEXITY_ORDER, ProjectComplexity } from '../project.model';

@Component({
  selector: 'app-complexity-indicator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span style="display:inline-flex;align-items:center;gap:8px">
      <span style="display:inline-flex;gap:2px">
        @for (i of segments; track i) {
          <span
            [style.width.px]="segmentWidth"
            [style.height.px]="segmentHeight"
            style="border-radius:2px"
            [style.background]="i <= level ? filledColor : emptyColor"
          ></span>
        }
      </span>
      @if (showLabel) {
        <span style="font-size:12.5px;color:#8A847C">{{ label }}</span>
      }
    </span>
  `,
})
export class ComplexityIndicatorComponent {
  @Input({ required: true }) complexity!: ProjectComplexity;
  @Input() size: 'table' | 'card' = 'table';
  @Input() showLabel = true;

  readonly filledColor = '#8A837A';
  readonly emptyColor = '#E6E1DA';
  readonly segments = [1, 2, 3, 4, 5];

  get level(): number {
    return PROJECT_COMPLEXITY_ORDER[this.complexity] ?? 0;
  }

  get label(): string {
    return PROJECT_COMPLEXITY_LABELS[this.complexity] ?? this.complexity;
  }

  get segmentWidth(): number {
    return this.size === 'card' ? 9 : 12;
  }

  get segmentHeight(): number {
    return this.size === 'card' ? 4 : 5;
  }
}
