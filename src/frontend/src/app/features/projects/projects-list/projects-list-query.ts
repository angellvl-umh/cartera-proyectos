/**
 * Pure functions for parsing and serializing projects-list query parameters.
 * No Angular dependencies — testable without TestBed.
 */
import { Params } from '@angular/router';
import { ProjectComplexity, ProjectStatus } from '../project.model';

export type ViewMode = 'tabla' | 'tablero';
export type SortDir = 'asc' | 'desc';

export const VALID_SORT_FIELDS = ['title', 'promoter', 'organicUnit', 'complexity', 'portfolioYear'] as const;
export type SortField = (typeof VALID_SORT_FIELDS)[number];

export const VALID_STATUSES: ProjectStatus[] = [
  'Stopped', 'PlanningWithClient', 'WaitingForDevelopers', 'PlanningSprint',
  'InSprint', 'DevelopmentOutsideSprint', 'InTesting', 'Completed', 'PostponedByClient',
];

export const VALID_COMPLEXITIES: ProjectComplexity[] = [
  'VerySmall', 'Small', 'Medium', 'Large', 'VeryLarge',
];

export const DEFAULT_PAGE = 1;
export const DEFAULT_PAGE_SIZE = 20;
export const VALID_PAGE_SIZES = [10, 20, 50, 100] as const;

export interface ProjectsListParams {
  q: string;
  status: ProjectStatus | null;
  complexity: ProjectComplexity | null;
  tagIds: number[];
  promoterId: number | null;
  page: number;
  pageSize: number;
  sortBy: SortField | null;
  sortDir: SortDir;
  viewMode: ViewMode;
}

/**
 * Returns true only if the string represents a strict positive integer
 * (digits only, no decimals, no garbage after the number like "3abc").
 * parseInt('3abc') = 3 but /^\d+$/ rejects it.
 */
function isStrictPositiveInt(s: string): boolean {
  return /^\d+$/.test(s) && parseInt(s, 10) > 0;
}

/**
 * Parse raw query params from ActivatedRoute.queryParamMap into a
 * typed, validated ProjectsListParams. Invalid values are silently ignored
 * (treated as the default). Integer parameters (promoterId, page, pageSize,
 * tagIds) only accept strings matching /^\d+$/ — "3abc" is treated as absent.
 */
export function parseProjectsListParams(params: Params): ProjectsListParams {
  const q = typeof params['q'] === 'string' ? params['q'] : '';

  const rawStatus = params['status'];
  const status: ProjectStatus | null =
    typeof rawStatus === 'string' && VALID_STATUSES.includes(rawStatus as ProjectStatus)
      ? (rawStatus as ProjectStatus)
      : null;

  const rawComplexity = params['complexity'];
  const complexity: ProjectComplexity | null =
    typeof rawComplexity === 'string' && VALID_COMPLEXITIES.includes(rawComplexity as ProjectComplexity)
      ? (rawComplexity as ProjectComplexity)
      : null;

  const rawTagIds = params['tagIds'];
  const tagIdsRaw: unknown[] = Array.isArray(rawTagIds)
    ? rawTagIds
    : rawTagIds != null
      ? [rawTagIds]
      : [];
  const tagIds = tagIdsRaw
    .filter(v => typeof v === 'string' && isStrictPositiveInt(v as string))
    .map(v => parseInt(v as string, 10));

  const rawPromoterId = params['promoterId'];
  const promoterId: number | null =
    typeof rawPromoterId === 'string' && isStrictPositiveInt(rawPromoterId)
      ? parseInt(rawPromoterId, 10)
      : null;

  const rawPage = params['page'];
  const page: number =
    typeof rawPage === 'string' && isStrictPositiveInt(rawPage)
      ? parseInt(rawPage, 10)
      : DEFAULT_PAGE;

  const rawPageSize = params['pageSize'];
  const parsedPageSize =
    typeof rawPageSize === 'string' && isStrictPositiveInt(rawPageSize)
      ? parseInt(rawPageSize, 10)
      : -1;
  const pageSize = VALID_PAGE_SIZES.includes(parsedPageSize as (typeof VALID_PAGE_SIZES)[number])
    ? parsedPageSize
    : DEFAULT_PAGE_SIZE;

  const rawSortBy = params['sortBy'];
  const sortBy: SortField | null =
    typeof rawSortBy === 'string' && VALID_SORT_FIELDS.includes(rawSortBy as SortField)
      ? (rawSortBy as SortField)
      : null;

  const rawSortDir = params['sortDir'];
  // sortDir without sortBy is ignored (treated as asc); unknown values → asc
  const sortDir: SortDir =
    sortBy != null && rawSortDir === 'desc' ? 'desc' : 'asc';

  const rawView = params['view'];
  const viewMode: ViewMode = rawView === 'tablero' ? 'tablero' : 'tabla';

  return { q, status, complexity, tagIds, promoterId, page, pageSize, sortBy, sortDir, viewMode };
}

/**
 * Serialize a ProjectsListParams back to Params for router.navigate.
 * Omits parameters that are empty or at their default value.
 */
export function serializeProjectsListParams(p: ProjectsListParams): Params {
  const params: Params = {};

  if (p.q) params['q'] = p.q;
  if (p.status) params['status'] = p.status;
  if (p.complexity) params['complexity'] = p.complexity;
  if (p.tagIds.length > 0) params['tagIds'] = p.tagIds.map(String);
  if (p.promoterId != null) params['promoterId'] = String(p.promoterId);
  if (p.page !== DEFAULT_PAGE) params['page'] = String(p.page);
  if (p.pageSize !== DEFAULT_PAGE_SIZE) params['pageSize'] = String(p.pageSize);
  if (p.sortBy != null) {
    params['sortBy'] = p.sortBy;
    if (p.sortDir === 'desc') params['sortDir'] = 'desc';
    // asc is the default; omit it
  }
  if (p.viewMode !== 'tabla') params['view'] = p.viewMode;

  return params;
}
