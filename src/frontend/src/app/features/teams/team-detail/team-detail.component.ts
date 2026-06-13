import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { FormsModule } from '@angular/forms';
import { TeamsService } from '../teams.service';
import { PersonsService } from '../persons.service';
import { Person, PersonRole, TeamDetail } from '../team.model';

@Component({
  selector: 'app-team-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzTableModule,
    NzButtonModule,
    NzPopconfirmModule,
    NzSpinModule,
    NzSelectModule,
    NzIconModule,
    NzCardModule,
    NzTagModule,
    NzDividerModule,
  ],
  template: `
    <div style="padding: 24px">
      <div style="margin-bottom: 16px">
        <button nz-button nzType="default" (click)="goBack()">
          <nz-icon nzType="arrow-left" />
          Volver a equipos
        </button>
      </div>

      @if (loading()) {
        <div style="text-align: center; padding: 40px">
          <nz-spin nzSize="large" />
        </div>
      } @else if (team()) {
        <nz-card [nzTitle]="team()!.name" style="margin-bottom: 24px">
          @if (team()!.description) {
            <p>{{ team()!.description }}</p>
          }
          @if (team()!.leadName) {
            <p><strong>Líder:</strong> {{ team()!.leadName }}</p>
          }
        </nz-card>

        <nz-card nzTitle="Miembros del equipo">
          <div style="display: flex; gap: 8px; margin-bottom: 16px; align-items: center">
            <nz-select
              [(ngModel)]="selectedPersonId"
              nzPlaceHolder="Seleccionar persona para añadir"
              nzAllowClear
              style="width: 300px"
            >
              @for (person of availablePersons(); track person.id) {
                <nz-option [nzValue]="person.id" [nzLabel]="person.name + ' (' + person.email + ')'" />
              }
            </nz-select>
            <button
              nz-button
              nzType="primary"
              [disabled]="!selectedPersonId"
              (click)="addMember()"
            >
              <nz-icon nzType="user-add" />
              Añadir miembro
            </button>
          </div>

          <nz-table
            #membersTable
            [nzData]="team()!.members"
            nzBordered
            [nzShowPagination]="false"
          >
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Email</th>
                <th>Rol</th>
                <th>Fecha de ingreso</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (member of membersTable.data; track member.id) {
                <tr>
                  <td>{{ member.name }}</td>
                  <td>{{ member.email }}</td>
                  <td>
                    <nz-tag [nzColor]="roleColor(member.role)">{{ member.role }}</nz-tag>
                  </td>
                  <td>{{ formatDate(member.joinedAt) }}</td>
                  <td>
                    <button
                      nz-button
                      nzType="link"
                      nzDanger
                      nz-popconfirm
                      nzPopconfirmTitle="¿Eliminar este miembro del equipo?"
                      nzOkText="Eliminar"
                      nzCancelText="Cancelar"
                      (nzOnConfirm)="removeMember(member.id)"
                    >
                      Eliminar
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </nz-table>
        </nz-card>
      }
    </div>
  `,
})
export class TeamDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly teamsService = inject(TeamsService);
  private readonly personsService = inject(PersonsService);
  private readonly message = inject(NzMessageService);

  readonly team = signal<TeamDetail | null>(null);
  readonly allPersons = signal<Person[]>([]);
  readonly loading = signal(false);
  selectedPersonId: string | null = null;

  readonly availablePersons = computed(() => {
    const memberIds = new Set(this.team()?.members.map(m => m.id) ?? []);
    return this.allPersons().filter(p => !memberIds.has(p.id));
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadTeam(id);
    this.loadPersons();
  }

  private loadTeam(id: string): void {
    this.loading.set(true);
    this.teamsService.getTeam(id).subscribe({
      next: (data) => {
        this.team.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.message.error('Error al cargar el equipo');
      },
    });
  }

  private loadPersons(): void {
    // Cargamos con pageSize alto para tener todos los disponibles en el selector
    this.personsService.getPersons(1, 500).subscribe({
      next: (result) => this.allPersons.set(result.items),
      error: () => this.message.error('Error al cargar las personas'),
    });
  }

  addMember(): void {
    if (!this.selectedPersonId || !this.team()) return;

    this.teamsService.assignMember(this.team()!.id, this.selectedPersonId).subscribe({
      next: () => {
        this.message.success('Miembro añadido correctamente');
        this.selectedPersonId = null;
        this.loadTeam(this.team()!.id);
      },
      error: () => {
        this.message.error('Error al añadir el miembro');
      },
    });
  }

  removeMember(personId: string): void {
    if (!this.team()) return;

    this.teamsService.removeMember(this.team()!.id, personId).subscribe({
      next: () => {
        this.message.success('Miembro eliminado correctamente');
        this.loadTeam(this.team()!.id);
      },
      error: () => {
        this.message.error('Error al eliminar el miembro');
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/teams']);
  }

  roleColor(role: PersonRole): string {
    const colors: Record<PersonRole, string> = {
      Gestor: 'purple',
      JefeEquipo: 'blue',
      Desarrollador: 'green',
    };
    return colors[role] ?? 'default';
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('es-ES', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}
