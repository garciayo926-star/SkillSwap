import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'http://localhost:5066/api/Auth';

  constructor(private http: HttpClient) { }

  login(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/login`, credentials).pipe(
      tap(response => {
        if (this.hasStorage()) {
          localStorage.setItem('userId', response.userId);
          localStorage.setItem('username', response.username);
          localStorage.setItem('userRole', response.role);
          if (response.studentId) {
            localStorage.setItem('studentId', response.studentId);
          } else {
            localStorage.removeItem('studentId');
          }
        }
      })
    );
  }

  register(userData: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/register`, userData);
  }

  // Devuelve '' cuando no hay sesión, así el guard puede redirigir al login
  getUserRole(): string {
    return this.getItem('userRole');
  }

  getUsername(): string {
    return this.getItem('username');
  }

  getUserId(): string {
    return this.getItem('userId');
  }

  getStudentId(): number | null {
    const id = this.getItem('studentId');
    return id ? Number(id) : null;
  }

  isLoggedIn(): boolean {
    return !!this.getUserId() && !!this.getUserRole();
  }

  isMaster(): boolean {
    return this.getUserRole() === 'Master';
  }

  // Master y Technical pueden administrar los catálogos del sistema
  isAdmin(): boolean {
    const role = this.getUserRole();
    return role === 'Master' || role === 'Technical';
  }

  logout(): void {
    if (this.hasStorage()) {
      localStorage.clear();
    }
  }

  private getItem(key: string): string {
    return this.hasStorage() ? localStorage.getItem(key) || '' : '';
  }

  // localStorage solo existe en el navegador (no durante el renderizado en servidor)
  private hasStorage(): boolean {
    return typeof window !== 'undefined' && !!window.localStorage;
  }
}
