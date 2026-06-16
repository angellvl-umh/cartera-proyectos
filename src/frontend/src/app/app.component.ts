import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzIconModule } from 'ng-zorro-antd/icon';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NzLayoutModule, NzMenuModule, NzIconModule],
  template: `
    <nz-layout style="min-height: 100vh;">
      <nz-sider nzCollapsible [(nzCollapsed)]="collapsed" [nzWidth]="220">
        <div style="height:64px; display:flex; align-items:center; justify-content:center; padding:0 16px;">
          @if (!collapsed()) {
            <span style="color:white; font-weight:600; font-size:14px; white-space:nowrap;">Cartera de Proyectos</span>
          } @else {
            <span style="color:white; font-weight:700; font-size:16px;">CP</span>
          }
        </div>
        <ul nz-menu nzTheme="dark" nzMode="inline">
          <li nz-menu-item routerLink="/dashboard" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="dashboard"></span>
            <span>Dashboard</span>
          </li>
          <li nz-menu-item routerLink="/projects" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="project"></span>
            <span>Proyectos</span>
          </li>
          <li nz-menu-item routerLink="/teams" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="team"></span>
            <span>Equipos</span>
          </li>
          <li nz-menu-item routerLink="/persons" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="user"></span>
            <span>Personas</span>
          </li>
          <li nz-menu-item routerLink="/portfolio" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="fund"></span>
            <span>Cartera</span>
          </li>
          <li nz-menu-item routerLink="/my-tasks" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="check-square"></span>
            <span>Mis tareas</span>
          </li>
          <li nz-menu-item routerLink="/capacity" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="team"></span>
            <span>Capacidad</span>
          </li>
          <li nz-submenu nzTitle="Administración" nzIcon="setting">
            <ul>
              <li nz-menu-item routerLink="/admin/promoters" routerLinkActive="ant-menu-item-selected">
                <span>Promotores</span>
              </li>
              <li nz-menu-item routerLink="/admin/organic-units" routerLinkActive="ant-menu-item-selected">
                <span>Unidades Orgánicas</span>
              </li>
              <li nz-menu-item routerLink="/admin/tags" routerLinkActive="ant-menu-item-selected">
                <span>Etiquetas</span>
              </li>
            </ul>
          </li>
        </ul>
      </nz-sider>
      <nz-layout>
        <nz-header style="background:#fff; padding:0 24px; display:flex; align-items:center; justify-content:space-between; box-shadow:0 1px 4px rgba(0,21,41,.08);">
          <span style="font-size:16px; font-weight:500;">Cartera de Proyectos TIC</span>
          <button nz-button nzType="link" (click)="logout()">
            <span nz-icon nzType="logout"></span> Salir
          </button>
        </nz-header>
        <nz-content style="padding:24px; background:#f0f2f5;">
          <router-outlet />
        </nz-content>
      </nz-layout>
    </nz-layout>
  `,
})
export class AppComponent {
  private readonly oidc = inject(OidcSecurityService);
  collapsed = signal(false);

  logout(): void {
    this.oidc.logoff().subscribe();
  }
}
