import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NzLayoutModule } from 'ng-zorro-antd/layout';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NzLayoutModule],
  template: `
    <nz-layout>
      <nz-header>
        <h1 style="color: white; margin: 0; padding: 0 24px;">Cartera de Proyectos TIC</h1>
      </nz-header>
      <nz-content style="padding: 24px;">
        <router-outlet />
      </nz-content>
    </nz-layout>
  `,
})
export class AppComponent {}
