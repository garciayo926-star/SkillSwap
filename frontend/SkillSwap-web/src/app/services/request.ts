import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SkillRequest {
  id: number;
  notes: string;
  isActive: boolean;
  studentId: number;
  studentName: string;
  skillId: number;
  skillName: string;
  category: string;
}

export interface RequestFilters {
  skillName?: string;
  category?: string;
  studentId?: number | null;
  includeInactive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class RequestService {
  private baseUrl = 'http://localhost:5066/api/Requests';

  constructor(private http: HttpClient) { }

  getAll(filters: RequestFilters = {}): Observable<SkillRequest[]> {
    let params = new HttpParams();
    if (filters.skillName) params = params.set('skillName', filters.skillName);
    if (filters.category) params = params.set('category', filters.category);
    if (filters.studentId) params = params.set('studentId', filters.studentId);
    if (filters.includeInactive) params = params.set('includeInactive', true);
    return this.http.get<SkillRequest[]>(this.baseUrl, { params });
  }

  create(request: { studentId: number; skillId: number; notes: string; isActive: boolean }): Observable<any> {
    return this.http.post<any>(this.baseUrl, request);
  }

  update(id: number, request: { id: number; studentId: number; skillId: number; notes: string; isActive: boolean }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
