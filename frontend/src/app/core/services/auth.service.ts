import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../config';
import { AuthenticatedUser, LoginRequest } from '../models/user.model';

const STORAGE_KEY = 'library_system_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _currentUser = signal<AuthenticatedUser | null>(this.readFromStorage());

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => !!this._currentUser());
  readonly isAdmin = computed(() => this._currentUser()?.role === 'Admin');
  readonly isUser = computed(() => this._currentUser()?.role === 'User');

  constructor(private http: HttpClient) {}

  login(credentials: LoginRequest): Observable<AuthenticatedUser> {
    return this.http.post<AuthenticatedUser>(`${API_BASE_URL}/auth/login`, credentials).pipe(
      tap((user) => this.setUser(user))
    );
  }

  logout(): void {
    this._currentUser.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  getToken(): string | null {
    return this._currentUser()?.token ?? null;
  }

  private setUser(user: AuthenticatedUser): void {
    this._currentUser.set(user);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  }

  private readFromStorage(): AuthenticatedUser | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const parsed: AuthenticatedUser = JSON.parse(raw);
      if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }
}
