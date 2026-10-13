import { Component, ChangeDetectionStrategy, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Exchange, ExchangeService } from '../../services/exchange';
import { AuthService } from '../../services/auth';

// Un día del calendario con sus sesiones agendadas
interface CalendarDay {
  key: string;        // yyyy-mm-dd (para ordenar)
  label: string;      // "lunes, 13 de octubre de 2026"
  isToday: boolean;
  isPast: boolean;
  sessions: Exchange[];
}

@Component({
  selector: 'app-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './calendar.html',
  styleUrls: ['./calendar.css']
})
export class CalendarComponent implements OnInit {
  exchanges = signal<Exchange[]>([]);
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  isAdmin = false;
  myStudentId: number | null = null;

  // Formulario para agendar/editar una sesión
  scheduling: Exchange | null = null;
  formStart = '';     // datetime-local
  formEnd = '';       // datetime-local
  formRoom = '';
  formMeetingUrl = '';

  constructor(
    private exchangeService: ExchangeService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.myStudentId = this.authService.getStudentId();
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const studentId = this.isAdmin ? null : this.myStudentId;
    // Solo intercambios aceptados o completados pueden tener clases agendadas
    this.exchangeService.getAll({ studentId }).subscribe({
      next: (data) => {
        this.exchanges.set(data.filter(e => e.status === 'Aceptado' || e.status === 'Completado'));
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'No se pudo cargar el calendario. Verifica la conexión con la API.');
      }
    });
  }

  // Sesiones ya agendadas (con fecha), agrupadas por día y ordenadas
  days = computed<CalendarDay[]>(() => {
    const withDate = this.exchanges().filter(e => e.sessionDate);
    const groups = new Map<string, Exchange[]>();

    for (const e of withDate) {
      const d = new Date(e.sessionDate!);
      const key = d.toISOString().slice(0, 10);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(e);
    }

    const todayKey = new Date().toISOString().slice(0, 10);

    return Array.from(groups.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([key, sessions]) => ({
        key,
        label: new Date(key + 'T00:00:00').toLocaleDateString('es-ES', {
          weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
        }),
        isToday: key === todayKey,
        isPast: key < todayKey,
        sessions: sessions.sort((x, y) => (x.sessionDate! < y.sessionDate! ? -1 : 1))
      }));
  });

  // Intercambios aceptados aún SIN agendar (para invitar a programar la clase)
  pendingToSchedule = computed(() =>
    this.exchanges().filter(e => e.status === 'Aceptado' && !e.sessionDate)
  );

  // El nombre de la "materia" = habilidad que se enseña en el intercambio
  subject(e: Exchange): string {
    return e.offeredSkillName || e.requestedSkillName || 'Intercambio de habilidades';
  }

  timeRange(e: Exchange): string {
    if (!e.sessionDate) return '';
    const start = new Date(e.sessionDate).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
    if (!e.sessionEndDate) return start;
    const end = new Date(e.sessionEndDate).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
    return `${start} – ${end}`;
  }

  canManage(e: Exchange): boolean {
    return this.isAdmin || e.initiatorStudentId === this.myStudentId || e.receiverStudentId === this.myStudentId;
  }

  // ----- Agendar / editar -----
  openSchedule(e: Exchange): void {
    this.clearMessages();
    this.scheduling = e;
    this.formStart = this.toLocalInput(e.sessionDate);
    this.formEnd = this.toLocalInput(e.sessionEndDate);
    this.formRoom = e.room || '';
    this.formMeetingUrl = e.meetingUrl || '';
  }

  cancelSchedule(): void {
    this.scheduling = null;
    this.clearMessages();
  }

  saveSchedule(): void {
    if (!this.scheduling) return;
    this.clearMessages();

    if (!this.formStart) {
      this.errorMessage.set('Indica al menos la fecha y hora de inicio de la clase.');
      return;
    }
    if (this.formEnd && this.formEnd <= this.formStart) {
      this.errorMessage.set('La hora de fin debe ser posterior a la de inicio.');
      return;
    }

    this.exchangeService.schedule(this.scheduling.id, {
      sessionDate: this.formStart || null,
      sessionEndDate: this.formEnd || null,
      room: this.formRoom.trim(),
      meetingUrl: this.formMeetingUrl.trim()
    }).subscribe({
      next: () => {
        this.successMessage.set('Clase agendada correctamente.');
        this.scheduling = null;
        this.load();
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'No se pudo agendar la clase.')
    });
  }

  // Convierte una fecha ISO del backend a formato datetime-local para el input
  private toLocalInput(iso: string | null): string {
    if (!iso) return '';
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }
}
