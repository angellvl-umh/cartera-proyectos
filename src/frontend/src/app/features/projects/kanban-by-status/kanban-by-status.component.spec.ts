/**
 * Tests del componente KanbanByStatusComponent:
 *  - carga el portfolio y distribuye los proyectos en columnas
 *  - el computed filteredProjects reacciona a cambios de signal inputs
 *    (filterPromoterId, filterTagIds, filterQ, filterComplexity)
 *
 * Nota sobre setInput: fixture.componentRef.setInput() no funciona con signal
 * inputs (`input()` de Angular 21) en este entorno de test porque
 * `@angular/platform-browser-dynamic` no está instalado. Sin él, el compilador
 * JIT no registra los signal inputs en `ɵcmp.inputs` y setInput lanza NG0303.
 * Como alternativa, se usa `node.applyValueToInputSignal(node, value)` —
 * el método interno de INPUT_SIGNAL_NODE que el framework usa al aplicar
 * bindings de padre a hijo — accedido vía el símbolo `ɵSIGNAL`.
 *
 * El patrón de inicialización sigue los tests de ChatPanelComponent:
 * createApplication + createEnvironmentInjector, sin TestBed.
 */
import '@angular/compiler';
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import {
  ApplicationRef,
  createEnvironmentInjector,
  EnvironmentInjector,
  provideZonelessChangeDetection,
  runInInjectionContext,
  ɵSIGNAL as SIGNAL,
} from '@angular/core';
import { createApplication } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { KanbanByStatusComponent } from './kanban-by-status.component';
import { ProjectComplexity, ProjectStatus } from '../project.model';

/**
 * Sets a signal input's value on a component instance.
 * Uses `node.applyValueToInputSignal(node, value)` — the method on
 * INPUT_SIGNAL_NODE that the Angular framework calls when a parent binds a
 * value to a signal input. This correctly updates the value AND invalidates
 * downstream computed signals (calls signalSetFn → producerIncrementEpoch →
 * producerNotifyConsumers).
 */
function setSignalInput<T>(comp: KanbanByStatusComponent, inputName: keyof KanbanByStatusComponent, value: T) {
  const inputSignal = comp[inputName] as unknown;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const node = (inputSignal as any)[SIGNAL as symbol];
  if (!node) throw new Error(`No SIGNAL node found for input '${String(inputName)}'`);
  node.applyValueToInputSignal(node, value);
}

// ── Fixtures ──────────────────────────────────────────────────────────────────

const PROJECT_A = {
  id: 1, title: 'Proyecto A', status: 'InSprint' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'Small' as ProjectComplexity,
  promoterId: 3, tagIds: [1, 2] as number[],
};
const PROJECT_B = {
  id: 2, title: 'Proyecto B', status: 'Stopped' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'Medium' as ProjectComplexity,
  promoterId: 4, tagIds: [3] as number[],
};
const PROJECT_C = {
  id: 3, title: 'Proyecto C', status: 'InSprint' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'Large' as ProjectComplexity,
  promoterId: null, tagIds: [] as number[],
};
// Proyecto D: DTO SIN la propiedad tagIds — simula una respuesta de la API
// en la que el campo no viene incluido (p.ej. endpoint antiguo o campo opcional).
// La línea `(p.tagIds ?? [])` del componente debe tolerarlo sin lanzar excepción.
const PROJECT_D_NO_TAGIDS = {
  id: 4, title: 'Proyecto D', status: 'Stopped' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'VerySmall' as ProjectComplexity,
  promoterId: null,
  // tagIds: intencionalmente ausente
} as unknown as { id: number; title: string; status: ProjectStatus; requestingUnit: string; complexity: ProjectComplexity; promoterId: null; tagIds: number[] };

const PORTFOLIO_RESPONSE = { projects: [PROJECT_A, PROJECT_B, PROJECT_C] };
const PORTFOLIO_WITH_D = { projects: [PROJECT_A, PROJECT_B, PROJECT_C, PROJECT_D_NO_TAGIDS] };

// ── App setup ─────────────────────────────────────────────────────────────────

let appRef: ApplicationRef;
let rootInjector: EnvironmentInjector;

beforeAll(async () => {
  appRef = await createApplication({ providers: [provideZonelessChangeDetection()] });
  rootInjector = appRef.injector.get(EnvironmentInjector);
});

afterAll(() => {
  appRef.destroy();
});

function createComponent(portfolioResponse = PORTFOLIO_RESPONSE): KanbanByStatusComponent {
  const httpMock = { get: () => of(portfolioResponse) };
  const routerMock = { navigate: () => {} };

  const inj = createEnvironmentInjector(
    [
      { provide: HttpClient, useValue: httpMock },
      { provide: Router, useValue: routerMock },
    ],
    rootInjector,
  );
  return runInInjectionContext(inj, () => new KanbanByStatusComponent());
}

function visibleProjects(comp: KanbanByStatusComponent) {
  return Object.values(comp.projectsByStatus()).flat();
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('KanbanByStatusComponent – carga inicial', () => {
  it('expone los tres proyectos cuando no hay filtros activos', () => {
    const comp = createComponent();
    expect(visibleProjects(comp)).toHaveLength(3);
  });

  it('projectsByStatus agrupa correctamente por estado', () => {
    const comp = createComponent();
    expect(comp.projectsByStatus()['InSprint']).toHaveLength(2); // A y C
    expect(comp.projectsByStatus()['Stopped']).toHaveLength(1);  // B
  });
});

describe('KanbanByStatusComponent – filterPromoterId reacciona como signal input', () => {
  it('filtra por promotor sin recrear el componente', () => {
    const comp = createComponent();
    expect(visibleProjects(comp)).toHaveLength(3);

    setSignalInput(comp, 'filterPromoterId', 3);

    const visible = visibleProjects(comp);
    expect(visible).toHaveLength(1);
    expect(visible[0].id).toBe(PROJECT_A.id);
  });

  it('limpiar filterPromoterId vuelve a mostrar todos', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterPromoterId', 3);
    setSignalInput(comp, 'filterPromoterId', null);
    expect(visibleProjects(comp)).toHaveLength(3);
  });
});

describe('KanbanByStatusComponent – filterTagIds reacciona como signal input', () => {
  it('filtra por etiqueta: solo muestra proyectos con alguna de las etiquetas', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterTagIds', [3]);

    const visible = visibleProjects(comp);
    expect(visible).toHaveLength(1);
    expect(visible[0].id).toBe(PROJECT_B.id);
  });

  it('sin filtro de etiquetas muestra todos', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterTagIds', []);
    expect(visibleProjects(comp)).toHaveLength(3);
  });

  it('tagIds tolerante: proyecto SIN propiedad tagIds en el DTO no lanza excepción', () => {
    // PROJECT_D_NO_TAGIDS no tiene la propiedad tagIds: prueba que (p.tagIds ?? [])
    // impide un TypeError al hacer .some() o .includes() sobre undefined.
    const comp = createComponent(PORTFOLIO_WITH_D); // 4 proyectos, uno sin tagIds

    // Sin filtro activo: los 4 proyectos son visibles (incluido D sin tagIds)
    expect(visibleProjects(comp)).toHaveLength(4);

    // Con un tagId activo: D queda excluido (tagIds ?? [] → no tiene ninguno)
    setSignalInput(comp, 'filterTagIds', [1]);
    // Solo A tiene tagId 1
    const visible = visibleProjects(comp);
    expect(visible).toHaveLength(1);
    expect(visible[0].id).toBe(PROJECT_A.id);
  });
});

describe('KanbanByStatusComponent – filterQ reacciona como signal input', () => {
  it('filtra por texto en el título (case-insensitive)', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterQ', 'proyecto a');

    const visible = visibleProjects(comp);
    expect(visible).toHaveLength(1);
    expect(visible[0].id).toBe(PROJECT_A.id);
  });
});

describe('KanbanByStatusComponent – filterComplexity reacciona como signal input', () => {
  it('filtra por complejidad', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterComplexity', 'Small');

    const visible = visibleProjects(comp);
    expect(visible).toHaveLength(1);
    expect(visible[0].id).toBe(PROJECT_A.id);
  });
});

describe('KanbanByStatusComponent – combinación de filtros cambia sin recrear el componente', () => {
  it('filterPromoterId y filterTagIds combinados: filtran correctamente', () => {
    const comp = createComponent();

    // Paso 1: filtrar por promotor 3 → solo A
    setSignalInput(comp, 'filterPromoterId', 3);
    expect(visibleProjects(comp)).toHaveLength(1);
    expect(visibleProjects(comp)[0].id).toBe(PROJECT_A.id);

    // Paso 2: añadir tagId 3 (A tiene promoterId=3 pero NO tagId 3)
    setSignalInput(comp, 'filterTagIds', [3]);
    expect(visibleProjects(comp)).toHaveLength(0);

    // Paso 3: quitar filtro de promotor → solo B tiene tagId 3
    setSignalInput(comp, 'filterPromoterId', null);
    expect(visibleProjects(comp)).toHaveLength(1);
    expect(visibleProjects(comp)[0].id).toBe(PROJECT_B.id);
  });
});
