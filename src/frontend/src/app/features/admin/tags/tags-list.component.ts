import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { CatalogsService } from '../catalogs.service';
import { TagDto } from '../../projects/project.model';

@Component({
  selector: 'app-tags-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NzTableModule, NzButtonModule, NzModalModule, NzFormModule,
    NzInputModule, NzPopconfirmModule, NzIconModule, NzTagModule,
  ],
  template: `
    <div style="padding:24px">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
        <h2 style="margin:0">Etiquetas</h2>
        <button nz-button nzType="primary" (click)="openCreate()">
          <nz-icon nzType="plus" /> Nueva etiqueta
        </button>
      </div>

      <nz-table [nzData]="tags()" [nzLoading]="loading()" nzBordered>
        <thead><tr><th>Vista previa</th><th>Nombre</th><th>Color</th><th>Acciones</th></tr></thead>
        <tbody>
          @for (t of tags(); track t.id) {
            <tr>
              <td><nz-tag [nzColor]="t.color ?? 'default'">{{ t.name }}</nz-tag></td>
              <td>{{ t.name }}</td>
              <td>{{ t.color ?? '—' }}</td>
              <td>
                <button nz-button nzType="link" (click)="openEdit(t)">Editar</button>
                <button nz-button nzType="link" nzDanger
                  nz-popconfirm nzPopconfirmTitle="¿Eliminar esta etiqueta? Se desasociará de todos los proyectos."
                  (nzOnConfirm)="delete(t.id)">Eliminar</button>
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    </div>

    <nz-modal [nzVisible]="modalVisible()" [nzTitle]="editing() ? 'Editar etiqueta' : 'Nueva etiqueta'"
      [nzFooter]="footer" (nzOnCancel)="closeModal()">
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="form" nzLayout="vertical">
          <nz-form-item>
            <nz-form-label nzRequired>Nombre</nz-form-label>
            <nz-form-control nzErrorTip="El nombre es obligatorio">
              <input nz-input formControlName="name" placeholder="Ej: Urgente" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label>Color (hex)</nz-form-label>
            <nz-form-control nzExtra="Ej: #FF5733 o nombre como 'blue'">
              <input nz-input formControlName="color" placeholder="#RRGGBB" />
            </nz-form-control>
          </nz-form-item>
          @if (form.value.name) {
            <p>Vista previa: <nz-tag [nzColor]="form.value.color || 'default'">{{ form.value.name }}</nz-tag></p>
          }
        </form>
      </ng-container>
      <ng-template #footer>
        <button nz-button (click)="closeModal()">Cancelar</button>
        <button nz-button nzType="primary" [nzLoading]="saving()" [disabled]="form.invalid" (click)="save()">
          {{ editing() ? 'Guardar' : 'Crear' }}
        </button>
      </ng-template>
    </nz-modal>
  `,
})
export class TagsListComponent {
  private readonly svc = inject(CatalogsService);
  private readonly msg = inject(NzMessageService);
  private readonly fb = inject(FormBuilder);

  tags = signal<TagDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  modalVisible = signal(false);
  editing = signal<TagDto | null>(null);

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    color: ['', Validators.maxLength(20)],
  });

  constructor() { this.load(); }

  load(): void {
    this.loading.set(true);
    this.svc.getTags().subscribe({
      next: t => { this.tags.set(t); this.loading.set(false); },
      error: () => { this.loading.set(false); this.msg.error('Error al cargar etiquetas'); },
    });
  }

  openCreate(): void { this.editing.set(null); this.form.reset(); this.modalVisible.set(true); }

  openEdit(t: TagDto): void { this.editing.set(t); this.form.patchValue({ name: t.name, color: t.color ?? '' }); this.modalVisible.set(true); }

  closeModal(): void { this.modalVisible.set(false); this.editing.set(null); this.form.reset(); }

  save(): void {
    if (this.form.invalid) return;
    const { name, color } = this.form.value;
    this.saving.set(true);
    const e = this.editing();
    const obs = (e
      ? this.svc.updateTag(e.id, name!, color || null)
      : this.svc.createTag(name!, color || null)) as Observable<unknown>;
    obs.subscribe({
      next: () => { this.saving.set(false); this.msg.success(e ? 'Actualizada' : 'Creada'); this.closeModal(); this.load(); },
      error: () => { this.saving.set(false); this.msg.error('Error al guardar'); },
    });
  }

  delete(id: number): void {
    this.svc.deleteTag(id).subscribe({
      next: () => { this.msg.success('Eliminada'); this.load(); },
      error: () => this.msg.error('Error al eliminar'),
    });
  }
}
