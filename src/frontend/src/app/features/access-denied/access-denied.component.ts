import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NzResultModule } from 'ng-zorro-antd/result';
import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzResultModule, NzButtonModule],
  template: `
    <div style="display:flex;align-items:center;justify-content:center;height:100vh;background:var(--bg-app,#f5f5f5)">
      <nz-result
        nzStatus="403"
        nzTitle="Sin acceso"
        [nzSubTitle]="subtitle()"
      >
        <div nz-result-extra>
          <button nz-button nzType="primary" (click)="logout()">
            Cerrar sesión
          </button>
        </div>
      </nz-result>
    </div>
  `,
})
export class AccessDeniedComponent {
  private readonly oidc = inject(OidcSecurityService);
  private readonly route = inject(ActivatedRoute);

  private readonly reason = toSignal(
    this.route.queryParamMap.pipe(map(p => p.get('reason'))),
    { initialValue: null as string | null },
  );

  readonly subtitle = computed(() =>
    this.reason() === 'inactive'
      ? 'Tu usuario está desactivado. Contacta con un gestor de la cartera.'
      : 'No tienes acceso a la aplicación. Solicita el alta a un gestor de la cartera.',
  );

  logout(): void {
    this.oidc.logoff().subscribe();
  }
}
