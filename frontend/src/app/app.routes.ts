import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { userGuard } from './core/guards/user.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'books' },
      {
        path: 'books',
        loadComponent: () =>
          import('./features/books/book-list/book-list.component').then((m) => m.BookListComponent),
      },
      {
        path: 'requests/my',
        canActivate: [userGuard],
        loadComponent: () =>
          import('./features/requests/my-requests/my-requests.component').then(
            (m) => m.MyRequestsComponent
          ),
      },
      {
        path: 'requests',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/requests/admin-requests/admin-requests.component').then(
            (m) => m.AdminRequestsComponent
          ),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notifications/notifications-page/notifications-page.component').then(
            (m) => m.NotificationsPageComponent
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'books' },
];
