import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SkillRequest, RequestService } from '../../services/request';
import { Skill, SkillService } from '../../services/skill';
import { Student, StudentService } from '../../services/student';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-requests',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './requests.html',
  styleUrls: ['./requests.css']
})
export class RequestsComponent implements OnInit {
  // Estado del backend (asíncrono): signals
  requests = signal<SkillRequest[]>([]);
  skills = signal<Skill[]>([]);
  categories = signal<string[]>([]);
  students = signal<Student[]>([]);
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  // Filtros y formulario: propiedades normales ([(ngModel)])
  skillNameFilter = '';
  categoryFilter = '';
  studentFilter: number | null = null;
  onlyMine = true;
  includeInactive = false;

  showForm = false;
  editingId: number | null = null;
  form = this.emptyForm();

  isAdmin = false;
  myStudentId: number | null = null;

  constructor(
    private requestService: RequestService,
    private skillService: SkillService,
    private studentService: StudentService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.myStudentId = this.authService.getStudentId();
    this.onlyMine = !this.isAdmin && !!this.myStudentId;

    this.skillService.getAll().subscribe({ next: (data) => this.skills.set(data) });
    this.skillService.getCategories().subscribe({ next: (data) => this.categories.set(data) });
    if (this.isAdmin) {
      this.studentService.getAll().subscribe({ next: (data) => this.students.set(data) });
    }

    this.loadRequests();
  }

  loadRequests(): void {
    this.loading.set(true);
    const studentId = this.onlyMine ? this.myStudentId : this.studentFilter;

    this.requestService.getAll({
      skillName: this.skillNameFilter,
      category: this.categoryFilter,
      studentId,
      includeInactive: this.includeInactive
    }).subscribe({
      next: (data) => {
        this.requests.set(data);
        this.loading.set(false);
      },
      error: (err) => this.handleError(err, 'No se pudieron cargar las solicitudes.')
    });
  }

  clearFilters(): void {
    this.skillNameFilter = '';
    this.categoryFilter = '';
    this.studentFilter = null;
    this.includeInactive = false;
    this.loadRequests();
  }

  // El estudiante solo puede modificar sus propias solicitudes
  canManage(request: SkillRequest): boolean {
    return this.isAdmin || request.studentId === this.myStudentId;
  }

  openCreate(): void {
    this.resetForm();
    this.form.studentId = this.myStudentId;
    this.showForm = true;
  }

  openEdit(request: SkillRequest): void {
    this.clearMessages();
    this.editingId = request.id;
    this.form = {
      studentId: request.studentId,
      skillId: request.skillId,
      notes: request.notes,
      isActive: request.isActive
    };
    this.showForm = true;
  }

  save(): void {
    this.clearMessages();
    const f = this.form;
    if (!f.studentId || !f.skillId) {
      this.errorMessage.set('Selecciona el estudiante y la habilidad.');
      return;
    }

    const data = { studentId: f.studentId, skillId: f.skillId, notes: f.notes, isActive: f.isActive };
    const call = this.editingId
      ? this.requestService.update(this.editingId, { id: this.editingId, ...data })
      : this.requestService.create(data);

    call.subscribe({
      next: () => {
        this.successMessage.set(this.editingId ? 'Solicitud actualizada correctamente.' : 'Solicitud registrada correctamente.');
        this.resetForm(false);
        this.loadRequests();
      },
      error: (err) => this.handleError(err, 'No se pudo guardar la solicitud.')
    });
  }

  delete(request: SkillRequest): void {
    if (!confirm(`¿Eliminar la solicitud de "${request.skillName}"?`)) return;
    this.clearMessages();

    this.requestService.delete(request.id).subscribe({
      next: () => {
        this.successMessage.set('Solicitud eliminada correctamente.');
        this.loadRequests();
      },
      error: (err) => this.handleError(err, 'No se pudo eliminar la solicitud.')
    });
  }

  resetForm(clearMessages = true): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.emptyForm();
    if (clearMessages) this.clearMessages();
  }

  private emptyForm() {
    return { studentId: null as number | null, skillId: null as number | null, notes: '', isActive: true };
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
