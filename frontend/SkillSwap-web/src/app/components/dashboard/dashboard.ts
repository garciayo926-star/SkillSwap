import { Component, ChangeDetectionStrategy, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
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
        error: () => this.errorMessage.set('No se pudo conectar con la API de SkillSwap (puerto 5066).')
      });
    }

    // El estado del sistema es responsabilidad del Administrador.
    if (this.isAdmin) {
      this.adminService.getSystemHealth().subscribe({
        next: (data) => this.health.set(data)
      });
    }

    const studentId = this.authService.getStudentId();
    if (studentId) {
      this.studentService.getById(studentId).subscribe({
        next: (data) => this.myProfile.set(data)
      });
    }
  }

  exchangesWithStatus(status: string): number {
    return this.analytics()?.exchangesByStatus.find(e => e.status === status)?.count ?? 0;
  }

  // Ancho proporcional de cada barra respecto al total
  barWidth(count: number, total: number): string {
    return total > 0 ? `${Math.round((count / total) * 100)}%` : '0%';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
