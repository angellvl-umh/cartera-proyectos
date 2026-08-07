import { Component, computed, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { BreakpointObserver } from '@angular/cdk/layout';
import { catchError, distinctUntilChanged, filter, map, of, skip, startWith, switchMap, take } from 'rxjs';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { ChatPanelComponent } from './features/chat/chat-panel.component';

const SIDEBAR_COLLAPSED_KEY = 'sidebar-collapsed';
const SIDEBAR_BREAKPOINT = '(max-width: 992px)';

interface MeDto { id: number; name: string; email: string; role: string; }

const ROUTE_LABELS: Record<string, string> = {
  dashboard: 'Dashboard',
  projects: 'Proyectos',
  teams: 'Equipos',
  persons: 'Personas',
  portfolio: 'Cartera',
  roadmap: 'Roadmap',
  reports: 'Informe semanal',
  'my-tasks': 'Mis tareas',
  capacity: 'Capacidad',
  'team-activity': 'Actividad',
  admin: 'Administración',
  promoters: 'Promotores',
  'organic-units': 'Unidades Orgánicas',
  tags: 'Etiquetas',
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NzLayoutModule, NzMenuModule, NzIconModule, NzButtonModule, NzTooltipModule, ChatPanelComponent],
  template: `
    <div style="display:flex;height:100vh;overflow:hidden">
      @if (!denied()) {
        <aside
          [style.width]="collapsed() ? '72px' : '252px'"
          style="flex:0 0 auto;background:var(--sidebar-bg);display:flex;flex-direction:column;overflow:hidden;transition:width 150ms ease"
        >
          <div
            [style.justify-content]="collapsed() ? 'center' : 'flex-start'"
            [style.padding]="collapsed() ? '0' : '0 16px'"
            style="height:64px;flex:0 0 auto;display:flex;align-items:center;gap:10px;border-bottom:1px solid var(--sidebar-border)"
          >
            <div style="width:38px;height:38px;border-radius:8px;background:var(--brand-primary);color:#fff;display:flex;align-items:center;justify-content:center;font-weight:800;font-size:13px;flex:0 0 auto">UMH</div>
            @if (!collapsed()) {
              <div style="line-height:1.25;overflow:hidden">
                <div style="color:#fff;font-weight:700;font-size:14px;white-space:nowrap">Cartera TIC</div>
                <div style="color:var(--sidebar-text-muted);font-size:11.5px;white-space:nowrap">Gestión de proyectos</div>
              </div>
            }
          </div>

          <nav style="flex:1 1 auto;overflow-y:auto;padding:16px 12px">
            @if (!collapsed()) {
              <div style="color:var(--sidebar-heading);font-size:11px;font-weight:700;letter-spacing:0.6px;text-transform:uppercase;padding:0 10px;margin-bottom:6px">Principal</div>
            }
            <a routerLink="/dashboard" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Dashboard" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="dashboard"></span>@if (!collapsed()) {<span>Dashboard</span>}
            </a>
            <a routerLink="/projects" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Proyectos" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="project"></span>@if (!collapsed()) {<span>Proyectos</span>}
            </a>
            <a routerLink="/teams" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Equipos" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="team"></span>@if (!collapsed()) {<span>Equipos</span>}
            </a>
            <a routerLink="/persons" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Personas" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="user"></span>@if (!collapsed()) {<span>Personas</span>}
            </a>
            <a routerLink="/portfolio" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Cartera" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="fund"></span>@if (!collapsed()) {<span>Cartera</span>}
            </a>
            <a routerLink="/roadmap" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Roadmap" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="schedule"></span>@if (!collapsed()) {<span>Roadmap</span>}
            </a>
            <a routerLink="/reports/weekly-portfolio" routerLinkActive="nav-item-active" class="sidebar-nav-item" style="padding-left:34px;font-size:13.5px" nz-tooltip nzTooltipTitle="Informe semanal" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="schedule"></span>@if (!collapsed()) {<span>Informe semanal</span>}
            </a>
            <a routerLink="/my-tasks" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Mis tareas" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="check-square"></span>@if (!collapsed()) {<span>Mis tareas</span>}
            </a>
            <a routerLink="/capacity" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Capacidad" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="team"></span>@if (!collapsed()) {<span>Capacidad</span>}
            </a>
            <a routerLink="/team-activity" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Actividad" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
              <span nz-icon nzType="eye"></span>@if (!collapsed()) {<span>Actividad</span>}
            </a>

            @if (isGestor()) {
              @if (!collapsed()) {
                <div style="color:var(--sidebar-heading);font-size:11px;font-weight:700;letter-spacing:0.6px;text-transform:uppercase;padding:0 10px;margin:16px 0 6px">Administración</div>
              }
              <a routerLink="/admin/promoters" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Promotores" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
                <span nz-icon nzType="solution"></span>@if (!collapsed()) {<span>Promotores</span>}
              </a>
              <a routerLink="/admin/organic-units" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Unidades Orgánicas" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
                <span nz-icon nzType="apartment"></span>@if (!collapsed()) {<span>Unidades Orgánicas</span>}
              </a>
              <a routerLink="/admin/tags" routerLinkActive="nav-item-active" class="sidebar-nav-item" nz-tooltip nzTooltipTitle="Etiquetas" [nzTooltipTrigger]="collapsed() ? 'hover' : null">
                <span nz-icon nzType="tags"></span>@if (!collapsed()) {<span>Etiquetas</span>}
              </a>
            }
          </nav>
        </aside>
      }

      <div style="flex:1 1 auto;display:flex;flex-direction:column;min-width:0;background:var(--bg-app)">
        @if (!denied()) {
          <header style="height:62px;flex:0 0 auto;background:var(--bg-surface);border-bottom:1px solid var(--border-strong-alt);padding:0 32px;display:flex;align-items:center;justify-content:space-between">
            <div style="display:flex;align-items:center;gap:12px">
              <button nz-button nzType="text" (click)="toggleCollapsed()" class="logout-btn">
                <span nz-icon [nzType]="collapsed() ? 'menu-unfold' : 'menu-fold'"></span>
              </button>
              <span style="font-size:13px;color:var(--text-muted)">Inicio / <span style="color:var(--ink);font-weight:600">{{ breadcrumb() }}</span></span>
            </div>
            <div style="display:flex;align-items:center;gap:12px">
              @if (me()) {
                <div style="width:34px;height:34px;border-radius:999px;background:var(--brand-primary);color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:13px">{{ initials() }}</div>
                <span style="font-size:13px;color:var(--ink-700)">{{ me()!.name }}</span>
                <span style="width:1px;height:20px;background:var(--border-strong-alt)"></span>
              }
              <button nz-button nzType="text" (click)="toggleChat()" class="logout-btn" nz-tooltip nzTooltipTitle="Chat IA">
                <span nz-icon nzType="message"></span>
              </button>
              <button nz-button nzType="text" (click)="logout()" class="logout-btn">
                <span nz-icon nzType="logout"></span> Salir
              </button>
            </div>
          </header>
        }
        <main style="flex:1 1 auto;overflow-y:auto;padding:30px 36px 48px">
          <router-outlet />
        </main>
      </div>
    </div>

    <app-chat-panel [open]="chatOpen()" (closed)="chatOpen.set(false)" />
  `,
  styles: [`
    .sidebar-nav-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 9px 10px;
      margin-bottom: 2px;
      border-radius: 8px;
      color: var(--sidebar-text);
      font-size: 14px;
      font-weight: 600;
      text-decoration: none;
      border-left: 3px solid transparent;
      transition: background 150ms ease, color 150ms ease;
    }
    .sidebar-nav-item:hover {
      background: rgba(255, 255, 255, 0.06);
      color: #fff;
    }
    .sidebar-nav-item.nav-item-active {
      background: var(--brand-primary-tint);
      color: #fff;
      border-left-color: var(--brand-primary);
    }
    .logout-btn:hover {
      color: var(--brand-primary) !important;
    }
  `],
})
export class AppComponent {
  private readonly oidc = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly breakpointObserver = inject(BreakpointObserver);

  private readonly storedCollapsed = ((): boolean | null => {
    const raw = localStorage.getItem(SIDEBAR_COLLAPSED_KEY);
    return raw === null ? null : raw === 'true';
  })();

  collapsed = signal(this.storedCollapsed ?? this.breakpointObserver.isMatched(SIDEBAR_BREAKPOINT));

  // Set to true when /api/me responds with 403, so sidebar/header are hidden.
  denied = signal(false);

  chatOpen = signal(false);

  constructor() {
    this.breakpointObserver
      .observe(SIDEBAR_BREAKPOINT)
      .pipe(
        map(state => state.matches),
        distinctUntilChanged(),
        skip(1), // ignora el estado inicial; solo reacciona al cruzar el breakpoint
        takeUntilDestroyed(),
      )
      .subscribe(matches => this.collapsed.set(matches));

    effect(() => {
      localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(this.collapsed()));
    });
  }

  toggleCollapsed(): void {
    this.collapsed.set(!this.collapsed());
  }

  toggleChat(): void {
    this.chatOpen.set(!this.chatOpen());
  }

  // Espera a que el token OIDC esté disponible antes de llamar a /api/me,
  // para que el interceptor pueda añadir el header Authorization.
  readonly me = toSignal(
    this.oidc.isAuthenticated$.pipe(
      filter(({ isAuthenticated }) => isAuthenticated),
      take(1),
      switchMap(() => this.http.get<MeDto>('/api/me')),
      catchError((err: HttpErrorResponse) => {
        if (err.status === 403) {
          this.denied.set(true);
          // Detect "desactivado" in the ProblemDetails detail/title
          const body = err.error as { detail?: string; title?: string } | null;
          const message = body?.detail ?? body?.title ?? '';
          const reason = message.includes('desactivado') ? 'inactive' : null;
          const extras = reason ? { queryParams: { reason } } : {};
          this.router.navigate(['/access-denied'], extras);
        }
        return of(null);
      }),
    ),
    { initialValue: null as MeDto | null },
  );

  readonly isGestor = computed(() => this.me()?.role === 'Gestor');

  readonly initials = computed(() => {
    const name = this.me()?.name ?? '';
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(p => p[0]?.toUpperCase())
      .join('');
  });

  readonly breadcrumb = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
      map(url => {
        const segment = url.split('?')[0].split('/').filter(Boolean)[0] ?? '';
        return ROUTE_LABELS[segment] ?? 'Dashboard';
      }),
    ),
    { initialValue: 'Dashboard' },
  );

  logout(): void {
    this.oidc.logoff().subscribe();
  }
}
