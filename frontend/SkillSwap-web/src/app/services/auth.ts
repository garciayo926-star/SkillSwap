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
          localStorage.setItem('token', response.token);
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

  // Token JWT firmado que la API valida en cada petición protegida
  getToken(): string {
    return this.getItem('token');
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

  // Hay sesión válida solo si existe un token (además del id y el rol)
  isLoggedIn(): boolean {
    return !!this.getToken() && !!this.getUserId() && !!this.getUserRole();
  }

  // Administrador: superusuario con control total
  isAdmin(): boolean {
    return this.getUserRole() === 'Administrador';
  }

  // Moderador: gestión de comunidad y contenido
  isModerador(): boolean {
    return this.getUserRole() === 'Moderador';
  }

  // Estudiante: usuario final (también el valor por defecto sin rol)
  isStudent(): boolean {
    const role = this.getUserRole();
    return role === 'Estudiante' || role === '';
  }

  // Perfiles con visión global (ven el dashboard analítico). El Estudiante NO.
  canSeeGlobalDashboard(): boolean {
    const role = this.getUserRole();
    return role === 'Administrador' || role === 'Moderador';
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
