import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideAuth, authInterceptor } from 'angular-auth-oidc-client';
import { es_ES, provideNzI18n } from 'ng-zorro-antd/i18n';

registerLocaleData(localeEs);
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  DashboardOutline, ProjectOutline, TeamOutline, UserOutline,
  LogoutOutline, PlusOutline, EditOutline, DeleteOutline,
  ArrowLeftOutline, EyeOutline, CheckOutline, CloseOutline,
  ExclamationCircleOutline, MenuFoldOutline, MenuUnfoldOutline,
  SearchOutline, FlagOutline,
  FundOutline, CheckSquareOutline, BarChartOutline,
  PieChartOutline, ThunderboltOutline, LayoutOutline,
  AppstoreOutline, WarningOutline, CheckCircleOutline,
  ClockCircleOutline, UserAddOutline, LinkOutline, CommentOutline,
  UserDeleteOutline, MessageOutline, SendOutline,
  FilterOutline, SyncOutline, PlusCircleOutline, FileTextOutline,
  QuestionCircleOutline, HistoryOutline,
} from '@ant-design/icons-angular/icons';
import { routes } from './app.routes';
import { authConfig } from './core/auth.config';

const icons = [
  DashboardOutline, ProjectOutline, TeamOutline, UserOutline,
  LogoutOutline, PlusOutline, EditOutline, DeleteOutline,
  ArrowLeftOutline, EyeOutline, CheckOutline, CloseOutline,
  ExclamationCircleOutline, MenuFoldOutline, MenuUnfoldOutline,
  SearchOutline, FlagOutline,
  FundOutline, CheckSquareOutline, BarChartOutline,
  PieChartOutline, ThunderboltOutline, LayoutOutline,
  AppstoreOutline, WarningOutline, CheckCircleOutline,
  ClockCircleOutline, UserAddOutline, LinkOutline, CommentOutline,
  UserDeleteOutline, MessageOutline, SendOutline,
  FilterOutline, SyncOutline, PlusCircleOutline, FileTextOutline,
  QuestionCircleOutline, HistoryOutline,
];

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor()])),
    provideAnimations(),
    provideAuth(authConfig),
    provideNzI18n(es_ES),
    provideNzIcons(icons),
  ],
};
