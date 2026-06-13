import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzResultModule } from 'ng-zorro-antd/result';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [NzCardModule, NzButtonModule, NzResultModule],
  template: `
    @if (user()) {
      <nz-card nzTitle="Mi Perfil">
        <p><strong>Nombre:</strong> {{ user().Name }}</p>
        <p><strong>Email:</strong> {{ user().Email }}</p>
        <p><strong>Rol:</strong> {{ user().Role }}</p>
        <button nz-button nzType="default" (click)="logout()">Cerrar sesión</button>
      </nz-card>
    } @else {
      <nz-result nzStatus="info" nzTitle="Cargando perfil..." />
    }
  `,
})
export class DashboardComponent {
  private readonly http = inject(HttpClient);
  private readonly oidc = inject(OidcSecurityService);

  user = signal<any>(null);

  constructor() {
    this.http.get('/api/me').subscribe(data => this.user.set(data));
  }

  logout(): void {
    this.oidc.logoff().subscribe();
  }
}
