import { Routes } from '@angular/router';

export const personsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./persons-list/persons-list.component').then(m => m.PersonsListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./person-profile/person-profile.component').then(m => m.PersonProfileComponent),
  },
];
