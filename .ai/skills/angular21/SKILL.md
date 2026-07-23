---
name: angular21-skill
description: Angular 21 development patterns - signals-first, zoneless, standalone components, inject(), control flow, NG-ZORRO 21, Vitest. Use when generating or reviewing Angular code.
---

# Angular 21 Skill

## Core Patterns

### Zoneless Change Detection
```typescript
// app.config.ts
import { provideZonelessChangeDetection } from '@angular/core';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient()
  ]
};
```

### Signals-First State Management
```typescript
// Always use signals for component state
name = signal('');
items = signal<Item[]>([]);
loading = signal(false);

// Computed for derived state
filteredItems = computed(() => this.items().filter(i => i.active));

// linkedSignal for dependent state
selectedId = signal<number | null>(null);
selectedItem = linkedSignal(() => this.items().find(i => i.id === this.selectedId()));
```

### Dependency Injection
```typescript
// ALWAYS use inject(), NEVER constructor injection
private readonly http = inject(HttpClient);
private readonly router = inject(Router);
private readonly projectService = inject(ProjectService);
```

### Standalone Components (obligatorio)
```typescript
@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [NzTableModule, NzButtonModule, NzTagModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `...`
})
export class ProjectListComponent { }
```

### Control Flow (NUNCA *ngIf, *ngFor, *ngSwitch)
```html
@if (loading()) {
  <nz-spin />
} @else {
  @for (item of items(); track item.id) {
    <app-item-card [item]="item" />
  } @empty {
    <nz-empty />
  }
}

@switch (status()) {
  @case ('active') { <nz-tag nzColor="green">Activo</nz-tag> }
  @case ('paused') { <nz-tag nzColor="orange">Pausado</nz-tag> }
  @default { <nz-tag>Desconocido</nz-tag> }
}
```

### Services with Signals
```typescript
@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);

  getProjects() {
    return this.http.get<Project[]>('/api/projects');
  }

  getProject(id: number) {
    return this.http.get<Project>(`/api/projects/${id}`);
  }
}
```

### Signal Forms (experimental)
```typescript
import { SignalForm, SignalFormControl } from '@angular/forms/experimental';

// For new forms, use Signal Forms
title = new SignalFormControl('');
description = new SignalFormControl('');

// Reactive Forms as fallback for complex cases
form = inject(FormBuilder).group({
  title: ['', Validators.required],
  description: ['']
});
```

### toSignal() for Observables
```typescript
private readonly route = inject(ActivatedRoute);
private readonly projectService = inject(ProjectService);

id = toSignal(this.route.paramMap.pipe(map(p => +p.get('id')!)));
project = toSignal(this.projectService.getProject(this.id()!));
```

## NG-ZORRO 21 Patterns

- Import modules individually: `NzTableModule`, `NzButtonModule`, etc.
- All components support OnPush and zoneless natively
- Use `nz-` prefix for all UI components
- Follow Ant Design layout patterns (NzLayoutModule, NzGridModule)

## Testing with Vitest
```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/angular';

describe('ProjectListComponent', () => {
  it('should render projects', async () => {
    await render(ProjectListComponent, {
      providers: [provideHttpClientTesting()]
    });
    expect(screen.getByText('Proyectos')).toBeTruthy();
  });
});
```

## File Structure
```
src/app/features/<feature>/
├── <feature>.component.ts       # Smart component (page)
├── <feature>.component.html     # Template (optional, can be inline)
├── <feature>.routes.ts          # Feature routes
├── components/                  # Dumb/presentational components
│   └── <name>.component.ts
├── services/
│   └── <name>.service.ts
└── models/
    └── <name>.model.ts
```

## Prohibiciones
- ❌ NUNCA usar NgModules
- ❌ NUNCA usar constructor injection
- ❌ NUNCA usar *ngIf, *ngFor, *ngSwitch
- ❌ NUNCA usar zone.js
- ❌ NUNCA usar Karma/Jasmine
- ❌ NUNCA usar BehaviorSubject cuando un signal() basta
