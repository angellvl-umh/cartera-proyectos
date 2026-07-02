import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable, Subject, startWith, switchMap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { Person, PersonsService, PersonUpsertDto } from '../persons.service';

const ROLE_COLORS: Record<string, string> = {
  Gestor: 'purple',
  JefeEquipo: 'geekblue',
  Desarrollador: 'blue',
};

const ROLE_LABELS: Record<string, string> = {
  Gestor: 'Gestor',
  JefeEquipo: 'Jefe de equipo',
  Desarrollador: 'Desarrollador',
};

@Component({
  selector: 'app-persons-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, ReactiveFormsModule, RouterLink,
    NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzModalModule, NzFormModule, NzInputModule, NzSelectModule,
    NzSwitchModule, NzPopconfirmModule, NzSpaceModule,
    NzTooltipModule, NzSpinModule,
  ],
  template: `
    <!-- Encabezado -->
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <div>
        <h2 style="margin:0">Personas</h2>
        <p style="margin:4px 0 0;font-size:13px;color:#8c8c8c">
          {{ persons()?.total ?? 0 }} persona{{ (persons()?.total ?? 0) !== 1 ? 's' : '' }}
        </p>
      </div>
      <nz-space>
        <label *nzSpaceItem style="font-size:13px;display:inline-flex;align-items:center;gap:8px;cursor:pointer">
          <nz-switch
            [(ngModel)]="showInactive"
            (ngModelChange)="onShowInactiveChange()"
            nzSize="small"
          ></nz-switch>
          <span>Mostrar inactivas</span>
        </label>
        @if (isGestor()) {
          <button *nzSpaceItem nz-button nzType="primary" (click)="openCreateModal()">
            <span nz-icon nzType="plus"></span> Nueva persona
          </button>
        }
      </nz-space>
    </div>

    <!-- Tabla -->
    <nz-table
      [nzData]="persons()?.items ?? []"
      [nzTotal]="persons()?.total ?? 0"
      [nzPageIndex]="page()"
      [nzPageSize]="20"
      [nzFrontPagination]="false"
      [nzLoading]="!persons()"
      (nzPageIndexChange)="onPageChange($event)"
      nzBordered
      nzSize="middle"
    >
      <thead>
        <tr>
          <th>Nombre</th>
          <th>Email</th>
          <th nzWidth="160px">Rol</th>
          <th nzWidth="200px">Estado</th>
          @if (isGestor()) {
            <th nzWidth="140px">Acciones</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (p of persons()?.items ?? []; track p.id) {
          <tr>
            <td>
              <a [routerLink]="['/persons', p.id]" style="font-weight:600">{{ p.name }}</a>
            </td>
            <td style="font-size:13px">{{ p.email }}</td>
            <td>
              <nz-tag [nzColor]="roleColor(p.role)">{{ roleLabel(p.role) }}</nz-tag>
            </td>
            <td>
              <nz-space nzSize="small">
                @if (p.isActive) {
                  <nz-tag *nzSpaceItem nzColor="success">Activa</nz-tag>
                } @else {
                  <nz-tag *nzSpaceItem nzColor="default">Inactiva</nz-tag>
                }
                @if (!p.hasLoggedIn) {
                  <nz-tag *nzSpaceItem nzColor="orange"
                    nz-tooltip nzTooltipTitle="Pre-registrada: se vinculará con su cuenta al primer inicio de sesión">
                    Sin acceso aún
                  </nz-tag>
                }
              </nz-space>
            </td>
            @if (isGestor()) {
              <td>
                <nz-space nzSize="small">
                  <button *nzSpaceItem nz-button nzSize="small" (click)="openEditModal(p)"
                    title="Editar">
                    <span nz-icon nzType="edit"></span>
                  </button>
                  @if (p.isActive) {
                    <button *nzSpaceItem nz-button nzSize="small" nzDanger
                      nz-popconfirm
                      [nzPopconfirmTitle]="'¿Desactivar a ' + p.name + '? No aparecerá en listados ni podrá recibir tareas.'"
                      (nzOnConfirm)="toggleActive(p)">
                      <span nz-icon nzType="stop"></span>
                    </button>
                  } @else {
                    <button *nzSpaceItem nz-button nzSize="small"
                      nz-popconfirm
                      [nzPopconfirmTitle]="'¿Reactivar a ' + p.name + '?'"
                      (nzOnConfirm)="toggleActive(p)">
                      <span nz-icon nzType="check-circle"></span>
                    </button>
                  }
                </nz-space>
              </td>
            }
          </tr>
        } @empty {
          @if (persons()) {
            <tr>
              <td [attr.colspan]="isGestor() ? 5 : 4" style="text-align:center;padding:32px;color:#999">
                Sin personas registradas.
              </td>
            </tr>
          }
        }
      </tbody>
    </nz-table>

    <!-- Modal crear / editar -->
    <nz-modal
      [nzVisible]="modalVisible()"
      [nzTitle]="editingPerson() ? 'Editar persona' : 'Nueva persona'"
      [nzOkText]="saving() ? 'Guardando...' : 'Guardar'"
      [nzOkLoading]="saving()"
      (nzOnOk)="submitModal()"
      (nzOnCancel)="closeModal()"
    >
      <ng-container *nzModalContent>
        <form nz-form [formGroup]="personForm" nzLayout="vertical">
          <nz-form-item>
            <nz-form-label nzRequired>Nombre</nz-form-label>
            <nz-form-control nzErrorTip="El nombre es obligatorio">
              <input nz-input formControlName="name" placeholder="Nombre completo" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Email</nz-form-label>
            <nz-form-control nzErrorTip="Introduce un email válido">
              <input nz-input type="email" formControlName="email" placeholder="usuario@dominio.es" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Rol</nz-form-label>
            <nz-form-control nzErrorTip="Selecciona un rol">
              <nz-select formControlName="role" nzPlaceHolder="Seleccionar rol" style="width:100%">
                <nz-option nzValue="Desarrollador" nzLabel="Desarrollador" />
                <nz-option nzValue="Gestor" nzLabel="Gestor" />
              </nz-select>
            </nz-form-control>
          </nz-form-item>
        </form>
      </ng-container>
    </nz-modal>
  `,
})
export class PersonsListComponent {
  private readonly svc = inject(PersonsService);
  private readonly msg = inject(NzMessageService);
  private readonly http = inject(HttpClient);

  // ── Current user ─────────────────────────────────────────────────────────
  private readonly currentUser = toSignal(
    this.http.get<{ id: number; role: string }>('/api/me')
  );

  readonly isGestor = computed(() => this.currentUser()?.role === 'Gestor');

  // ── Pagination / filter state ────────────────────────────────────────────
  page = signal(1);
  showInactive = false;
  private readonly refresh$ = new Subject<void>();

  persons = toSignal(
    this.refresh$.pipe(
      startWith(null),
      switchMap(() => this.svc.getPersons(this.page(), 20, this.showInactive))
    )
  );

  // ── Modal state ──────────────────────────────────────────────────────────
  modalVisible = signal(false);
  editingPerson = signal<Person | null>(null);
  saving = signal(false);

  personForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    role: new FormControl<'Desarrollador' | 'Gestor' | null>(null, [Validators.required]),
  });

  // ── Pagination ───────────────────────────────────────────────────────────
  onPageChange(p: number): void {
    this.page.set(p);
    this.refresh$.next();
  }

  onShowInactiveChange(): void {
    this.page.set(1);
    this.refresh$.next();
  }

  // ── Modal helpers ─────────────────────────────────────────────────────────
  openCreateModal(): void {
    this.editingPerson.set(null);
    this.personForm.reset({ name: '', email: '', role: null });
    this.modalVisible.set(true);
  }

  openEditModal(p: Person): void {
    this.editingPerson.set(p);
    // If historical JefeEquipo, keep blank so user picks a valid role
    const editableRole = (p.role === 'Desarrollador' || p.role === 'Gestor') ? p.role : null;
    this.personForm.reset({ name: p.name, email: p.email, role: editableRole });
    this.modalVisible.set(true);
  }

  closeModal(): void {
    this.modalVisible.set(false);
    this.editingPerson.set(null);
    this.personForm.reset();
  }

  submitModal(): void {
    if (this.personForm.invalid) {
      Object.values(this.personForm.controls).forEach(c => {
        c.markAsDirty();
        c.updateValueAndValidity({ onlySelf: true });
      });
      return;
    }

    const raw = this.personForm.getRawValue();
    const dto: PersonUpsertDto = {
      name: raw.name,
      email: raw.email,
      role: raw.role!,
    };

    this.saving.set(true);
    const editing = this.editingPerson();

    const op$: Observable<unknown> = editing
      ? this.svc.updatePerson(editing.id, dto)
      : this.svc.createPerson(dto);

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.msg.success(editing ? 'Persona actualizada' : 'Persona creada');
        this.closeModal();
        this.refresh$.next();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        const msg = (err as { error?: { message?: string } })?.error?.message;
        this.msg.error(msg ?? 'Error al guardar la persona');
      },
    });
  }

  // ── Activate / deactivate ─────────────────────────────────────────────────
  toggleActive(p: Person): void {
    this.svc.setActive(p.id, !p.isActive).subscribe({
      next: () => {
        this.msg.success(p.isActive ? `${p.name} desactivada` : `${p.name} reactivada`);
        this.refresh$.next();
      },
      error: (err: unknown) => {
        const msg = (err as { error?: { message?: string } })?.error?.message;
        this.msg.error(msg ?? 'Error al cambiar el estado');
      },
    });
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  roleColor(role: string): string { return ROLE_COLORS[role] ?? 'default'; }
  roleLabel(role: string): string { return ROLE_LABELS[role] ?? role; }
}
