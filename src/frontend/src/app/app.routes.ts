import { Routes } from '@angular/router';
import { AutoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
  },
  {
    path: 'teams',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/teams/teams.routes').then(m => m.teamsRoutes),
  },
  {
    path: 'projects',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/projects/projects.routes').then(m => m.projectsRoutes),
  },
  { path: 'callback', loadComponent: () => import('./core/callback.component').then(m => m.CallbackComponent) },
];
