import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export interface LineChartSeries {
  name: string;
  color: string;
  dashed?: boolean;
  values: (number | null)[];
}

const AXIS_COLOR = '#d9d9d9';
const LABEL_COLOR = '#8c8c8c';

@Component({
  selector: 'app-line-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div style="width:100%;overflow-x:auto">
      <svg
        [attr.width]="svgWidth()"
        [attr.height]="height()"
        [attr.viewBox]="'0 0 ' + svgWidth() + ' ' + height()"
        style="display:block;font-family:system-ui,sans-serif"
      >
        <!-- Y grid lines and labels -->
        @for (tick of yTicks(); track tick.value) {
          <line
            [attr.x1]="paddingLeft"
            [attr.x2]="svgWidth() - paddingRight"
            [attr.y1]="tick.y"
            [attr.y2]="tick.y"
            [attr.stroke]="tick.value === 0 ? '#bfbfbf' : AXIS_COLOR"
            stroke-width="1"
          />
          <text
            [attr.x]="paddingLeft - 6"
            [attr.y]="tick.y + 4"
            text-anchor="end"
            [attr.fill]="LABEL_COLOR"
            font-size="11"
          >{{ tick.label }}</text>
        }

        <!-- X axis labels (every Nth label to avoid overlap) -->
        @for (lbl of xLabels(); track lbl.index) {
          <text
            [attr.x]="lbl.x"
            [attr.y]="chartBottom() + 14"
            text-anchor="middle"
            [attr.fill]="LABEL_COLOR"
            font-size="10"
          >{{ lbl.label }}</text>
        }

        <!-- X axis line -->
        <line
          [attr.x1]="paddingLeft"
          [attr.x2]="svgWidth() - paddingRight"
          [attr.y1]="chartBottom()"
          [attr.y2]="chartBottom()"
          stroke="#bfbfbf"
          stroke-width="1"
        />

        <!-- Series lines -->
        @for (s of seriesData(); track s.name) {
          <polyline
            [attr.points]="s.points"
            fill="none"
            [attr.stroke]="s.color"
            stroke-width="2"
            [attr.stroke-dasharray]="s.dashed ? '6 4' : 'none'"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
          <!-- Dots for real (non-dashed) series -->
          @if (!s.dashed) {
            @for (dot of s.dots; track dot.index) {
              <circle
                [attr.cx]="dot.x"
                [attr.cy]="dot.y"
                r="3"
                [attr.fill]="s.color"
              />
            }
          }
        }

        <!-- Legend -->
        @for (s of series(); track s.name; let i = $index) {
          <line
            [attr.x1]="paddingLeft + i * 140"
            [attr.y1]="height() - 10"
            [attr.x2]="paddingLeft + i * 140 + 18"
            [attr.y2]="height() - 10"
            [attr.stroke]="s.color"
            stroke-width="2"
            [attr.stroke-dasharray]="s.dashed ? '4 3' : 'none'"
          />
          <text
            [attr.x]="paddingLeft + i * 140 + 22"
            [attr.y]="height() - 6"
            [attr.fill]="LABEL_COLOR"
            font-size="11"
          >{{ s.name }}</text>
        }
      </svg>
    </div>
  `,
})
export class LineChartComponent {
  readonly labels = input<string[]>([]);
  readonly series = input<LineChartSeries[]>([]);
  readonly height = input<number>(260);

  readonly paddingLeft = 44;
  readonly paddingRight = 16;
  readonly paddingTop = 16;
  readonly paddingBottom = 44;

  readonly AXIS_COLOR = AXIS_COLOR;
  readonly LABEL_COLOR = LABEL_COLOR;

  readonly svgWidth = computed(() => {
    const n = this.labels().length;
    return Math.max(400, this.paddingLeft + this.paddingRight + n * 24);
  });

  readonly chartBottom = computed(() => this.height() - this.paddingBottom);
  readonly chartHeight = computed(() => this.chartBottom() - this.paddingTop);
  readonly chartWidth = computed(() => this.svgWidth() - this.paddingLeft - this.paddingRight);

  readonly maxValue = computed(() => {
    const all = this.series().flatMap(s => s.values).filter((v): v is number => v !== null);
    return all.length ? Math.max(...all, 1) : 1;
  });

  readonly yTicks = computed(() => {
    const max = this.maxValue();
    const count = 5;
    const step = Math.ceil(max / count) || 1;
    const ticks = [];
    for (let v = 0; v <= max + step; v += step) {
      const y = this.chartBottom() - (v / (max + step)) * this.chartHeight();
      ticks.push({ value: v, y, label: String(v) });
    }
    return ticks;
  });

  private xForIndex(i: number, n: number): number {
    if (n <= 1) return this.paddingLeft + this.chartWidth() / 2;
    return this.paddingLeft + (i / (n - 1)) * this.chartWidth();
  }

  private yForValue(v: number): number {
    const max = this.maxValue();
    return this.chartBottom() - (v / (max * 1.1)) * this.chartHeight();
  }

  readonly xLabels = computed(() => {
    const labels = this.labels();
    const n = labels.length;
    // Show at most ~10 labels to avoid clutter
    const step = Math.max(1, Math.ceil(n / 10));
    return labels
      .map((label, i) => ({ index: i, label, x: this.xForIndex(i, n) }))
      .filter((_, i) => i % step === 0 || i === n - 1);
  });

  readonly seriesData = computed(() => {
    const labels = this.labels();
    const n = labels.length;

    return this.series().map(s => {
      // Build continuous segments: only connect points that are non-null and adjacent (or skip nulls)
      const validPoints: { x: number; y: number; index: number }[] = [];
      for (let i = 0; i < n; i++) {
        const v = s.values[i];
        if (v !== null && v !== undefined) {
          validPoints.push({ x: this.xForIndex(i, n), y: this.yForValue(v), index: i });
        }
      }

      const pointsStr = validPoints.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');

      return {
        name: s.name,
        color: s.color,
        dashed: s.dashed ?? false,
        points: pointsStr,
        dots: validPoints,
      };
    });
  });
}
