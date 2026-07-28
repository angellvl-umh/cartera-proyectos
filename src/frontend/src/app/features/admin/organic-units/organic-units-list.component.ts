import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { Observable, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
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
import { OrganicUnitDto } from '../../projects/project.model';

@Component({
  selector: 'app-organic-units-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, FormsModule,
    NzTableModule, NzButtonModule, NzModalModule, NzFormModule,
    NzInputModule, NzPopconfirmModule, NzIconModule, NzSpinModule,
  ],
  template: `
    <div style="padding:24px">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
        <h2 style="margin:0">Unidades Orgánicas</h2>
        <button nz-button nzType="primary" (click)="openCreate()">
          <nz-icon nzType="plus" /> Nueva unidad orgánica
        </button>
      </div>

      <div style="margin-bottom:16px">
        <nz-input-group [nzPrefix]="searchIcon" style="max-width:300px">
          <input nz-input placeholder="Buscar por nombre o código..." 
            [ngModel]="searchText()" (ngModelChange)="onSearchChange($event)" />
        </nz-input-group>
        <ng-template #searchIcon><span nz-icon nzType="search"></span></ng-template>
      </div>

      <nz-table [nzData]="units()" [nzLoading]="loading()" [nzFrontPagination]="false"
        [nzTotal]="total()" [nzPageIndex]="page()" [nzPageSize]="pageSize()"
        (nzPageIndexChange)="loadPage($event)" nzBordered>
        <thead><tr><th>Nombre</th><th>Código</th><th>Acciones</th></tr></thead>
        <tbody>
          @for (u of units(); track u.id) {
            <tr>
              <td>{{ u.name }}</td>
              <td>{{ u.code ?? '—' }}</td>
              <td>
                <button nz-button nzType="link" (click)="openEdit(u)">Editar</button>
                <button nz-button nzType="link" nzDanger
                  nz-popconfirm nzPopconfirmTitle="¿Eliminar esta unidad orgánica?"
                  (nzOnConfirm)="delete(u.id)">Eliminar</button>
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    </div>

    <nz-modal [nzVisible]="modalVisible()" [nzTitle]="editing() ? 'Editar unidad orgánica' : 'Nueva unidad orgánica'"
      [nzFooter]="footer" (nzOnCancel)="closeModal()">
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="form" nzLayout="vertical">
          <nz-form-item>
            <nz-form-label nzRequired>Nombre</nz-form-label>
            <nz-form-control nzErrorTip="El nombre es obligatorio">
              <input nz-input formControlName="name" placeholder="Nombre de la unidad" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label>Código</nz-form-label>
            <nz-form-control>
              <input nz-input formControlName="code" placeholder="Ej: TIC" />
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
export class OrganicUnitsListComponent {
  private readonly svc = inject(CatalogsService);
  private readonly msg = inject(NzMessageService);
  private readonly fb = inject(FormBuilder);

  units = signal<OrganicUnitDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  modalVisible = signal(false);
  editing = signal<OrganicUnitDto | null>(null);
  total = signal(0);
  page = signal(1);
  readonly pageSize = signal(20);
  searchText = signal('');

  private readonly searchSubject = new Subject<string>();

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    code: ['', Validators.maxLength(50)],
  });

  constructor() {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(q => {
      this.page.set(1);
      this.load(1, q);
    });
    this.load();
  }

  load(page = 1, q?: string): void {
    this.loading.set(true);
    this.svc.getOrganicUnits(q, page, this.pageSize()).subscribe({
      next: r => { this.units.set(r.items); this.total.set(r.total); this.page.set(page); this.loading.set(false); },
      error: () => { this.loading.set(false); this.msg.error('Error al cargar unidades orgánicas'); },
    });
  }

  onSearchChange(text: string): void {
    this.searchText.set(text);
    this.searchSubject.next(text);
  }

  loadPage(p: number): void { this.load(p, this.searchText()); }

  openCreate(): void { this.editing.set(null); this.form.reset(); this.modalVisible.set(true); }

  openEdit(u: OrganicUnitDto): void { this.editing.set(u); this.form.patchValue({ name: u.name, code: u.code ?? '' }); this.modalVisible.set(true); }

  closeModal(): void { this.modalVisible.set(false); this.editing.set(null); this.form.reset(); }

  save(): void {
    if (this.form.invalid) return;
    const { name, code } = this.form.value;
    this.saving.set(true);
    const e = this.editing();
    const obs = (e
      ? this.svc.updateOrganicUnit(e.id, name!, code || null)
      : this.svc.createOrganicUnit(name!, code || null)) as Observable<unknown>;
    obs.subscribe({
      next: () => { this.saving.set(false); this.msg.success(e ? 'Actualizada' : 'Creada'); this.closeModal(); this.load(this.page(), this.searchText()); },
      error: () => { this.saving.set(false); this.msg.error('Error al guardar'); },
    });
  }

  delete(id: number): void {
    this.svc.deleteOrganicUnit(id).subscribe({
      next: () => { this.msg.success('Eliminada'); this.load(this.page(), this.searchText()); },
      error: () => this.msg.error('No se puede eliminar (tiene proyectos asignados)'),
    });
  }
}
