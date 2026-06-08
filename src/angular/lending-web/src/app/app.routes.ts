import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { customerGuard } from './core/guards/customer.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(
        (m) => m.LoginComponent
      ),
  },
  {
    path: 'auth/register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then(
        (m) => m.RegisterComponent
      ),
  },
  {
    path: 'account/profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/account/profile/profile.component').then(
        (m) => m.ProfileComponent
      ),
  },
  {
    path: 'applications',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/applications/application-list/application-list.component'
      ).then((m) => m.ApplicationListComponent),
  },
  {
    path: 'applications/new',
    canActivate: [authGuard, customerGuard],
    loadComponent: () =>
      import(
        './features/applications/application-new/application-new.component'
      ).then((m) => m.ApplicationNewComponent),
  },
  {
    path: 'applications/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/applications/application-detail/application-detail.component'
      ).then((m) => m.ApplicationDetailComponent),
  },
  {
    path: 'admin/portfolio',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import(
        './features/admin/admin-portfolio/admin-portfolio.component'
      ).then((m) => m.AdminPortfolioComponent),
  },
  {
    path: 'admin/manual-verification',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import(
        './features/admin/admin-manual-verification/admin-manual-verification.component'
      ).then((m) => m.AdminManualVerificationComponent),
  },
  {
    path: 'admin/runs/:id',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import(
        './features/admin/admin-run-detail/admin-run-detail.component'
      ).then((m) => m.AdminRunDetailComponent),
  },
  { path: 'admin/budget', redirectTo: 'admin/portfolio', pathMatch: 'full' },
  { path: 'admin/runs', redirectTo: 'admin/portfolio', pathMatch: 'full' },
  { path: '**', redirectTo: '' },
];
