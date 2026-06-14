import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzMessageService } from 'ng-zorro-antd/message';
import { FormsModule } from '@angular/forms';
import { Person, PersonsService } from '../persons.service';
import { RouterLink } from '@angular/router';
import { switchMap, Subject, startWith } from 'rxjs';

@Component({
  selector: 'app-persons-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzTableModule, NzTagModule, NzSelectModule, FormsModule, RouterLink],
  template: `
    <nz-table
      nzTitle="Personas"
      [nzData]="persons()?.items ?? []"
      [nzTotal]="persons()?.total ?? 0"
      [nzPageIndex]="page()"
      [nzPageSize]="20"
      [nzFrontPagination]="false"
      [nzLoading]="!persons()"
      (nzPageIndexChange)="page.set($event)"
    >
      <thead>
        <tr>
          <th>Nombre</th>
          <th>Email</th>
          <th>Rol</th>
        </tr>
      </thead>
      <tbody>
        @for (p of persons()?.items ?? []; track p.id) {
          <tr>
            <td><a [routerLink]="['/persons', p.id]" style="font-weight:600">{{ p.name }}</a></td>
            <td>{{ p.email }}</td>
            <td>
              <nz-select [ngModel]="p.role" (ngModelChange)="changeRole(p, $event)" nzSize="small">
                <nz-option nzValue="Desarrollador" nzLabel="Desarrollador"></nz-option>
                <nz-option nzValue="JefeEquipo" nzLabel="Jefe de Equipo"></nz-option>
                <nz-option nzValue="Gestor" nzLabel="Gestor"></nz-option>
              </nz-select>
            </td>
          </tr>
        }
      </tbody>
    </nz-table>
  `,
})
export class PersonsListComponent {
  private readonly svc = inject(PersonsService);
  private readonly msg = inject(NzMessageService);

  page = signal(1);
  private refresh$ = new Subject<void>();

  persons = toSignal(
    this.refresh$.pipe(
      startWith(null),
      switchMap(() => this.svc.getPersons(this.page()))
    )
  );

  changeRole(person: Person, role: string): void {
    this.svc.updateRole(person.id, role).subscribe({
      next: () => {
        this.msg.success(`Rol de ${person.name} actualizado a ${role}`);
        this.refresh$.next();
      },
      error: () => this.msg.error('Error al actualizar el rol'),
    });
  }
}
