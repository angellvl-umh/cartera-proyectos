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
  {
    path: 'persons',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/persons/persons.routes').then(m => m.personsRoutes),
  },
  {
    path: 'portfolio',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/portfolio/portfolio.component').then(m => m.PortfolioComponent),
  },
  {
    path: 'my-tasks',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/my-tasks/my-tasks.component').then(m => m.MyTasksComponent),
  },
  {
    path: 'capacity',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/capacity/capacity.component').then(m => m.CapacityComponent),
  },
  { path: 'callback', loadComponent: () => import('./core/callback.component').then(m => m.CallbackComponent) },
];
