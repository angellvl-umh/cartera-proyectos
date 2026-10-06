import { describe, expect, it } from 'vitest';
import {
  DEFAULT_PAGE,
  DEFAULT_PAGE_SIZE,
  parseProjectsListParams,
  serializeProjectsListParams,
  type ProjectsListParams,
} from './projects-list-query';

// ── parseProjectsListParams ───────────────────────────────────────────────────

describe('parseProjectsListParams', () => {
  it('returns defaults for empty params', () => {
    const result = parseProjectsListParams({});
    expect(result.q).toBe('');
    expect(result.status).toBeNull();
    expect(result.complexity).toBeNull();
    expect(result.tagIds).toEqual([]);
    expect(result.promoterId).toBeNull();
    expect(result.page).toBe(DEFAULT_PAGE);
    expect(result.pageSize).toBe(DEFAULT_PAGE_SIZE);
    expect(result.sortBy).toBeNull();
    expect(result.sortDir).toBe('asc');
    expect(result.viewMode).toBe('tabla');
  });

  it('parses valid q param', () => {
    const result = parseProjectsListParams({ q: 'portal' });
    expect(result.q).toBe('portal');
  });

  it('parses valid status', () => {
    const result = parseProjectsListParams({ status: 'InSprint' });
    expect(result.status).toBe('InSprint');
  });

  it('ignores invalid status', () => {
    const result = parseProjectsListParams({ status: 'Unknown' });
    expect(result.status).toBeNull();
  });

  it('parses valid complexity', () => {
    const result = parseProjectsListParams({ complexity: 'Large' });
    expect(result.complexity).toBe('Large');
  });

  it('ignores invalid complexity', () => {
    const result = parseProjectsListParams({ complexity: 'Huge' });
    expect(result.complexity).toBeNull();
  });

  it('parses single tagId as array', () => {
    const result = parseProjectsListParams({ tagIds: '5' });
    expect(result.tagIds).toEqual([5]);
  });

  it('parses multiple tagIds (repeated param)', () => {
    const result = parseProjectsListParams({ tagIds: ['1', '2', '3'] });
    expect(result.tagIds).toEqual([1, 2, 3]);
  });

  it('ignores non-numeric tagIds', () => {
    const result = parseProjectsListParams({ tagIds: ['1', 'x', '3'] });
    expect(result.tagIds).toEqual([1, 3]);
  });

  it('parses valid promoterId', () => {
    const result = parseProjectsListParams({ promoterId: '3' });
    expect(result.promoterId).toBe(3);
  });

  it('ignores non-numeric promoterId', () => {
    const result = parseProjectsListParams({ promoterId: 'abc' });
    expect(result.promoterId).toBeNull();
  });

  it('parses valid page', () => {
    const result = parseProjectsListParams({ page: '5' });
    expect(result.page).toBe(5);
  });

  it('ignores page <= 0', () => {
    const result = parseProjectsListParams({ page: '-1' });
    expect(result.page).toBe(DEFAULT_PAGE);
  });

  it('ignores non-numeric page', () => {
    const result = parseProjectsListParams({ page: 'abc' });
    expect(result.page).toBe(DEFAULT_PAGE);
  });

  it('parses valid pageSize', () => {
    const result = parseProjectsListParams({ pageSize: '50' });
    expect(result.pageSize).toBe(50);
  });

  it('ignores invalid pageSize (not in [10,20,50,100])', () => {
    const result = parseProjectsListParams({ pageSize: '7' });
    expect(result.pageSize).toBe(DEFAULT_PAGE_SIZE);
  });

  it('parses valid sortBy and sortDir=desc', () => {
    const result = parseProjectsListParams({ sortBy: 'promoter', sortDir: 'desc' });
    expect(result.sortBy).toBe('promoter');
    expect(result.sortDir).toBe('desc');
  });

  it('parses valid sortBy with default sortDir when sortDir is absent', () => {
    const result = parseProjectsListParams({ sortBy: 'title' });
    expect(result.sortBy).toBe('title');
    expect(result.sortDir).toBe('asc');
  });

  it('ignores unknown sortBy', () => {
    const result = parseProjectsListParams({ sortBy: 'foo' });
    expect(result.sortBy).toBeNull();
  });

  it('sortDir without sortBy defaults to asc and sortBy is null', () => {
    const result = parseProjectsListParams({ sortDir: 'desc' });
    expect(result.sortBy).toBeNull();
    expect(result.sortDir).toBe('asc');
  });

  it('treats unknown sortDir as asc', () => {
    const result = parseProjectsListParams({ sortBy: 'title', sortDir: 'up' });
    expect(result.sortDir).toBe('asc');
  });

  it('parses view=tablero', () => {
    const result = parseProjectsListParams({ view: 'tablero' });
    expect(result.viewMode).toBe('tablero');
  });

  it('ignores unknown view', () => {
    const result = parseProjectsListParams({ view: 'foo' });
    expect(result.viewMode).toBe('tabla');
  });

  it('parses full URL params: promoterId + tagIds + page + pageSize + sortBy + sortDir + view', () => {
    const result = parseProjectsListParams({
      promoterId: '3',
      tagIds: ['1', '2'],
      page: '2',
      pageSize: '50',
      sortBy: 'promoter',
      sortDir: 'desc',
      view: 'tabla',
    });
    expect(result.promoterId).toBe(3);
    expect(result.tagIds).toEqual([1, 2]);
    expect(result.page).toBe(2);
    expect(result.pageSize).toBe(50);
    expect(result.sortBy).toBe('promoter');
    expect(result.sortDir).toBe('desc');
    expect(result.viewMode).toBe('tabla');
  });

  it('all invalid: uses defaults', () => {
    const result = parseProjectsListParams({
      promoterId: 'abc',
      page: '-1',
      pageSize: '7',
      sortBy: 'foo',
      sortDir: 'up',
      status: 'Unknown',
      complexity: 'Huge',
      tagIds: 'x',
      view: 'foo',
    });
    expect(result.promoterId).toBeNull();
    expect(result.page).toBe(DEFAULT_PAGE);
    expect(result.pageSize).toBe(DEFAULT_PAGE_SIZE);
    expect(result.sortBy).toBeNull();
    expect(result.sortDir).toBe('asc');
    expect(result.status).toBeNull();
    expect(result.complexity).toBeNull();
    expect(result.tagIds).toEqual([]);
    expect(result.viewMode).toBe('tabla');
  });

  // ── Fix #4: strict integer validation tests ───────────────────────────────────

  it('promoterId=3abc is ignored (parseInt would return 3, strict validation rejects it)', () => {
    const result = parseProjectsListParams({ promoterId: '3abc' });
    expect(result.promoterId).toBeNull();
  });

  it('page=2.5 is ignored (strict validation rejects non-integer strings)', () => {
    const result = parseProjectsListParams({ page: '2.5' });
    expect(result.page).toBe(DEFAULT_PAGE);
  });

  it('pageSize=50x is ignored', () => {
    const result = parseProjectsListParams({ pageSize: '50x' });
    expect(result.pageSize).toBe(DEFAULT_PAGE_SIZE);
  });

  it('tagIds=1&tagIds=2x: keeps 1, discards 2x', () => {
    const result = parseProjectsListParams({ tagIds: ['1', '2x'] });
    expect(result.tagIds).toEqual([1]);
  });

  it('promoterId= (empty string) is ignored', () => {
    const result = parseProjectsListParams({ promoterId: '' });
    expect(result.promoterId).toBeNull();
  });
});

// ── serializeProjectsListParams ───────────────────────────────────────────────

describe('serializeProjectsListParams', () => {
  const defaults: ProjectsListParams = {
    q: '',
    status: null,
    complexity: null,
    tagIds: [],
    promoterId: null,
    page: DEFAULT_PAGE,
    pageSize: DEFAULT_PAGE_SIZE,
    sortBy: null,
    sortDir: 'asc',
    viewMode: 'tabla',
  };

  it('returns empty object for all-defaults', () => {
    expect(serializeProjectsListParams(defaults)).toEqual({});
  });

  it('includes q when non-empty', () => {
    const params = serializeProjectsListParams({ ...defaults, q: 'portal' });
    expect(params['q']).toBe('portal');
  });

  it('includes status when set', () => {
    const params = serializeProjectsListParams({ ...defaults, status: 'InSprint' });
    expect(params['status']).toBe('InSprint');
  });

  it('includes tagIds as array of strings', () => {
    const params = serializeProjectsListParams({ ...defaults, tagIds: [1, 2] });
    expect(params['tagIds']).toEqual(['1', '2']);
  });

  it('includes promoterId when set', () => {
    const params = serializeProjectsListParams({ ...defaults, promoterId: 3 });
    expect(params['promoterId']).toBe('3');
  });

  it('omits page when page is 1', () => {
    const params = serializeProjectsListParams({ ...defaults, page: 1 });
    expect(params['page']).toBeUndefined();
  });

  it('includes page when > 1', () => {
    const params = serializeProjectsListParams({ ...defaults, page: 3 });
    expect(params['page']).toBe('3');
  });

  it('omits pageSize when 20 (default)', () => {
    const params = serializeProjectsListParams({ ...defaults, pageSize: 20 });
    expect(params['pageSize']).toBeUndefined();
  });

  it('includes pageSize when not default', () => {
    const params = serializeProjectsListParams({ ...defaults, pageSize: 50 });
    expect(params['pageSize']).toBe('50');
  });

  it('includes sortBy and omits sortDir=asc (default)', () => {
    const params = serializeProjectsListParams({ ...defaults, sortBy: 'title', sortDir: 'asc' });
    expect(params['sortBy']).toBe('title');
    expect(params['sortDir']).toBeUndefined();
  });

  it('includes sortBy and sortDir when desc', () => {
    const params = serializeProjectsListParams({ ...defaults, sortBy: 'promoter', sortDir: 'desc' });
    expect(params['sortBy']).toBe('promoter');
    expect(params['sortDir']).toBe('desc');
  });

  it('omits sortDir when sortBy is null', () => {
    const params = serializeProjectsListParams({ ...defaults, sortBy: null, sortDir: 'asc' });
    expect(params['sortBy']).toBeUndefined();
    expect(params['sortDir']).toBeUndefined();
  });

  it('includes view=tablero when viewMode is tablero', () => {
    const params = serializeProjectsListParams({ ...defaults, viewMode: 'tablero' });
    expect(params['view']).toBe('tablero');
  });

  it('omits view when viewMode is tabla (default)', () => {
    const params = serializeProjectsListParams({ ...defaults, viewMode: 'tabla' });
    expect(params['view']).toBeUndefined();
  });
});
