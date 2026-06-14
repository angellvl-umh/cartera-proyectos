import { Routes } from '@angular/router';

export const projectsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./projects-list/projects-list.component').then(m => m.ProjectsListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./project-detail/project-detail.component').then(m => m.ProjectDetailComponent),
  },
  {
    path: ':id/kanban',
    loadComponent: () =>
      import('./kanban-board/kanban-board.component').then(m => m.KanbanBoardComponent),
  },
  {
    path: ':id/report',
    loadComponent: () =>
      import('../reports/project-report.component').then(m => m.ProjectReportComponent),
  },
  {
    path: ':id/sprints/:sprintId/kanban',
    loadComponent: () =>
      import('./kanban-board/kanban-board.component').then(m => m.KanbanBoardComponent),
  },
];
