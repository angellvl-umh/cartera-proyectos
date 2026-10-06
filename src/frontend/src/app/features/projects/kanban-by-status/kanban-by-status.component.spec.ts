/**
 * Tests del componente KanbanByStatusComponent:
 *  - carga el portfolio y distribuye los proyectos en columnas
 *  - el computed filteredProjects reacciona a cambios de signal inputs
 *    (filterPromoterId, filterTagIds, filterQ, filterComplexity)
 *
 * Sigue el mismo patrón que los tests de ChatPanelComponent (createApplication +
 * createEnvironmentInjector, sin TestBed). Para modificar signal inputs en tests
 * sin fixture, accede al nodo signal interno mediante `ɵSIGNAL` y actualiza su
 * valor con `signalSetFn`, que es la misma función que usa Angular internamente
 * cuando el padre vincula un input. Esto replica lo que haría
 * `fixture.componentRef.setInput()` con un framework de testing completo.
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
 * Accesses the underlying SIGNAL node via Angular's internal ɵSIGNAL symbol,
 * then calls `node.applyValueToInputSignal(node, value)` — exactly the method
 * the framework uses when it applies a bound value to a signal input.
 * This correctly updates the value and invalidates downstream computed signals.
 */
function setSignalInput<T>(comp: KanbanByStatusComponent, inputName: keyof KanbanByStatusComponent, value: T) {
  const inputSignal = comp[inputName] as unknown;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const node = (inputSignal as any)[SIGNAL as symbol];
  if (!node) throw new Error(`No signal node found for input '${String(inputName)}'`);
  // applyValueToInputSignal calls signalSetFn internally, which:
  //   1. Updates node.value
  //   2. Calls producerIncrementEpoch() (global epoch++)
  //   3. Calls producerNotifyConsumers(node)
  node.applyValueToInputSignal(node, value);
}

// ── Fixtures ──────────────────────────────────────────────────────────────────

const PROJECT_A = {
  id: 1, title: 'Proyecto A', status: 'InSprint' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'Small' as ProjectComplexity,
  promoterId: 3, tagIds: [1, 2],
};
const PROJECT_B = {
  id: 2, title: 'Proyecto B', status: 'Stopped' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'Medium' as ProjectComplexity,
  promoterId: 4, tagIds: [3],
};
const PROJECT_C = {
  id: 3, title: 'Proyecto C', status: 'InSprint' as ProjectStatus,
  requestingUnit: 'TIC', complexity: 'Large' as ProjectComplexity,
  promoterId: null, tagIds: [],
};

const PORTFOLIO_RESPONSE = { projects: [PROJECT_A, PROJECT_B, PROJECT_C] };

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

function createComponent(): KanbanByStatusComponent {
  const httpMock = { get: () => of(PORTFOLIO_RESPONSE) };
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
  it('filtra por promotor sin recrear el componente (setInput)', () => {
    const comp = createComponent();

    // Estado inicial: 3 proyectos
    expect(visibleProjects(comp)).toHaveLength(3);

    // Cambiar el signal input y verificar que el computed reacciona
    setSignalInput(comp, 'filterPromoterId', 3);

    // Solo el proyecto A tiene promoterId === 3
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

    // Solo B tiene tagId 3
    const visible = visibleProjects(comp);
    expect(visible).toHaveLength(1);
    expect(visible[0].id).toBe(PROJECT_B.id);
  });

  it('sin filtro de etiquetas muestra todos', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterTagIds', []);
    expect(visibleProjects(comp)).toHaveLength(3);
  });

  it('tagIds tolerante con proyectos sin tagIds ([] → vacío, sin excepción)', () => {
    const comp = createComponent();
    setSignalInput(comp, 'filterTagIds', [99]);
    // Ningún proyecto tiene tagId 99
    expect(visibleProjects(comp)).toHaveLength(0);
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
  it('primero filterPromoterId, luego filterTagIds: ambos se aplican a la vez', () => {
    const comp = createComponent();

    // Paso 1: filtrar por promotor 3 → solo A
    setSignalInput(comp, 'filterPromoterId', 3);
    expect(visibleProjects(comp)).toHaveLength(1);
    expect(visibleProjects(comp)[0].id).toBe(PROJECT_A.id);

    // Paso 2: añadir filtro por tagId 3 (A tiene promoterId=3 pero NO tagId 3)
    setSignalInput(comp, 'filterTagIds', [3]);
    expect(visibleProjects(comp)).toHaveLength(0);

    // Paso 3: quitar filtro de promotor → solo B tiene tagId 3
    setSignalInput(comp, 'filterPromoterId', null);
    expect(visibleProjects(comp)).toHaveLength(1);
    expect(visibleProjects(comp)[0].id).toBe(PROJECT_B.id);
  });
});
