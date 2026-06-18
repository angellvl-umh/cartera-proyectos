export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type ProjectStatus =
  | 'Stopped'
  | 'PlanningWithClient'
  | 'WaitingForDevelopers'
  | 'PlanningSprint'
  | 'InSprint'
  | 'DevelopmentOutsideSprint'
  | 'InTesting'
  | 'Completed'
  | 'PostponedByClient';

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  Stopped: 'Parado',
  PlanningWithClient: 'Planificando con cliente',
  WaitingForDevelopers: 'Esperando desarrolladores',
  PlanningSprint: 'Planificando sprint',
  InSprint: 'En sprint',
  DevelopmentOutsideSprint: 'Desarrollo fuera de sprint',
  InTesting: 'En pruebas',
  Completed: 'Finalizado',
  PostponedByClient: 'Pospuesto por cliente',
};

export type ProjectComplexity = 'VerySmall' | 'Small' | 'Medium' | 'Large' | 'VeryLarge';

export const PROJECT_COMPLEXITY_LABELS: Record<ProjectComplexity, string> = {
  VerySmall: 'Muy pequeño',
  Small: 'Pequeño',
  Medium: 'Medio',
  Large: 'Grande',
  VeryLarge: 'Muy grande',
};

export const PROJECT_COMPLEXITY_ORDER: Record<ProjectComplexity, number> = {
  VerySmall: 1,
  Small: 2,
  Medium: 3,
  Large: 4,
  VeryLarge: 5,
};

export interface StatusPillColor {
  bg: string;
  fg: string;
  dot: string;
}

// Colores institucionales UMH del handoff de diseño. PlanningWithClient, PlanningSprint
// y PostponedByClient no estaban en el mockup original (solo cubría 6 de los 9 estados);
// se extrapolan con tonos distintos a los ya usados para no colisionar visualmente.
export const PROJECT_STATUS_PILL_COLORS: Record<ProjectStatus, StatusPillColor> = {
  InSprint: { bg: '#E7F2EC', fg: '#1C7A4B', dot: '#1F9D5B' },
  InTesting: { bg: '#FBEEDE', fg: '#A8631A', dot: '#E08A2B' },
  WaitingForDevelopers: { bg: '#F6F0D6', fg: '#8A6B10', dot: '#C9A11A' },
  DevelopmentOutsideSprint: { bg: '#E7EEF8', fg: '#2A5BA8', dot: '#3A74D0' },
  Stopped: { bg: '#ECEAE6', fg: '#6B6661', dot: '#9A938B' },
  Completed: { bg: '#ECEAF3', fg: '#574686', dot: '#7C6BB0' },
  PlanningWithClient: { bg: '#E3F1F1', fg: '#1C6B6B', dot: '#2B9090' },
  PlanningSprint: { bg: '#F0E8F5', fg: '#7A3F94', dot: '#9C5BB8' },
  PostponedByClient: { bg: '#FBE9E7', fg: '#A8401F', dot: '#D35F3A' },
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

export type ProjectHealthStatus = 'OnTrack' | 'AtRisk' | 'Blocked';

export const PROJECT_HEALTH_STATUS_LABELS: Record<ProjectHealthStatus, string> = {
  OnTrack: 'En curso',
  AtRisk: 'En riesgo',
  Blocked: 'Bloqueado',
};

export const PROJECT_HEALTH_STATUS_COLORS: Record<ProjectHealthStatus, string> = {
  OnTrack: 'green',
  AtRisk: 'gold',
  Blocked: 'red',
};

export interface ProjectWeeklyUpdateDto {
  id: number;
  authorId: number;
  authorName: string;
  weekOf: string;
  summary: string;
  healthStatus: ProjectHealthStatus;
  updatedAt: string;
}

export interface UpsertWeeklyUpdateDto {
  summary: string;
  healthStatus: ProjectHealthStatus;
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
  estimatedBudget: number | null;
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
  estimatedBudget?: number | null;
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
