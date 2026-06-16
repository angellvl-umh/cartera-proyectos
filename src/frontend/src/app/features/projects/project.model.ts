export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type ProjectStatus =
  | 'Stopped'
  | 'PlanningWithClient'
  | 'PlanningSprint'
  | 'InSprint'
  | 'DevelopmentOutsideSprint'
  | 'InTesting'
  | 'Completed'
  | 'PostponedByClient';

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  Stopped: 'Parado',
  PlanningWithClient: 'Planificando con cliente',
  PlanningSprint: 'Planificando sprint',
  InSprint: 'En sprint',
  DevelopmentOutsideSprint: 'Desarrollo fuera de sprint',
  InTesting: 'En pruebas',
  Completed: 'Finalizado',
  PostponedByClient: 'Pospuesto por cliente',
};

export type ProjectComplexity = 'VerySmall' | 'Small' | 'Medium' | 'Large';

export const PROJECT_COMPLEXITY_LABELS: Record<ProjectComplexity, string> = {
  VerySmall: 'Muy pequeño',
  Small: 'Pequeño',
  Medium: 'Medio',
  Large: 'Grande',
};

export type SiptGroup = 'WebTransversal' | 'RRHH' | 'Academico' | 'Sede' | 'Observatorio' | 'InvestigacionEconomico';

export const SIPT_GROUP_LABELS: Record<SiptGroup, string> = {
  WebTransversal: 'Web Transversal',
  RRHH: 'RRHH',
  Academico: 'Académico',
  Sede: 'Sede',
  Observatorio: 'Observatorio',
  InvestigacionEconomico: 'Investigación / Económico',
};

export interface TagDto {
  id: number;
  name: string;
  color: string | null;
}

export interface PromoterDto {
  id: number;
  name: string;
}

export interface OrganicUnitDto {
  id: number;
  name: string;
  code: string | null;
}

export interface ProjectNoteDto {
  id: number;
  projectId: number;
  authorId: number;
  authorName: string;
  text: string;
  createdAt: string;
}

export interface Project {
  id: number;
  title: string;
  requestingUnit: string | null;
  complexity: ProjectComplexity;
  status: ProjectStatus;
  portfolioYear: number | null;
  startDate: string | null;
  endDate: string | null;
  groupPriority: number | null;
  siptGroup: SiptGroup | null;
  promoterId: number | null;
  promoterName: string | null;
  tags: TagDto[];
}

export interface ProjectTeam {
  teamId: number;
  teamName: string;
  isPrimary: boolean;
}

export interface ProjectDetail extends Project {
  description: string | null;
  teams: ProjectTeam[];
  previousReferenceId: number | null;
  beneficiaryCount: number | null;
  organicUnitId: number | null;
  organicUnitName: string | null;
  uorOrder: number | null;
  desiredDeploymentDate: string | null;
  specificationsUrl: string | null;
  epicUrl: string | null;
  tags: TagDto[];
}

export interface CreateProjectDto {
  title: string;
  description?: string | null;
  requestingUnit?: string | null;
  complexity: ProjectComplexity;
  portfolioYear?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  previousReferenceId?: number | null;
  beneficiaryCount?: number | null;
  promoterId?: number | null;
  organicUnitId?: number | null;
  uorOrder?: number | null;
  groupPriority?: number | null;
  siptGroup?: SiptGroup | null;
  desiredDeploymentDate?: string | null;
  specificationsUrl?: string | null;
  epicUrl?: string | null;
  tagIds?: number[];
}

export type UpdateProjectDto = CreateProjectDto;

export interface ProjectFilters {
  status?: ProjectStatus;
  year?: number;
  teamId?: number;
  complexity?: ProjectComplexity;
  q?: string;
  tagId?: number;
  tagIds?: number[];
  siptGroup?: SiptGroup;
  promoterId?: number;
  page?: number;
  pageSize?: number;
}

export interface Team {
  id: number;
  name: string;
}
