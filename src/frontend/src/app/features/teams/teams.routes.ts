import { Routes } from '@angular/router';

export const teamsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./teams-list/teams-list.component').then(m => m.TeamsListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./team-detail/team-detail.component').then(m => m.TeamDetailComponent),
  },
];
