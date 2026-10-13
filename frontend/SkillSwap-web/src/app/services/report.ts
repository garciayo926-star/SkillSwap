import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Report {
  id: number;
  reason: string;
  description: string;
  status: string;            // Abierto, EnRevision, Resuelto, Descartado
  createdAt: string;
  resolvedAt: string | null;
  resolution: string;
  reporter: string | null;
  reportedUserId: string | null;
  reported: string | null;
  exchangeId: number | null;
}

// Estados del ciclo de vida de un reporte (coinciden con el backend)
export const REPORT_STATUSES = ['Abierto', 'EnRevision', 'Resuelto', 'Descartado'];

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private baseUrl = 'http://localhost:5066/api/Reports';

  constructor(private http: HttpClient) { }

  getAll(status = ''): Observable<Report[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<Report[]>(this.baseUrl, { params });
  }

  // Crear un reporte (cualquier usuario autenticado)
  create(dto: {
    reportedUserId?: string | null;
    exchangeId?: number | null;
    reason: string;
    description: string;
  }): Observable<any> {
    return this.http.post<any>(this.baseUrl, dto);
  }

  // Gestionar un reporte (Moderador/Administrador)
  updateStatus(id: number, data: { status: string; resolution?: string }): Observable<any> {
    return this.http.patch<any>(`${this.baseUrl}/${id}/status`, data);
  }
}
