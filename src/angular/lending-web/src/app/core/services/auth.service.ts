import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { rolesFromJwt } from '../jwt-claims';
import { toUserError } from './api-error';

const TOKEN_KEY = 'lending_access_token';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName?: string;
  dateOfBirth: string;
  monthlyIncome?: number;
  employmentStatus: string;
  taxIdMasked?: string;
  phoneNumber?: string;
}

export interface UserProfile {
  id: string;
  email: string | null;
  displayName: string | null;
  dateOfBirth: string | null;
  employmentStatus: string | null;
  phoneNumber: string | null;
  monthlyIncome: number | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly _token = signal<string | null>(this.readToken());

  readonly token = this._token.asReadonly();
  readonly isLoggedIn = computed(() => this._token() != null);
  readonly roles = computed(() => {
    const t = this._token();
    if (!t) return [] as string[];
    return rolesFromJwt(t);
  });
  readonly isAdmin = computed(() => this.roles().includes('Admin'));

  private readToken(): string | null {
    if (typeof sessionStorage === 'undefined') return null;
    return sessionStorage.getItem(TOKEN_KEY);
  }

  setTokenFromStorage(token: string): void {
    this._token.set(token);
  }

  async login(body: LoginRequest): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<{
        accessToken: string;
        userId: string;
      }>(`${environment.apiUrl}/api/v1/auth/login`, body).pipe(
        catchError((e) => toUserError(e))
      )
    );
    sessionStorage.setItem(TOKEN_KEY, res.accessToken);
    this._token.set(res.accessToken);
  }

  async getMe(): Promise<UserProfile> {
    const res = await firstValueFrom(
      this.http.get<UserProfile>(`${environment.apiUrl}/api/v1/auth/me`)
        .pipe(catchError((e) => toUserError(e)))
    );
    return res;
  }

  async register(body: RegisterRequest): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<{
        accessToken: string;
        userId: string;
      }>(`${environment.apiUrl}/api/v1/auth/register`, body).pipe(
        catchError((e) => toUserError(e))
      )
    );
    sessionStorage.setItem(TOKEN_KEY, res.accessToken);
    this._token.set(res.accessToken);
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
    void this.router.navigateByUrl('/');
  }
}
