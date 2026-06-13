import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { TeamsService } from '../teams.service';
import { PersonsService } from '../persons.service';
import { Team, Person } from '../team.model';
import { TeamFormComponent } from '../team-form/team-form.component';

@Component({
  selector: 'app-teams-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NzTableModule,
    NzButtonModule,
    NzPopconfirmModule,
    NzSpinModule,
    NzIconModule,
    TeamFormComponent,
  ],
  template: `
    <div style="padding: 24px">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px">
        <h2 style="margin: 0">Equipos</h2>
        <button nz-button nzType="primary" (click)="openCreate()">
          <nz-icon nzType="plus" />
          Nuevo equipo
        </button>
      </div>

      <nz-table
        [nzData]="teams()"
        [nzLoading]="loading()"
        [nzTotal]="total()"
        [nzPageIndex]="currentPage()"
        [nzPageSize]="pageSize()"
        [nzFrontPagination]="false"
        (nzPageIndexChange)="onPageChange($event)"
        (nzPageSizeChange)="onPageSizeChange($event)"
        nzBordered
      >
        <thead>
          <tr>
            <th>Nombre</th>
            <th>Descripción</th>
            <th>Líder</th>
            <th>N.º miembros</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          @for (team of teams(); track team.id) {
            <tr>
              <td>{{ team.name }}</td>
              <td>{{ team.description ?? '—' }}</td>
              <td>{{ team.leadName ?? '—' }}</td>
              <td>{{ team.memberCount }}</td>
              <td>
                <button nz-button nzType="link" (click)="viewDetail(team)">
                  Ver miembros
                </button>
                <button nz-button nzType="link" (click)="openEdit(team)">
                  Editar
                </button>
                <button
                  nz-button
                  nzType="link"
                  nzDanger
                  nz-popconfirm
                  nzPopconfirmTitle="¿Eliminar este equipo?"
                  nzOkText="Eliminar"
                  nzCancelText="Cancelar"
                  (nzOnConfirm)="deleteTeam(team)"
                >
                  Eliminar
                </button>
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    </div>

    <app-team-form
      [visible]="formVisible()"
      [team]="selectedTeam()"
      [leads]="leads()"
      (saved)="onSaved()"
      (cancelled)="closeForm()"
    />
  `,
})
export class TeamsListComponent {
  private readonly teamsService = inject(TeamsService);
  private readonly personsService = inject(PersonsService);
  private readonly message = inject(NzMessageService);
  private readonly router = inject(Router);

  teams = signal<Team[]>([]);
  leads = signal<Person[]>([]);
  loading = signal(false);
  formVisible = signal(false);
  selectedTeam = signal<Team | null>(null);
  currentPage = signal(1);
  pageSize = signal(20);
  total = signal(0);

  constructor() {
    this.loadTeams();
    this.loadLeads();
  }

  private loadTeams(): void {
    this.loading.set(true);
    this.teamsService.getTeams(this.currentPage(), this.pageSize()).subscribe({
      next: (result) => {
        this.teams.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.message.error('Error al cargar los equipos');
      },
    });
  }

  private loadLeads(): void {
    this.personsService.getPersons().subscribe({
      next: (result) => {
        this.leads.set(result.items.filter(p => p.role === 'JefeEquipo' || p.role === 'Gestor'));
      },
      error: () => {
        this.message.error('Error al cargar las personas');
      },
    });
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
    this.loadTeams();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadTeams();
  }

  openCreate(): void {
    this.selectedTeam.set(null);
    this.formVisible.set(true);
  }

  openEdit(team: Team): void {
    this.selectedTeam.set(team);
    this.formVisible.set(true);
  }

  closeForm(): void {
    this.formVisible.set(false);
    this.selectedTeam.set(null);
  }

  onSaved(): void {
    this.closeForm();
    this.loadTeams();
  }

  viewDetail(team: Team): void {
    this.router.navigate(['/teams', team.id]);
  }

  deleteTeam(team: Team): void {
    this.teamsService.deleteTeam(team.id).subscribe({
      next: () => {
        this.message.success('Equipo eliminado correctamente');
        this.loadTeams();
      },
      error: () => {
        this.message.error('Error al eliminar el equipo. Puede que tenga proyectos activos.');
      },
    });
  }
}
