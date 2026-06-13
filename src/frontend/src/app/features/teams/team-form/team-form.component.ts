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
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzMessageService } from 'ng-zorro-antd/message';
import { TeamsService } from '../teams.service';
import { Person, Team } from '../team.model';

@Component({
  selector: 'app-team-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NzModalModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzButtonModule,
  ],
  template: `
    <nz-modal
      [nzVisible]="visible"
      [nzTitle]="team ? 'Editar equipo' : 'Nuevo equipo'"
      [nzFooter]="modalFooter"
      (nzOnCancel)="onCancel()"
    >
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="form" nzLayout="vertical">
          <nz-form-item>
            <nz-form-label nzRequired>Nombre</nz-form-label>
            <nz-form-control nzErrorTip="El nombre es obligatorio">
              <input nz-input formControlName="name" placeholder="Nombre del equipo" />
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Descripción</nz-form-label>
            <nz-form-control>
              <textarea
                nz-input
                formControlName="description"
                placeholder="Descripción del equipo (opcional)"
                [nzAutosize]="{ minRows: 2, maxRows: 4 }"
              ></textarea>
            </nz-form-control>
          </nz-form-item>

          <nz-form-item>
            <nz-form-label>Líder del equipo</nz-form-label>
            <nz-form-control>
              <nz-select
                formControlName="leadPersonId"
                nzPlaceHolder="Seleccionar líder (opcional)"
                nzAllowClear
                style="width: 100%"
              >
                @for (person of leads; track person.id) {
                  <nz-option [nzValue]="person.id" [nzLabel]="person.name + ' (' + person.role + ')'" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>
        </form>
      </ng-container>

      <ng-template #modalFooter>
        <button nz-button nzType="default" (click)="onCancel()">Cancelar</button>
        <button
          nz-button
          nzType="primary"
          [nzLoading]="saving()"
          [disabled]="form.invalid"
          (click)="onSave()"
        >
          {{ team ? 'Guardar cambios' : 'Crear equipo' }}
        </button>
      </ng-template>
    </nz-modal>
  `,
})
export class TeamFormComponent implements OnChanges {
  @Input() visible = false;
  @Input() team: Team | null = null;
  @Input() leads: Person[] = [];
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly teamsService = inject(TeamsService);
  private readonly message = inject(NzMessageService);

  readonly saving = signal(false);

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    leadPersonId: [null as string | null],
  });

  ngOnChanges(): void {
    if (this.visible) {
      if (this.team) {
        this.form.patchValue({
          name: this.team.name,
          description: this.team.description ?? '',
          leadPersonId: this.team.leadPersonId,
        });
      } else {
        this.form.reset({ name: '', description: '', leadPersonId: null });
      }
    }
  }

  onCancel(): void {
    this.form.reset();
    this.cancelled.emit();
  }

  onSave(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const payload = {
      name: value.name!,
      description: value.description || null,
      leadPersonId: value.leadPersonId || null,
    };

    this.saving.set(true);

    if (this.team) {
      this.teamsService.updateTeam(this.team.id, payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.message.success('Equipo actualizado correctamente');
          this.form.reset();
          this.saved.emit();
        },
        error: () => {
          this.saving.set(false);
          this.message.error('Error al actualizar el equipo');
        },
      });
    } else {
      this.teamsService.createTeam(payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.message.success('Equipo creado correctamente');
          this.form.reset();
          this.saved.emit();
        },
        error: () => {
          this.saving.set(false);
          this.message.error('Error al crear el equipo');
        },
      });
    }
  }
}
