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
    path: 'reports/weekly-portfolio',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/reports/weekly-portfolio-report.component').then(m => m.WeeklyPortfolioReportComponent),
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
  {
    path: 'capacity/forecast',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/capacity/capacity-forecast.component').then(m => m.CapacityForecastComponent),
  },
  {
    path: 'roadmap',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadComponent: () => import('./features/roadmap/roadmap.component').then(m => m.RoadmapComponent),
  },
  {
    path: 'admin',
    canActivate: [AutoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
  },
  { path: 'callback', loadComponent: () => import('./core/callback.component').then(m => m.CallbackComponent) },
];
