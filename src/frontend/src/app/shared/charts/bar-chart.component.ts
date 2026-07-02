import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export interface ChartSeries {
  name: string;
  color: string;
  values: number[];
}

const AXIS_COLOR = '#d9d9d9';
const LABEL_COLOR = '#8c8c8c';
const FONT = '11px system-ui, sans-serif';

@Component({
  selector: 'app-bar-chart',
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
        <!-- Y axis grid lines and labels -->
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

        <!-- Bars -->
        @for (group of barGroups(); track group.index) {
          @for (bar of group.bars; track bar.seriesIndex) {
            <rect
              [attr.x]="bar.x"
              [attr.y]="bar.y"
              [attr.width]="bar.width"
              [attr.height]="bar.barHeight"
              [attr.fill]="bar.color"
              rx="2"
            />
            @if (bar.barHeight > 14) {
              <text
                [attr.x]="bar.x + bar.width / 2"
                [attr.y]="bar.y + bar.barHeight - 4"
                text-anchor="middle"
                fill="#fff"
                font-size="10"
                font-weight="600"
              >{{ bar.value }}</text>
            }
          }

          <!-- X-axis label -->
          <text
            [attr.x]="group.centerX"
            [attr.y]="height() - paddingBottom + 14"
            text-anchor="middle"
            [attr.fill]="LABEL_COLOR"
            font-size="11"
          >{{ group.label }}</text>
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

        <!-- Legend -->
        @for (s of series(); track s.name; let i = $index) {
          <rect
            [attr.x]="paddingLeft + i * 120"
            [attr.y]="height() - 18"
            width="10" height="10"
            [attr.fill]="s.color"
            rx="2"
          />
          <text
            [attr.x]="paddingLeft + i * 120 + 14"
            [attr.y]="height() - 9"
            [attr.fill]="LABEL_COLOR"
            font-size="11"
          >{{ s.name }}</text>
        }
      </svg>
    </div>
  `,
})
export class BarChartComponent {
  readonly labels = input<string[]>([]);
  readonly series = input<ChartSeries[]>([]);
  readonly height = input<number>(260);

  readonly paddingLeft = 40;
  readonly paddingRight = 16;
  readonly paddingTop = 16;
  readonly paddingBottom = 50; // room for x labels + legend

  readonly AXIS_COLOR = AXIS_COLOR;
  readonly LABEL_COLOR = LABEL_COLOR;

  readonly groupCount = computed(() => this.labels().length);

  readonly svgWidth = computed(() => {
    const n = this.groupCount();
    const minColWidth = (this.series().length || 1) * 20 + 20;
    return Math.max(400, this.paddingLeft + this.paddingRight + n * Math.max(minColWidth, 64));
  });

  readonly chartBottom = computed(() => this.height() - this.paddingBottom);
  readonly chartHeight = computed(() => this.chartBottom() - this.paddingTop);

  readonly maxValue = computed(() => {
    const all = this.series().flatMap(s => s.values);
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

  readonly barGroups = computed(() => {
    const labels = this.labels();
    const series = this.series();
    const n = labels.length;
    const colWidth = n > 0 ? (this.svgWidth() - this.paddingLeft - this.paddingRight) / n : 0;
    const seriesCount = series.length || 1;
    const groupPad = colWidth * 0.15;
    const barWidth = Math.max(6, (colWidth - groupPad * 2) / seriesCount - 2);
    const max = this.maxValue();
    const scale = max > 0 ? this.chartHeight() / (max * 1.1) : 1;

    return labels.map((label, gi) => {
      const groupX = this.paddingLeft + gi * colWidth + groupPad;
      const centerX = this.paddingLeft + gi * colWidth + colWidth / 2;

      const bars = series.map((s, si) => {
        const value = s.values[gi] ?? 0;
        const barH = Math.max(0, value * scale);
        const x = groupX + si * (barWidth + 2);
        const y = this.chartBottom() - barH;
        return {
          seriesIndex: si,
          x,
          y,
          width: barWidth,
          barHeight: barH,
          color: s.color,
          value,
        };
      });

      return { index: gi, label, centerX, bars };
    });
  });
}
