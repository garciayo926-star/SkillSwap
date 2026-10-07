import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Analytics {
  totalUsers: number;
  totalStudents: number;
  totalSkills: number;
  totalOffers: number;
  totalRequests: number;
  totalExchanges: number;
  studentsWithActivity: number;
  studentsWithoutActivity: number;
  averageRating: number;
  exchangesByStatus: { status: string; count: number }[];
  topOfferedSkills: { name: string; category: string; count: number }[];
  topRequestedSkills: { name: string; category: string; count: number }[];
  skillsByCategory: { category: string; count: number }[];
  usersByRole: { role: string; count: number }[];
}

export interface SystemHealth {
  apiStatus: string;
  database: string;
  databaseConnected: boolean;
  pendingMigrations: string[];
  serverHealth: string;
  timestamp: string;
}

export interface AdminUser {
  id: string;
  username: string;
  email: string;
  fullName: string;
  isActive: boolean;
  studentId: number | null;
  role: string;
}

export const ROLES = ['Master', 'Technical', 'Student'];

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = 'http://localhost:5066/api/Admin';

  constructor(private http: HttpClient) { }

  getAnalytics(): Observable<Analytics> {
    return this.http.get<Analytics>(`${this.baseUrl}/analytics`);
  }

  getSystemHealth(): Observable<SystemHealth> {
    return this.http.get<SystemHealth>(`${this.baseUrl}/system-health`);
  }

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.baseUrl}/users`);
  }

  updateUserRole(userId: string, roleName: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/users/${userId}/role`, { roleName });
  }
}
