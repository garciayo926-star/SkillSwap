import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Student, StudentService } from '../../services/student';
import { AdminService, ROLES } from '../../services/admin';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-students',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './students.html',
  styleUrls: ['./students.css']
})
export class StudentsComponent implements OnInit {
  roles = ROLES;

  // Estado del backend (asíncrono): signals
  students = signal<Student[]>([]);
  userRoles = signal<Record<string, string>>({});
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  // Filtros y formulario: propiedades normales ([(ngModel)])
  nameFilter = '';
  skillFilter = '';

  showForm = false;
  editingId: number | null = null;
  form = this.emptyForm();

  isMaster = false;

  constructor(
    private studentService: StudentService,
    private adminService: AdminService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isMaster = this.authService.isMaster();
    this.loadStudents();
    if (this.isMaster) this.loadRoles();
  }

  loadStudents(): void {
    this.loading.set(true);
    this.studentService.getAll({ name: this.nameFilter, skillName: this.skillFilter }).subscribe({
      next: (data) => {
        this.students.set(data);
        this.loading.set(false);
      },
      error: (err) => this.handleError(err, 'No se pudieron cargar los estudiantes.')
    });
  }

  loadRoles(): void {
    this.adminService.getUsers().subscribe({
      next: (users) => {
        const map: Record<string, string> = {};
        users.forEach(u => map[u.id] = u.role);
        this.userRoles.set(map);
      }
    });
  }

  roleOf(userId: string): string {
    return this.userRoles()[userId] || 'Student';
  }

  clearFilters(): void {
    this.nameFilter = '';
    this.skillFilter = '';
    this.loadStudents();
  }

  openCreate(): void {
    this.resetForm();
    this.showForm = true;
  }

  openEdit(student: Student): void {
    this.clearMessages();
    this.editingId = student.id;
    this.form = {
      username: student.username,
      email: student.email,
      password: '',
      firstName: student.firstName,
      lastName: student.lastName,
      bio: student.bio,
      isActive: student.isActive
    };
    this.showForm = true;
  }

  save(): void {
    this.clearMessages();
    const f = this.form;
    if (!f.username || !f.email || !f.firstName || !f.lastName || (!this.editingId && !f.password)) {
      this.errorMessage.set('Completa todos los campos obligatorios.');
      return;
    }

    const call = this.editingId
      ? this.studentService.update(this.editingId, {
          username: f.username, email: f.email, firstName: f.firstName,
          lastName: f.lastName, bio: f.bio, isActive: f.isActive
        })
      : this.studentService.create({
          username: f.username, email: f.email, password: f.password,
          firstName: f.firstName, lastName: f.lastName, bio: f.bio
        });

    call.subscribe({
      next: () => {
        this.successMessage.set(this.editingId ? 'Estudiante actualizado correctamente.' : 'Estudiante creado correctamente.');
        this.resetForm(false);
        this.loadStudents();
        if (this.isMaster) this.loadRoles();
      },
      error: (err) => this.handleError(err, 'No se pudo guardar el estudiante.')
    });
  }

  delete(student: Student): void {
    if (!confirm(`¿Eliminar al estudiante "${student.fullName}"? También se eliminarán sus ofertas y solicitudes.`)) return;
    this.clearMessages();

    this.studentService.delete(student.id).subscribe({
      next: () => {
        this.successMessage.set('Estudiante eliminado correctamente.');
        this.loadStudents();
      },
      error: (err) => this.handleError(err, 'No se pudo eliminar el estudiante.')
    });
  }

  changeRole(student: Student, roleName: string): void {
    this.clearMessages();
    this.adminService.updateUserRole(student.userId, roleName).subscribe({
      next: (res) => {
        this.userRoles.update(map => ({ ...map, [student.userId]: res.role }));
        this.successMessage.set(`Rol de ${student.fullName} actualizado a ${res.role}.`);
      },
      error: (err) => this.handleError(err, 'No se pudo actualizar el rol.')
    });
  }

  resetForm(clearMessages = true): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.emptyForm();
    if (clearMessages) this.clearMessages();
  }

  private emptyForm() {
    return { username: '', email: '', password: '', firstName: '', lastName: '', bio: '', isActive: true };
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
