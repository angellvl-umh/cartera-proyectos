import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { map } from 'rxjs';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ProjectsService } from '../projects.service';
import {
  CreateProjectDto,
  ProjectComplexity,
  ProjectDetail,
} from '../project.model';

@Component({
  selector: 'app-project-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzInputNumberModule,
    NzDatePickerModule,
    NzModalModule,
    NzButtonModule,
  ],
  template: `
    <nz-modal
      [nzVisible]="visible"
      [nzTitle]="project ? 'Editar proyecto' : 'Nuevo proyecto'"
      [nzOkText]="saving() ? 'Guardando...' : 'Guardar'"
      [nzOkLoading]="saving()"
      [nzCancelText]="'Cancelar'"
      (nzOnOk)="submit()"
      (nzOnCancel)="cancel()"
    >
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="form" nzLayout="vertical">
          <nz-form-item>
            <nz-form-label nzRequired>Título</nz-form-label>
            <nz-form-control nzErrorTip="El título es obligatorio">
              <input nz-input formControlName="title" placeholder="Título del proyecto" />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Descripción</nz-form-label>
            <nz-form-control>
              <textarea
                nz-input
                formControlName="description"
                [nzAutosize]="{ minRows: 2, maxRows: 5 }"
                placeholder="Descripción opcional"
              ></textarea>
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label nzRequired>Unidad solicitante</nz-form-label>
            <nz-form-control nzErrorTip="La unidad solicitante es obligatoria">
              <input
                nz-input
                formControlName="requestingUnit"
                placeholder="Departamento o unidad"
              />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label nzRequired>Complejidad</nz-form-label>
            <nz-form-control nzErrorTip="Selecciona la complejidad">
              <nz-select formControlName="complexity" nzPlaceHolder="Selecciona complejidad">
                @for (opt of complexityOptions; track opt.value) {
                  <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Año de cartera</nz-form-label>
            <nz-form-control>
              <nz-input-number
                formControlName="portfolioYear"
                [nzMin]="2000"
                [nzMax]="2100"
                nzPlaceHolder="Ej: 2026"
                style="width: 100%"
              />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Fecha de inicio</nz-form-label>
            <nz-form-control>
              <nz-date-picker
                formControlName="startDate"
                nzPlaceHolder="Selecciona fecha"
                style="width: 100%"
              />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Fecha de fin</nz-form-label>
            <nz-form-control>
              <nz-date-picker
                formControlName="endDate"
                nzPlaceHolder="Selecciona fecha"
                style="width: 100%"
              />
            </nz-form-control>
          </nz-form-item>
        </form>
      </ng-container>
    </nz-modal>
  `,
})
export class ProjectFormComponent implements OnChanges {
  @Input() visible = false;
  @Input() project: ProjectDetail | null = null;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private readonly service = inject(ProjectsService);
  private readonly message = inject(NzMessageService);

  saving = signal(false);

  readonly complexityOptions: { value: ProjectComplexity; label: string }[] = [
    { value: 'Low', label: 'Baja' },
    { value: 'Medium', label: 'Media' },
    { value: 'High', label: 'Alta' },
    { value: 'VeryHigh', label: 'Muy alta' },
  ];

  form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl<string | null>(null),
    requestingUnit: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    complexity: new FormControl<ProjectComplexity>('Low', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    portfolioYear: new FormControl<number | null>(null),
    startDate: new FormControl<Date | null>(null),
    endDate: new FormControl<Date | null>(null),
  });

  ngOnChanges(): void {
    if (this.visible && this.project) {
      this.form.patchValue({
        title: this.project.title,
        description: this.project.description ?? null,
        requestingUnit: this.project.requestingUnit,
        complexity: this.project.complexity,
        portfolioYear: this.project.portfolioYear ?? null,
        startDate: this.project.startDate ? new Date(this.project.startDate) : null,
        endDate: this.project.endDate ? new Date(this.project.endDate) : null,
      });
    } else if (this.visible && !this.project) {
      this.form.reset();
    }
  }

  submit(): void {
    if (this.form.invalid) {
      Object.values(this.form.controls).forEach(c => {
        c.markAsDirty();
        c.updateValueAndValidity({ onlySelf: true });
      });
      return;
    }

    const raw = this.form.getRawValue();
    const payload: CreateProjectDto = {
      title: raw.title,
      requestingUnit: raw.requestingUnit,
      complexity: raw.complexity,
      ...(raw.description ? { description: raw.description } : {}),
      ...(raw.portfolioYear ? { portfolioYear: raw.portfolioYear } : {}),
      ...(raw.startDate
        ? { startDate: (raw.startDate as Date).toISOString().split('T')[0] }
        : {}),
      ...(raw.endDate
        ? { endDate: (raw.endDate as Date).toISOString().split('T')[0] }
        : {}),
    };

    this.saving.set(true);

    const op$ = this.project
      ? this.service.updateProject(this.project.id, payload)
      : this.service.createProject(payload).pipe(map(() => undefined as void));

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.message.success(this.project ? 'Proyecto actualizado' : 'Proyecto creado');
        this.saved.emit();
      },
      error: () => {
        this.saving.set(false);
        this.message.error('Error al guardar el proyecto');
      },
    });
  }

  cancel(): void {
    this.form.reset();
    this.cancelled.emit();
  }
}
