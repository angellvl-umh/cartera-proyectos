/**
 * Tests del componente ProjectsListComponent:
 *  (a) Error HTTP seguido de cambio de filtro: la segunda carga se ejecuta y pinta datos.
 *  (b) Debounce: varias pulsaciones < 300ms → una sola navegación con el último texto.
 *  (c) Escribir, Atrás y volver a escribir el mismo texto: vuelve a navegar.
 *  (d) onPageSizeChange + onPageChange en el mismo tick: pageSize correcto en la URL final.
 *  (e) Carga inicial con ?promoterId=3&page=2: primera llamada a getProjects lleva esos filtros.
 *
 * Usa TestBed + BrowserTestingModule (zoneless).
 * ActivatedRoute se mockea con un ReplaySubject<ParamMap> para controlar queryParamMap.
 * Router se provee directamente como token DI con un mock de navigate.
 * ProjectsService se mockea con vi.fn().
 * vi.useFakeTimers() controla el debounce de 300ms.
 */
import '@angular/compiler';
import { describe, it, expect, beforeAll, beforeEach, afterEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserTestingModule, platformBrowserTesting } from '@angular/platform-browser/testing';
import { NO_ERRORS_SCHEMA, provideZonelessChangeDetection } from '@angular/core';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { ReplaySubject, Subject } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  PlusOutline, EyeOutline, EditOutline, DeleteOutline,
} from '@ant-design/icons-angular/icons';
import { ProjectsListComponent } from './projects-list.component';
import { ProjectsService } from '../projects.service';
import { PagedResult, Project } from '../project.model';

// ── TestBed initialization (once per file) ────────────────────────────────────

beforeAll(() => {
  TestBed.initTestEnvironment(BrowserTestingModule, platformBrowserTesting(), {
    teardown: { destroyAfterEach: false },
  });
});

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeProject(id: number, title = `Proyecto ${id}`): Project {
  return {
    id, title,
    requestingUnit: null, complexity: 'Small', status: 'Stopped',
    portfolioYear: null, startDate: null, endDate: null,
    groupPriority: null, promoterId: null, promoterName: null,
    estimatedBudget: null, businessValue: null, tags: [],
    organicUnitId: null, organicUnitName: null,
  };
}

function makePagedResult(items: Project[] = [], total?: number): PagedResult<Project> {
  return { items, total: total ?? items.length, page: 1, pageSize: 20 };
}

// ── Setup factory ─────────────────────────────────────────────────────────────

interface TestContext {
  fixture: ComponentFixture<ProjectsListComponent>;
  queryParamSubject: ReplaySubject<ParamMap>;
  getProjectsSubject: Subject<PagedResult<Project>>;
  navigateSpy: ReturnType<typeof vi.fn>;
  getProjectsSpy: ReturnType<typeof vi.fn>;
  errorSpy: ReturnType<typeof vi.fn>;
  emitParams(params: Record<string, string | string[]>): void;
}

function setupComponent(): TestContext {
  const queryParamSubject = new ReplaySubject<ParamMap>(1);
  let getProjectsSubject = new Subject<PagedResult<Project>>();
  const navigateSpy = vi.fn();
  const getProjectsSpy = vi.fn(() => getProjectsSubject.asObservable());
  const errorSpy = vi.fn();

  const routeMock = { queryParamMap: queryParamSubject.asObservable() };
  const routerMock = { navigate: navigateSpy };
  const serviceMock = {
    getProjects: getProjectsSpy,
    getTags: vi.fn(() => new Subject()),
    getPromoters: vi.fn(() => new Subject()),
    updateProject: vi.fn(() => new Subject()),
    getProject: vi.fn(() => new Subject()),
    deleteProject: vi.fn(() => new Subject()),
    createProject: vi.fn(() => new Subject()),
    getOrganicUnits: vi.fn(() => new Subject()),
    getTeams: vi.fn(() => new Subject()),
    transitionStatus: vi.fn(() => new Subject()),
    assignTeam: vi.fn(() => new Subject()),
    removeTeam: vi.fn(() => new Subject()),
    getNotes: vi.fn(() => new Subject()),
    createNote: vi.fn(() => new Subject()),
    deleteNote: vi.fn(() => new Subject()),
    getWeeklyUpdates: vi.fn(() => new Subject()),
    upsertWeeklyUpdate: vi.fn(() => new Subject()),
    getStatusHistory: vi.fn(() => new Subject()),
  };
  const messageMock = { error: errorSpy, success: vi.fn(), warning: vi.fn() };

  TestBed.configureTestingModule({
    imports: [ProjectsListComponent],
    providers: [
      provideZonelessChangeDetection(),
      provideNzIcons([PlusOutline, EyeOutline, EditOutline, DeleteOutline]),
      { provide: ActivatedRoute, useValue: routeMock },
      { provide: Router, useValue: routerMock },
      { provide: ProjectsService, useValue: serviceMock },
      { provide: NzMessageService, useValue: messageMock },
    ],
    // NO_ERRORS_SCHEMA suppresses errors for unregistered elements/attributes
    // (child components not fully rendered: nz-table, app-kanban-by-status, etc.)
    schemas: [NO_ERRORS_SCHEMA],
  });

  const fixture = TestBed.createComponent(ProjectsListComponent);

  return {
    fixture,
    queryParamSubject,
    get getProjectsSubject() { return getProjectsSubject; },
    set getProjectsSubject(s) { getProjectsSubject = s; },
    navigateSpy,
    getProjectsSpy,
    errorSpy,
    emitParams(params) { queryParamSubject.next(convertToParamMap(params)); },
  };
}

// ── (a) Error HTTP + carga posterior ─────────────────────────────────────────

describe('ProjectsListComponent – (a) error HTTP + carga posterior', () => {
  let ctx: TestContext;

  beforeEach(() => {
    vi.useFakeTimers();
    ctx = setupComponent();
  });
  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('después de un error HTTP, un cambio de filtro lanza una nueva carga', () => {
    ctx.fixture.detectChanges(); // ngOnInit
    ctx.emitParams({}); // initial params

    // Primera carga: error HTTP
    ctx.getProjectsSubject.error(new Error('network error'));
    expect(ctx.errorSpy).toHaveBeenCalledWith('Error al cargar los proyectos');
    expect(ctx.fixture.componentInstance.loading()).toBe(false);

    // Reemplazar Subject (el anterior ya emitió error y está cerrado)
    ctx.getProjectsSubject = new Subject();
    ctx.getProjectsSpy.mockReturnValue(ctx.getProjectsSubject.asObservable());

    // Segunda carga: nuevo queryParam emitido
    ctx.emitParams({ status: 'InSprint' });
    ctx.fixture.detectChanges();

    // La segunda carga responde con datos
    ctx.getProjectsSubject.next(makePagedResult([makeProject(1)]));
    ctx.fixture.detectChanges();

    expect(ctx.fixture.componentInstance.projects()).toHaveLength(1);
    expect(ctx.fixture.componentInstance.loading()).toBe(false);
  });
});

// ── (b) Debounce ──────────────────────────────────────────────────────────────

describe('ProjectsListComponent – (b) debounce de texto', () => {
  let ctx: TestContext;

  beforeEach(() => {
    vi.useFakeTimers();
    ctx = setupComponent();
  });
  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('varias pulsaciones en < 300ms resultan en una sola llamada a navigate con el último texto', () => {
    ctx.fixture.detectChanges();
    ctx.emitParams({});
    ctx.fixture.detectChanges();

    const callsBefore = ctx.navigateSpy.mock.calls.length;

    ctx.fixture.componentInstance.onQChange('p');
    ctx.fixture.componentInstance.onQChange('po');
    ctx.fixture.componentInstance.onQChange('por');
    ctx.fixture.componentInstance.onQChange('port');

    // Antes del debounce no debe haber nueva navegación de texto
    vi.advanceTimersByTime(299);
    expect(ctx.navigateSpy.mock.calls.length).toBe(callsBefore);

    // Pasados 300ms, una sola navegación con 'port'
    vi.advanceTimersByTime(1);
    const newCalls = ctx.navigateSpy.mock.calls.slice(callsBefore);
    expect(newCalls).toHaveLength(1);
    expect(newCalls[0][1].queryParams).toMatchObject({ q: 'port' });
  });
});

// ── (c) Escribir, Atrás, reescribir mismo texto ───────────────────────────────

describe('ProjectsListComponent – (c) escribir, Atrás y volver a escribir el mismo texto', () => {
  let ctx: TestContext;

  beforeEach(() => {
    vi.useFakeTimers();
    ctx = setupComponent();
  });
  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('vuelve a navegar aunque el texto sea idéntico (sin distinctUntilChanged)', () => {
    ctx.fixture.detectChanges();
    ctx.emitParams({});
    ctx.fixture.detectChanges();

    // Primera vez: escribe "ab" y espera debounce
    ctx.fixture.componentInstance.onQChange('ab');
    vi.advanceTimersByTime(300);
    const callsAfterFirst = ctx.navigateSpy.mock.calls.length;

    // "Atrás": URL vuelve a q=""
    ctx.emitParams({});
    ctx.fixture.detectChanges();

    // Escribe "ab" de nuevo
    ctx.fixture.componentInstance.onQChange('ab');
    vi.advanceTimersByTime(300);

    // Debe haber navegado de nuevo
    expect(ctx.navigateSpy.mock.calls.length).toBeGreaterThan(callsAfterFirst);
    const lastCall = ctx.navigateSpy.mock.calls.at(-1)!;
    expect(lastCall[1].queryParams).toMatchObject({ q: 'ab' });
  });
});

// ── (d) pageSize + pageIndex en el mismo tick ─────────────────────────────────

describe('ProjectsListComponent – (d) pageSize + pageIndex en el mismo tick', () => {
  let ctx: TestContext;

  beforeEach(() => {
    vi.useFakeTimers();
    ctx = setupComponent();
  });
  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('onPageSizeChange(100) seguido de onPageChange(3) lleva pageSize=100 en la URL', () => {
    ctx.fixture.detectChanges();
    // Empezamos en página 3, pageSize 20
    ctx.emitParams({ page: '3' });
    ctx.fixture.detectChanges();

    const callsBefore = ctx.navigateSpy.mock.calls.length;

    // NG-ZORRO emite pageSize y luego pageIndex síncronamente
    ctx.fixture.componentInstance.onPageSizeChange(100);
    ctx.fixture.componentInstance.onPageChange(3);

    // Hubo 2 llamadas a navigate (una por cada handler)
    expect(ctx.navigateSpy.mock.calls.length).toBe(callsBefore + 2);

    // La ÚLTIMA llamada es la de onPageChange(3); debe llevar pageSize=100
    // porque navigate() actualiza pendingState síncronamente
    const lastCall = ctx.navigateSpy.mock.calls.at(-1)!;
    const qp = lastCall[1].queryParams as Record<string, string>;
    expect(qp['pageSize']).toBe('100');
    // page=3 está por encima del default (1), así que aparece
    expect(qp['page']).toBe('3');
  });
});

// ── (e) Carga inicial con query params ───────────────────────────────────────

describe('ProjectsListComponent – (e) carga inicial con query params', () => {
  let ctx: TestContext;

  beforeEach(() => {
    vi.useFakeTimers();
    ctx = setupComponent();
  });
  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('con ?promoterId=3&page=2 la primera llamada a getProjects lleva esos filtros', () => {
    ctx.fixture.detectChanges();
    ctx.emitParams({ promoterId: '3', page: '2' });
    ctx.fixture.detectChanges();

    expect(ctx.getProjectsSpy).toHaveBeenCalled();
    const filters = ctx.getProjectsSpy.mock.calls[0][0];
    expect(filters.promoterId).toBe(3);
    expect(filters.page).toBe(2);
  });
});
