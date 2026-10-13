import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Report, ReportService, REPORT_STATUSES } from '../../services/report';

@Component({
  selector: 'app-reports',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.html',
  styleUrls: ['./reports.css']
})
export class ReportsComponent implements OnInit {
  statuses = REPORT_STATUSES;

  reports = signal<Report[]>([]);
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  statusFilter = '';

  // Gestión de un reporte seleccionado
  managing: Report | null = null;
  newStatus = '';
  resolution = '';

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.reportService.getAll(this.statusFilter).subscribe({
      next: (data) => {
        this.reports.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'No se pudieron cargar los reportes. Verifica la conexión con la API.');
      }
    });
  }

  clearFilter(): void {
    this.statusFilter = '';
    this.load();
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Abierto': return 'ss-badge-red';
      case 'EnRevision': return 'ss-badge-orange';
      case 'Resuelto': return 'ss-badge-teal';
      case 'Descartado': return 'ss-badge-gray';
      default: return 'ss-badge';
    }
  }

  target(r: Report): string {
    if (r.reported) return `Usuario: ${r.reported}`;
    if (r.exchangeId) return `Intercambio #${r.exchangeId}`;
    return '—';
  }

  // ----- Gestión -----
  openManage(r: Report): void {
    this.clearMessages();
    this.managing = r;
    this.newStatus = r.status;
    this.resolution = r.resolution || '';
  }

  cancelManage(): void {
    this.managing = null;
    this.clearMessages();
  }

  saveManage(): void {
    if (!this.managing) return;
    this.clearMessages();

    if ((this.newStatus === 'Resuelto' || this.newStatus === 'Descartado') && !this.resolution.trim()) {
      this.errorMessage.set('Indica una resolución al resolver o descartar el reporte.');
      return;
    }

    this.reportService.updateStatus(this.managing.id, {
      status: this.newStatus,
      resolution: this.resolution.trim()
    }).subscribe({
      next: () => {
        this.successMessage.set(`Reporte #${this.managing!.id} actualizado a ${this.newStatus}.`);
        this.managing = null;
        this.load();
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'No se pudo actualizar el reporte.')
    });
  }

  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }
}
