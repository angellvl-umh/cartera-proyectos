import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: 'promoters',
    loadComponent: () =>
      import('./promoters/promoters-list.component').then(m => m.PromotersListComponent),
  },
  {
    path: 'organic-units',
    loadComponent: () =>
      import('./organic-units/organic-units-list.component').then(m => m.OrganicUnitsListComponent),
  },
  {
    path: 'tags',
    loadComponent: () =>
      import('./tags/tags-list.component').then(m => m.TagsListComponent),
  },
  { path: '', redirectTo: 'promoters', pathMatch: 'full' },
];
