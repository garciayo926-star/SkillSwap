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
        if (typeof window !== 'undefined' && window.localStorage) {
          localStorage.setItem('userId', response.userId);
          localStorage.setItem('username', response.username);
          localStorage.setItem('userRole', response.role);
          if (response.studentId) {
            localStorage.setItem('studentId', response.studentId);
          }
        }
      })
    );
  }

  register(userData: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/register`, userData);
  }

  getUserRole(): string {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem('userRole') || 'Student';
    }
    return 'Student';
  }

  logout(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.clear();
    }
  }
}