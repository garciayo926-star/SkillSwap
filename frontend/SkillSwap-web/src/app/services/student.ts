import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Student {
  id: number;
  userId: string;
  username: string;
  fullName: string;
  firstName: string;
  lastName: string;
  email: string;
  bio: string;
  isActive: boolean;
  offeredSkills: string[];
  requestedSkills: string[];
  exchangesCount: number;
}

export interface StudentDetail {
  id: number;
  userId: string;
  username: string;
  fullName: string;
  firstName: string;
  lastName: string;
  email: string;
  bio: string;
  isActive: boolean;
  offers: { id: number; skillId: number; skillName: string; category: string; description: string; isActive: boolean }[];
  requests: { id: number; skillId: number; skillName: string; category: string; notes: string; isActive: boolean }[];
  exchangesPending: number;
  exchangesActive: number;
  exchangesCompleted: number;
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private baseUrl = 'http://localhost:5066/api/Students';

  constructor(private http: HttpClient) { }

  getAll(filters: { name?: string; skillName?: string } = {}): Observable<Student[]> {
    let params = new HttpParams();
    if (filters.name) params = params.set('name', filters.name);
    if (filters.skillName) params = params.set('skillName', filters.skillName);
    return this.http.get<Student[]>(this.baseUrl, { params });
  }

  getById(id: number): Observable<StudentDetail> {
    return this.http.get<StudentDetail>(`${this.baseUrl}/${id}`);
  }

  create(student: any): Observable<any> {
    return this.http.post<any>(this.baseUrl, student);
  }

  update(id: number, student: any): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, student);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
