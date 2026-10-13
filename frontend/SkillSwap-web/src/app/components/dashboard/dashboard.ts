import { Component, ChangeDetectionStrategy, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth';
import { AdminService, Analytics, SystemHealth, ROLE_LABELS } from '../../services/admin';
import { StudentDetail, StudentService } from '../../services/student';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  username: string = '';
  userRole: string = '';
  roleLabel: string = '';

  isAdmin = false;
  isModerador = false;
  isStudent = false;

  // Solo los perfiles con visión global ven las métricas administrativas.
  // El Estudiante nunca ve el desglose de usuarios ni los indicadores del sistema.
  canSeeGlobal = false;

  // Métricas reales obtenidas desde la API de SkillSwap (asíncronas): signals
  analytics = signal<Analytics | null>(null);
  health = signal<SystemHealth | null>(null);
  myProfile = signal<StudentDetail | null>(null);
  errorMessage = signal('');

  // Indicadores derivados: computed() se recalcula cuando cambian los signals
  activityRate = computed(() => {
    const a = this.analytics();
    return a && a.totalStudents > 0 ? Math.round((a.studentsWithActivity / a.totalStudents) * 100) : 0;
  });

  inactivityRate = computed(() => {
    const a = this.analytics();
    return a && a.totalStudents > 0 ? 100 - this.activityRate() : 0;
  });

  pendingExchanges = computed(() => this.exchangesWithStatus('Pendiente'));

  myActiveOffers = computed(() => this.myProfile()?.offers.filter(o => o.isActive).length ?? 0);

  myActiveRequests = computed(() => this.myProfile()?.requests.filter(r => r.isActive).length ?? 0);

  // --- Buscador / filtro de habilidades (interactivo, en tiempo real) ---
  skillFilter = signal('');

  // Listas filtradas según lo que el usuario escribe en el buscador
  filteredOfferedSkills = computed(() => {
    const q = this.skillFilter().toLowerCase().trim();
    const list = this.analytics()?.topOfferedSkills ?? [];
    return q ? list.filter(s => s.name.toLowerCase().includes(q)) : list;
  });

  filteredRequestedSkills = computed(() => {
    const q = this.skillFilter().toLowerCase().trim();
    const list = this.analytics()?.topRequestedSkills ?? [];
    return q ? list.filter(s => s.name.toLowerCase().includes(q)) : list;
  });

  // Valor máximo para escalar la altura de las barras del gráfico
  maxOfferedCount = computed(() => {
    const counts = (this.analytics()?.topOfferedSkills ?? []).map(s => s.count);
    return counts.length ? Math.max(...counts) : 1;
  });

  constructor(
    private router: Router,
    private authService: AuthService,
    private adminService: AdminService,
    private studentService: StudentService
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername() || 'Usuario';
    this.userRole = this.authService.getUserRole() || 'Estudiante';
    this.roleLabel = ROLE_LABELS[this.userRole] || this.userRole;

    this.isAdmin = this.userRole === 'Administrador';
    this.isModerador = this.userRole === 'Moderador';
    this.isStudent = this.authService.isStudent();
    this.canSeeGlobal = this.authService.canSeeGlobalDashboard();

    this.loadData();
  }

  loadData(): void {
    // Las métricas globales solo se piden si el rol tiene permiso (si no, la API responde 401).
    if (this.canSeeGlobal) {
      this.adminService.getAnalytics().subscribe({
        next: (data) => this.analytics.set(data),
        error: (err: HttpErrorResponse) => this.errorMessage.set(this.describeError(err, 'métricas del dashboard'))
      });
    }

    // El estado del sistema es responsabilidad del Administrador.
    if (this.isAdmin) {
      this.adminService.getSystemHealth().subscribe({
        next: (data) => this.health.set(data),
        error: (err: HttpErrorResponse) => this.errorMessage.set(this.describeError(err, 'estado del sistema'))
      });
    }

    const studentId = this.authService.getStudentId();
    if (studentId) {
      this.studentService.getById(studentId).subscribe({
        next: (data) => this.myProfile.set(data),
        error: (err: HttpErrorResponse) => this.errorMessage.set(this.describeError(err, 'tu perfil'))
      });
    }
  }

  // Traduce un error HTTP a un mensaje claro y referenciado según su causa real.
  // 0 = la API no respondió (apagada o sin red); 401/403 = permisos/sesión;
  // 500 = error interno (típicamente la base de datos no está conectada).
  private describeError(err: HttpErrorResponse, contexto: string): string {
    const backendMsg = err.error?.message ? ` Detalle: ${err.error.message}` : '';
    switch (err.status) {
      case 0:
        return `No se pudo conectar con la API de SkillSwap (http://localhost:5066). ` +
               `Verifica que el backend .NET esté encendido. [Error de red / CORS]`;
      case 401:
        return 'Tu sesión expiró o no tienes autorización. Inicia sesión nuevamente. [HTTP 401]';
      case 403:
        return `No tienes permisos para ver las ${contexto}. [HTTP 403]`;
      case 404:
        return `El recurso de ${contexto} no existe en la API. [HTTP 404]`;
      case 500:
        return `Error interno del servidor al cargar ${contexto}. ` +
               `Posible fallo de conexión con la base de datos PostgreSQL.${backendMsg} [HTTP 500]`;
      default:
        return `Error al cargar ${contexto}.${backendMsg} [HTTP ${err.status}]`;
    }
  }

  exchangesWithStatus(status: string): number {
    return this.analytics()?.exchangesByStatus.find(e => e.status === status)?.count ?? 0;
  }

  // Ancho proporcional de cada barra respecto al total
  barWidth(count: number, total: number): string {
    return total > 0 ? `${Math.round((count / total) * 100)}%` : '0%';
  }

  // Altura proporcional de cada barra del gráfico (respecto a la más alta)
  barHeight(count: number): string {
    const max = this.maxOfferedCount();
    return max > 0 ? `${Math.round((count / max) * 100)}%` : '0%';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
