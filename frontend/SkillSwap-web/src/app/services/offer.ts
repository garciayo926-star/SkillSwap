import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Offer {
  id: number;
  description: string;
  isActive: boolean;
  studentId: number;
  studentName: string;
  skillId: number;
  skillName: string;
  category: string;
}

export interface OfferFilters {
  skillName?: string;
  category?: string;
  studentId?: number | null;
  includeInactive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class OfferService {
  private baseUrl = 'http://localhost:5066/api/Offers';

  constructor(private http: HttpClient) { }

  getAll(filters: OfferFilters = {}): Observable<Offer[]> {
    let params = new HttpParams();
    if (filters.skillName) params = params.set('skillName', filters.skillName);
    if (filters.category) params = params.set('category', filters.category);
    if (filters.studentId) params = params.set('studentId', filters.studentId);
    if (filters.includeInactive) params = params.set('includeInactive', true);
    return this.http.get<Offer[]>(this.baseUrl, { params });
  }

  create(offer: { studentId: number; skillId: number; description: string; isActive: boolean }): Observable<any> {
    return this.http.post<any>(this.baseUrl, offer);
  }

  update(id: number, offer: { id: number; studentId: number; skillId: number; description: string; isActive: boolean }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, offer);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
