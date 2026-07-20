import { PassedInitialConfig } from 'angular-auth-oidc-client';

declare global {
  interface Window {
    __env?: { keycloakAuthority?: string };
  }
}

export const authConfig: PassedInitialConfig = {
  config: {
    authority: window.__env?.keycloakAuthority || 'http://localhost:8080/realms/cartera',
    redirectUrl: window.location.origin + '/callback',
    postLogoutRedirectUri: window.location.origin,
    clientId: 'cartera-frontend',
    scope: 'openid profile email',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    postLoginRoute: '/dashboard',
    secureRoutes: [window.location.origin + '/api', '/api'],
  },
};
