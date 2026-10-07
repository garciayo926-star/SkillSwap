import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Exchange, ExchangeService, EXCHANGE_STATUSES } from '../../services/exchange';
import { Student, StudentService } from '../../services/student';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-exchanges',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './exchanges.html',
  styleUrls: ['./exchanges.css']
})
export class ExchangesComponent implements OnInit {
  statuses = EXCHANGE_STATUSES;

  // Estado del backend (asíncrono): signals
  exchanges = signal<Exchange[]>([]);
  students = signal<Student[]>([]);
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  // Filtros y formulario: propiedades normales ([(ngModel)])
  statusFilter = '';
  studentFilter: number | null = null;

  completing: Exchange | null = null;
  rating = 5;
  feedback = '';

  isAdmin = false;
  myStudentId: number | null = null;

  constructor(
    private exchangeService: ExchangeService,
    private studentService: StudentService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.myStudentId = this.authService.getStudentId();

    if (this.isAdmin) {
      this.studentService.getAll().subscribe({ next: (data) => this.students.set(data) });
    }

    this.loadExchanges();
  }

  loadExchanges(): void {
    this.loading.set(true);
    // El estudiante solo ve los intercambios en los que participa
    const studentId = this.isAdmin ? this.studentFilter : this.myStudentId;

    this.exchangeService.getAll({ studentId, status: this.statusFilter }).subscribe({
      next: (data) => {
        this.exchanges.set(data);
        this.loading.set(false);
      },
      error: (err) => this.handleError(err, 'No se pudieron cargar los intercambios.')
    });
  }

  clearFilters(): void {
    this.statusFilter = '';
    this.studentFilter = null;
    this.loadExchanges();
  }

  // ----- Reglas de acciones según estado y participante -----
  isReceiver(e: Exchange): boolean {
    return e.receiverStudentId === this.myStudentId;
  }

  isInitiator(e: Exchange): boolean {
    return e.initiatorStudentId === this.myStudentId;
  }

  isParticipant(e: Exchange): boolean {
    return this.isReceiver(e) || this.isInitiator(e);
  }

  canRespond(e: Exchange): boolean {
    return e.status === 'Pendiente' && (this.isReceiver(e) || this.isAdmin);
  }

  canCancel(e: Exchange): boolean {
    if (e.status === 'Pendiente') return this.isInitiator(e) || this.isAdmin;
    if (e.status === 'Aceptado') return this.isParticipant(e) || this.isAdmin;
    return false;
  }

  canComplete(e: Exchange): boolean {
    return e.status === 'Aceptado' && (this.isParticipant(e) || this.isAdmin);
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Pendiente': return 'ss-badge-orange';
      case 'Aceptado': return 'ss-badge-blue';
      case 'Completado': return 'ss-badge-teal';
      case 'Rechazado': return 'ss-badge-red';
      default: return 'ss-badge-gray';
    }
  }

  stars(rating: number): string {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }

  // ----- Acciones -----
  changeStatus(e: Exchange, status: string): void {
    const labels: Record<string, string> = { Aceptado: 'aceptar', Rechazado: 'rechazar', Cancelado: 'cancelar' };
    if (!confirm(`¿Deseas ${labels[status] || 'actualizar'} este intercambio?`)) return;
    this.clearMessages();

    this.exchangeService.updateStatus(e.id, { status }).subscribe({
      next: () => {
        this.successMessage.set(`Intercambio ${status.toLowerCase()} correctamente.`);
        this.loadExchanges();
      },
      error: (err) => this.handleError(err, 'No se pudo actualizar el intercambio.')
    });
  }

  openComplete(e: Exchange): void {
    this.clearMessages();
    this.completing = e;
    this.rating = 5;
    this.feedback = '';
  }

  confirmComplete(): void {
    if (!this.completing) return;
    this.clearMessages();

    this.exchangeService.updateStatus(this.completing.id, {
      status: 'Completado',
      rating: Number(this.rating),
      feedback: this.feedback
    }).subscribe({
      next: () => {
        this.successMessage.set('Intercambio completado y calificado. ¡Gracias por compartir tus conocimientos!');
        this.completing = null;
        this.loadExchanges();
      },
      error: (err) => this.handleError(err, 'No se pudo completar el intercambio.')
    });
  }

  delete(e: Exchange): void {
    if (!confirm('¿Eliminar definitivamente este intercambio?')) return;
    this.clearMessages();

    this.exchangeService.delete(e.id).subscribe({
      next: () => {
        this.successMessage.set('Intercambio eliminado correctamente.');
        this.loadExchanges();
      },
      error: (err) => this.handleError(err, 'No se pudo eliminar el intercambio.')
    });
  }

  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private handleError(err: any, fallback: string): void {
    this.loading.set(false);
    this.errorMessage.set(err.error?.message || fallback);
  }
}
