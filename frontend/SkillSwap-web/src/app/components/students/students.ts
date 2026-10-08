import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Student, StudentService } from '../../services/student';
import { AdminService, ROLES, ROLE_LABELS } from '../../services/admin';
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
  roleLabels = ROLE_LABELS;

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

  // Permisos por rol (coinciden con lo que la API autoriza con el token)
  isAdmin = false;
  isModerador = false;

  canCreate = false;       // Administrador
  canEdit = false;         // Administrador, Moderador
  canDelete = false;       // Administrador
  canDeactivate = false;   // Administrador, Moderador (suspender)
  canActivate = false;     // Administrador, Moderador
  canChangeRole = false;   // Administrador
  canResetPassword = false; // Administrador

  constructor(
    private studentService: StudentService,
    private adminService: AdminService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const role = this.authService.getUserRole();
    this.isAdmin = role === 'Administrador';
    this.isModerador = role === 'Moderador';

    this.canCreate = this.isAdmin;
    this.canEdit = this.isAdmin || this.isModerador;
    this.canDelete = this.isAdmin;
    this.canDeactivate = this.isAdmin || this.isModerador;
    this.canActivate = this.isAdmin || this.isModerador;
    this.canChangeRole = this.isAdmin;
    this.canResetPassword = this.isAdmin;

    this.loadStudents();
    this.loadRoles();
  }

  loadStudents(): void {
    this.loading.set(true);
    this.studentService.getAll({ name: this.nameFilter, skillName: this.skillFilter }).subscribe({
      next: (data) => {
        this.students.set(data);
        this.loading.set(false);
      },
      error: (err) => this.handleError(err, 'No se pudieron cargar los usuarios.')
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
    return this.userRoles()[userId] || 'Estudiante';
  }

  roleLabelOf(userId: string): string {
    return this.roleLabels[this.roleOf(userId)] || this.roleOf(userId);
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
      isActive: student.isActive,
      roleName: 'Estudiante'
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

    let call;
    if (this.editingId) {
      call = this.studentService.update(this.editingId, {
        username: f.username, email: f.email, firstName: f.firstName,
        lastName: f.lastName, bio: f.bio, isActive: f.isActive
      });
    } else {
      // Solo el Administrador crea cuentas, y puede elegir el rol
      call = this.adminService.createUser({
        username: f.username, email: f.email, password: f.password,
        firstName: f.firstName, lastName: f.lastName, roleName: f.roleName, bio: f.bio
      });
    }

    call.subscribe({
      next: () => {
        this.successMessage.set(this.editingId ? 'Usuario actualizado correctamente.' : 'Cuenta creada correctamente.');
        this.resetForm(false);
        this.loadStudents();
        this.loadRoles();
      },
      error: (err) => this.handleError(err, 'No se pudo guardar el usuario.')
    });
  }

  delete(student: Student): void {
    if (!confirm(`¿Eliminar al usuario "${student.fullName}"? También se eliminarán sus ofertas y solicitudes.`)) return;
    this.clearMessages();

    this.studentService.delete(student.id).subscribe({
      next: () => {
        this.successMessage.set('Usuario eliminado correctamente.');
        this.loadStudents();
      },
      error: (err) => this.handleError(err, 'No se pudo eliminar el usuario.')
    });
  }

  // Activar / suspender la cuenta (Administrador y Moderador)
  toggleStatus(student: Student): void {
    this.clearMessages();
    const newState = !student.isActive;
    if (!newState && !this.canDeactivate) {
      this.errorMessage.set('Tu perfil solo puede activar cuentas, no desactivarlas.');
      return;
    }
    this.adminService.setUserStatus(student.userId, newState).subscribe({
      next: (res) => {
        this.successMessage.set(res.message);
        this.loadStudents();
      },
      error: (err) => this.handleError(err, 'No se pudo cambiar el estado de la cuenta.')
    });
  }

  // Restablecer la contraseña (solo Administrador). La nueva contraseña se guarda cifrada.
  resetPassword(student: Student): void {
    this.clearMessages();
    const nueva = prompt(`Nueva contraseña para "${student.fullName}" (mínimo 6 caracteres):`);
    if (nueva === null) return;
    if (nueva.length < 6) {
      this.errorMessage.set('La nueva contraseña debe tener al menos 6 caracteres.');
      return;
    }
    this.adminService.resetPassword(student.userId, nueva).subscribe({
      next: (res) => this.successMessage.set(res.message),
      error: (err) => this.handleError(err, 'No se pudo restablecer la contraseña.')
    });
  }

  changeRole(student: Student, roleName: string): void {
    this.clearMessages();
    this.adminService.updateUserRole(student.userId, roleName).subscribe({
      next: (res) => {
        this.userRoles.update(map => ({ ...map, [student.userId]: res.role }));
        this.successMessage.set(`Rol de ${student.fullName} actualizado a ${this.roleLabels[res.role] || res.role}.`);
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
    return { username: '', email: '', password: '', firstName: '', lastName: '', bio: '', isActive: true, roleName: 'Estudiante' };
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
