import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { AuthResponse, UserProfile } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = '/api/auth';
  private readonly tokenKey = 'sub_access_token';
  private readonly userKey = 'sub_user_profile';

  currentUser = signal<UserProfile | null>(this.getStoredUser());
  isAuthenticated = computed(() => !!this.currentUser());
  isAdmin = computed(() => this.currentUser()?.roles.includes('Admin') ?? false);
  isCustomer = computed(() => this.currentUser()?.roles.includes('Customer') ?? false);

  constructor(private http: HttpClient) {}

  register(data: { email: string; password: string; firstName: string; lastName: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data, { withCredentials: true }).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  login(data: { email: string; password: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, data, { withCredentials: true }).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh-token`, {}, { withCredentials: true }).pipe(
      tap(res => this.handleAuthSuccess(res)),
      catchError(err => {
        this.logout();
        return throwError(() => err);
      })
    );
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true }).subscribe({
      next: () => {},
      error: () => {}
    });
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.currentUser.set(null);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  private handleAuthSuccess(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    localStorage.setItem(this.userKey, JSON.stringify(response.user));
    this.currentUser.set(response.user);
  }

  private getStoredUser(): UserProfile | null {
    const raw = localStorage.getItem(this.userKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UserProfile;
    } catch {
      return null;
    }
  }
}
