import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Exchange {
  id: number;
  initiatorStudentId: number;
  initiatorName: string;
  receiverStudentId: number;
  receiverName: string;
  offeredSkillId: number | null;
  offeredSkillName: string | null;
  requestedSkillId: number | null;
  requestedSkillName: string | null;
  status: string;
  dateProposed: string;
  dateCompleted: string | null;
  rating: number;
  feedback: string;
}

export interface MatchSkill {
  skillId: number;
  skillName: string;
}

export interface Match {
  studentId: number;
  fullName: string;
  bio: string;
  offeredSkillsToMe: MatchSkill[];
  wantedSkillsFromMe: MatchSkill[];
  matchType: 'Mutua' | 'Parcial';
}

export const EXCHANGE_STATUSES = ['Pendiente', 'Aceptado', 'Rechazado', 'Completado', 'Cancelado'];

@Injectable({
  providedIn: 'root'
})
export class ExchangeService {
  private baseUrl = 'http://localhost:5066/api/Exchanges';

  constructor(private http: HttpClient) { }

  getAll(filters: { studentId?: number | null; status?: string } = {}): Observable<Exchange[]> {
    let params = new HttpParams();
    if (filters.studentId) params = params.set('studentId', filters.studentId);
    if (filters.status) params = params.set('status', filters.status);
    return this.http.get<Exchange[]>(this.baseUrl, { params });
  }

  getMatches(studentId: number, includePartial = false): Observable<Match[]> {
    const params = new HttpParams().set('includePartial', includePartial);
    return this.http.get<Match[]>(`${this.baseUrl}/matches/${studentId}`, { params });
  }

  propose(exchange: {
    initiatorStudentId: number;
    receiverStudentId: number;
    offeredSkillId: number | null;
    requestedSkillId: number | null;
  }): Observable<any> {
    return this.http.post<any>(this.baseUrl, exchange);
  }

  updateStatus(id: number, data: { status: string; rating?: number; feedback?: string }): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
