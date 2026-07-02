import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
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
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzRateModule } from 'ng-zorro-antd/rate';
import { NzMessageService } from 'ng-zorro-antd/message';
import { FormsModule } from '@angular/forms';
import { ProjectsService } from '../projects.service';
import {
  CreateProjectDto,
  OrganicUnitDto,
  PROJECT_COMPLEXITY_LABELS,
  PROJECT_STATUS_LABELS,
  ProjectComplexity,
  ProjectDetail,
  ProjectStatus,
  PromoterDto,
  SIPT_GROUP_LABELS,
  SiptGroup,
  TagDto,
} from '../project.model';

@Component({
  selector: 'app-project-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, FormsModule,
    NzFormModule, NzInputModule, NzSelectModule, NzInputNumberModule,
    NzDatePickerModule, NzModalModule, NzButtonModule, NzDividerModule, NzRateModule,
  ],
  template: `
    <nz-modal
      [nzVisible]="visible"
      [nzTitle]="project ? 'Editar proyecto' : 'Nuevo proyecto'"
      [nzOkText]="saving() ? 'Guardando...' : 'Guardar'"
      [nzOkLoading]="saving()"
      [nzWidth]="720"
      (nzOnOk)="submit()"
      (nzOnCancel)="cancel()"
    >
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="form" nzLayout="vertical">

          <nz-divider nzText="Datos básicos" nzOrientation="left"></nz-divider>

          <nz-form-item>
            <nz-form-label nzRequired>Título</nz-form-label>
            <nz-form-control nzErrorTip="El título es obligatorio (máx. 150 caracteres)">
              <input nz-input formControlName="title" placeholder="Título del proyecto" />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Descripción</nz-form-label>
            <nz-form-control>
              <textarea nz-input formControlName="description"
                [nzAutosize]="{ minRows: 2, maxRows: 5 }" placeholder="Descripción opcional"></textarea>
            </nz-form-control>
          </nz-form-item>

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:0 16px">
            <nz-form-item>
              <nz-form-label nzRequired>Complejidad</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="complexity" nzPlaceHolder="Complejidad" style="width:100%">
                  @for (opt of complexityOptions; track opt.value) {
                    <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
                  }
                </nz-select>
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Estado</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="status" nzPlaceHolder="Estado" style="width:100%">
                  @for (opt of statusOptions; track opt.value) {
                    <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
                  }
                </nz-select>
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Año de cartera</nz-form-label>
              <nz-form-control>
                <nz-input-number formControlName="portfolioYear" [nzMin]="2000" [nzMax]="2100"
                  nzPlaceHolder="Ej: 2026" style="width:100%" />
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Ref. cartera anterior</nz-form-label>
              <nz-form-control>
                <nz-input-number formControlName="previousReferenceId" [nzMin]="1"
                  nzPlaceHolder="ID anterior" style="width:100%" />
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Fecha de inicio</nz-form-label>
              <nz-form-control>
                <nz-date-picker formControlName="startDate" nzPlaceHolder="Fecha inicio" style="width:100%" />
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Fecha de fin</nz-form-label>
              <nz-form-control>
                <nz-date-picker formControlName="endDate" nzPlaceHolder="Fecha fin" style="width:100%" />
              </nz-form-control>
            </nz-form-item>
          </div>

          <nz-divider nzText="Clasificación y gobernanza" nzOrientation="left"></nz-divider>

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:0 16px">
            <nz-form-item>
              <nz-form-label>Promotor</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="promoterId" nzPlaceHolder="Seleccionar promotor"
                  nzAllowClear style="width:100%">
                  @for (p of promoters(); track p.id) {
                    <nz-option [nzValue]="p.id" [nzLabel]="p.name" />
                  }
                </nz-select>
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Unidad orgánica</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="organicUnitId" nzPlaceHolder="Seleccionar unidad"
                  nzShowSearch nzAllowClear style="width:100%">
                  @for (u of organicUnits(); track u.id) {
                    <nz-option [nzValue]="u.id" [nzLabel]="u.name + (u.code ? ' (' + u.code + ')' : '')" />
                  }
                </nz-select>
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Grupo SIPT</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="siptGroup" nzPlaceHolder="Grupo SIPT" nzAllowClear style="width:100%">
                  @for (opt of siptOptions; track opt.value) {
                    <nz-option [nzValue]="opt.value" [nzLabel]="opt.label" />
                  }
                </nz-select>
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Prioridad estratégica (1-5)</nz-form-label>
              <nz-form-control>
                <nz-input-number formControlName="groupPriority" [nzMin]="1" [nzMax]="5"
                  nzPlaceHolder="1-5" style="width:100%" />
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Orden UOR</nz-form-label>
              <nz-form-control>
                <nz-input-number formControlName="uorOrder" [nzMin]="1"
                  nzPlaceHolder="Orden de prioridad" style="width:100%" />
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Nº beneficiarios</nz-form-label>
              <nz-form-control>
                <nz-input-number formControlName="beneficiaryCount" [nzMin]="0"
                  nzPlaceHolder="Usuarios beneficiados" style="width:100%" />
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>Presupuesto estimado (€)</nz-form-label>
              <nz-form-control>
                <nz-input-number formControlName="estimatedBudget" [nzMin]="0"
                  nzPlaceHolder="Presupuesto estimado" style="width:100%" />
              </nz-form-control>
            </nz-form-item>
          </div>

          <nz-form-item>
            <nz-form-label>Etiquetas</nz-form-label>
            <nz-form-control>
              <nz-select formControlName="tagIds" nzMode="multiple"
                nzPlaceHolder="Seleccionar etiquetas" nzAllowClear style="width:100%">
                @for (t of tags(); track t.id) {
                  <nz-option [nzValue]="t.id" [nzLabel]="t.name" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Valor de negocio</nz-form-label>
            <nz-form-control>
              <div style="display:flex;align-items:center;gap:12px">
                <nz-rate formControlName="businessValue" [nzCount]="5" nzAllowHalf="false"></nz-rate>
                <span style="font-size:12px;color:#8c8c8c">
                  @if (form.get('businessValue')?.value) {
                    {{ businessValueLabel(form.get('businessValue')!.value!) }}
                  } @else {
                    Sin valorar
                  }
                </span>
                @if (form.get('businessValue')?.value) {
                  <button type="button" nz-button nzSize="small" nzType="text"
                    (click)="form.get('businessValue')!.setValue(null)"
                    style="font-size:12px;color:#8c8c8c">Limpiar</button>
                }
              </div>
            </nz-form-control>
          </nz-form-item>

          <nz-divider nzText="Ciclo de vida" nzOrientation="left"></nz-divider>

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:0 16px">
            <nz-form-item>
              <nz-form-label>Fecha deseable de implantación</nz-form-label>
              <nz-form-control>
                <nz-date-picker formControlName="desiredDeploymentDate"
                  nzPlaceHolder="Fecha deseable" style="width:100%" />
              </nz-form-control>
            </nz-form-item>
          </div>

          <nz-form-item>
            <nz-form-label>URL especificaciones</nz-form-label>
            <nz-form-control>
              <input nz-input formControlName="specificationsUrl" placeholder="https://..." />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>URL épica (Jira)</nz-form-label>
            <nz-form-control>
              <input nz-input formControlName="epicUrl" placeholder="https://..." />
            </nz-form-control>
          </nz-form-item>

        </form>
      </ng-container>
    </nz-modal>
  `,
})
export class ProjectFormComponent implements OnChanges, OnInit {
  @Input() visible = false;
  @Input() project: ProjectDetail | null = null;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private readonly service = inject(ProjectsService);
  private readonly message = inject(NzMessageService);

  saving = signal(false);
  promoters = signal<PromoterDto[]>([]);
  organicUnits = signal<OrganicUnitDto[]>([]);
  tags = signal<TagDto[]>([]);

  readonly complexityOptions = (Object.keys(PROJECT_COMPLEXITY_LABELS) as ProjectComplexity[]).map(v => ({
    value: v, label: PROJECT_COMPLEXITY_LABELS[v],
  }));

  readonly statusOptions = (Object.keys(PROJECT_STATUS_LABELS) as ProjectStatus[]).map(v => ({
    value: v, label: PROJECT_STATUS_LABELS[v],
  }));

  readonly siptOptions = (Object.keys(SIPT_GROUP_LABELS) as SiptGroup[]).map(v => ({
    value: v, label: SIPT_GROUP_LABELS[v],
  }));

  form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] }),
    description: new FormControl<string | null>(null),
    complexity: new FormControl<ProjectComplexity>('Small', { nonNullable: true, validators: [Validators.required] }),
    status: new FormControl<ProjectStatus | null>(null),
    portfolioYear: new FormControl<number | null>(null),
    startDate: new FormControl<Date | null>(null),
    endDate: new FormControl<Date | null>(null),
    previousReferenceId: new FormControl<number | null>(null),
    beneficiaryCount: new FormControl<number | null>(null),
    promoterId: new FormControl<number | null>(null),
    organicUnitId: new FormControl<number | null>(null),
    uorOrder: new FormControl<number | null>(null),
    groupPriority: new FormControl<number | null>(null),
    siptGroup: new FormControl<SiptGroup | null>(null),
    desiredDeploymentDate: new FormControl<Date | null>(null),
    specificationsUrl: new FormControl<string | null>(null, Validators.maxLength(500)),
    epicUrl: new FormControl<string | null>(null, Validators.maxLength(500)),
    estimatedBudget: new FormControl<number | null>(null),
    businessValue: new FormControl<number | null>(null),
    tagIds: new FormControl<number[]>([]),
  });

  ngOnInit(): void {
    this.service.getPromoters().subscribe(r => this.promoters.set(r.items));
    this.service.getOrganicUnits().subscribe(r => this.organicUnits.set(r.items));
    this.service.getTags().subscribe(t => this.tags.set(t));
  }

  ngOnChanges(): void {
    if (this.visible && this.project) {
      const p = this.project;
      this.form.patchValue({
        title: p.title,
        description: p.description ?? null,
        complexity: p.complexity,
        status: p.status ?? null,
        portfolioYear: p.portfolioYear ?? null,
        startDate: p.startDate ? new Date(p.startDate) : null,
        endDate: p.endDate ? new Date(p.endDate) : null,
        previousReferenceId: p.previousReferenceId ?? null,
        beneficiaryCount: p.beneficiaryCount ?? null,
        promoterId: p.promoterId ?? null,
        organicUnitId: p.organicUnitId ?? null,
        uorOrder: p.uorOrder ?? null,
        groupPriority: p.groupPriority ?? null,
        siptGroup: p.siptGroup ?? null,
        desiredDeploymentDate: p.desiredDeploymentDate ? new Date(p.desiredDeploymentDate) : null,
        specificationsUrl: p.specificationsUrl ?? null,
        epicUrl: p.epicUrl ?? null,
        estimatedBudget: p.estimatedBudget ?? null,
        businessValue: p.businessValue ?? null,
        tagIds: p.tags.map(t => t.id),
      });
    } else if (this.visible && !this.project) {
      this.form.reset({ complexity: 'Small', tagIds: [] });
    }
  }

  private toDateStr(d: Date | null): string | null {
    return d ? d.toISOString().split('T')[0] : null;
  }

  submit(): void {
    if (this.form.invalid) {
      Object.values(this.form.controls).forEach(c => { c.markAsDirty(); c.updateValueAndValidity({ onlySelf: true }); });
      return;
    }

    const raw = this.form.getRawValue();
    const payload: CreateProjectDto = {
      title: raw.title,
      description: raw.description || null,
      complexity: raw.complexity,
      portfolioYear: raw.portfolioYear || null,
      startDate: this.toDateStr(raw.startDate),
      endDate: this.toDateStr(raw.endDate),
      previousReferenceId: raw.previousReferenceId || null,
      beneficiaryCount: raw.beneficiaryCount || null,
      promoterId: raw.promoterId || null,
      organicUnitId: raw.organicUnitId || null,
      uorOrder: raw.uorOrder || null,
      groupPriority: raw.groupPriority || null,
      siptGroup: raw.siptGroup || null,
      desiredDeploymentDate: this.toDateStr(raw.desiredDeploymentDate),
      specificationsUrl: raw.specificationsUrl || null,
      epicUrl: raw.epicUrl || null,
      estimatedBudget: raw.estimatedBudget ?? null,
      businessValue: raw.businessValue ?? null,
      tagIds: raw.tagIds ?? [],
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
    this.form.reset({ complexity: 'Small', tagIds: [] });
    this.cancelled.emit();
  }

  businessValueLabel(value: number): string {
    const labels: Record<number, string> = {
      1: 'Marginal',
      2: 'Bajo',
      3: 'Moderado',
      4: 'Alto',
      5: 'Crítico',
    };
    return labels[value] ?? '';
  }
}
