import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Skill {
  id: number;
  name: string;
  category: string;
  status: string;        // Pendiente, Aprobada, Rechazada
  offersCount: number;
  requestsCount: number;
}

// Estados del flujo de aprobación del catálogo
export const SKILL_STATUSES = ['Pendiente', 'Aprobada', 'Rechazada'];

@Injectable({
  providedIn: 'root'
})
export class SkillService {
  private baseUrl = 'http://localhost:5066/api/Skills';

  constructor(private http: HttpClient) { }

  getAll(filters: { category?: string; search?: string; status?: string } = {}): Observable<Skill[]> {
    let params = new HttpParams();
    if (filters.category) params = params.set('category', filters.category);
    if (filters.search) params = params.set('search', filters.search);
    if (filters.status) params = params.set('status', filters.status);
    return this.http.get<Skill[]>(this.baseUrl, { params });
  }

  // Aprobar o rechazar una habilidad propuesta (Moderador/Administrador)
  review(id: number, status: 'Aprobada' | 'Rechazada'): Observable<any> {
    return this.http.patch<any>(`${this.baseUrl}/${id}/review`, { status });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  create(skill: { name: string; category: string }): Observable<any> {
    return this.http.post<any>(this.baseUrl, skill);
  }

  update(id: number, skill: { id: number; name: string; category: string }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, skill);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
