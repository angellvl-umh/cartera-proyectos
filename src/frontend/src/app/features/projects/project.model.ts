export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type ProjectStatus =
  | 'Proposed'
  | 'Approved'
  | 'InProgress'
  | 'Paused'
  | 'Completed'
  | 'Cancelled';

export type ProjectComplexity = 'Low' | 'Medium' | 'High' | 'VeryHigh';

export interface Project {
  id: string;
  title: string;
  requestingUnit: string;
  complexity: ProjectComplexity;
  status: ProjectStatus;
  portfolioYear: number | null;
  startDate: string | null;
  endDate: string | null;
}

export interface ProjectTeam {
  teamId: string;
  teamName: string;
  isPrimary: boolean;
}

export interface ProjectDetail extends Project {
  description: string | null;
  teams: ProjectTeam[];
}

export interface CreateProjectDto {
  title: string;
  description?: string;
  requestingUnit: string;
  complexity: ProjectComplexity;
  portfolioYear?: number;
  startDate?: string;
  endDate?: string;
}

export type UpdateProjectDto = CreateProjectDto;

export interface ProjectFilters {
  status?: ProjectStatus;
  year?: number;
  teamId?: string;
  complexity?: ProjectComplexity;
  q?: string;
  page?: number;
  pageSize?: number;
}

export interface Team {
  id: string;
  name: string;
}
