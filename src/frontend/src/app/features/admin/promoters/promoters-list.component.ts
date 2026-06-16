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
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { CatalogsService } from '../catalogs.service';
import { PromoterDto } from '../../projects/project.model';

@Component({
  selector: 'app-promoters-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NzTableModule, NzButtonModule, NzModalModule, NzFormModule,
    NzInputModule, NzPopconfirmModule, NzIconModule, NzSpinModule,
  ],
  template: `
    <div style="padding:24px">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
        <h2 style="margin:0">Promotores</h2>
        <button nz-button nzType="primary" (click)="openCreate()">
          <nz-icon nzType="plus" /> Nuevo promotor
        </button>
      </div>

      <nz-table [nzData]="promoters()" [nzLoading]="loading()" [nzFrontPagination]="false"
        [nzTotal]="total()" [nzPageIndex]="page()" [nzPageSize]="pageSize()"
        (nzPageIndexChange)="loadPage($event)" nzBordered>
        <thead><tr><th>Nombre</th><th>Acciones</th></tr></thead>
        <tbody>
          @for (p of promoters(); track p.id) {
            <tr>
              <td>{{ p.name }}</td>
              <td>
                <button nz-button nzType="link" (click)="openEdit(p)">Editar</button>
                <button nz-button nzType="link" nzDanger
                  nz-popconfirm nzPopconfirmTitle="¿Eliminar este promotor?"
                  (nzOnConfirm)="delete(p.id)">Eliminar</button>
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    </div>

    <nz-modal [nzVisible]="modalVisible()" [nzTitle]="editing() ? 'Editar promotor' : 'Nuevo promotor'"
      [nzFooter]="footer" (nzOnCancel)="closeModal()">
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="form" nzLayout="vertical">
          <nz-form-item>
            <nz-form-label nzRequired>Nombre</nz-form-label>
            <nz-form-control nzErrorTip="El nombre es obligatorio">
              <input nz-input formControlName="name" placeholder="Nombre del promotor" />
            </nz-form-control>
          </nz-form-item>
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
export class PromotersListComponent {
  private readonly svc = inject(CatalogsService);
  private readonly msg = inject(NzMessageService);
  private readonly fb = inject(FormBuilder);

  promoters = signal<PromoterDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  modalVisible = signal(false);
  editing = signal<PromoterDto | null>(null);
  total = signal(0);
  page = signal(1);
  readonly pageSize = signal(20);

  form = this.fb.group({ name: ['', [Validators.required, Validators.maxLength(200)]] });

  constructor() { this.load(); }

  load(page = 1): void {
    this.loading.set(true);
    this.svc.getPromoters(page, this.pageSize()).subscribe({
      next: r => { this.promoters.set(r.items); this.total.set(r.total); this.page.set(page); this.loading.set(false); },
      error: () => { this.loading.set(false); this.msg.error('Error al cargar promotores'); },
    });
  }

  loadPage(p: number): void { this.load(p); }

  openCreate(): void { this.editing.set(null); this.form.reset(); this.modalVisible.set(true); }

  openEdit(p: PromoterDto): void { this.editing.set(p); this.form.patchValue({ name: p.name }); this.modalVisible.set(true); }

  closeModal(): void { this.modalVisible.set(false); this.editing.set(null); this.form.reset(); }

  save(): void {
    if (this.form.invalid) return;
    const name = this.form.value.name!;
    this.saving.set(true);
    const e = this.editing();
    const obs = (e ? this.svc.updatePromoter(e.id, name) : this.svc.createPromoter(name)) as Observable<unknown>;
    obs.subscribe({
      next: () => { this.saving.set(false); this.msg.success(e ? 'Actualizado' : 'Creado'); this.closeModal(); this.load(this.page()); },
      error: () => { this.saving.set(false); this.msg.error('Error al guardar'); },
    });
  }

  delete(id: number): void {
    this.svc.deletePromoter(id).subscribe({
      next: () => { this.msg.success('Eliminado'); this.load(this.page()); },
      error: () => this.msg.error('No se puede eliminar (tiene proyectos asignados)'),
    });
  }
}
