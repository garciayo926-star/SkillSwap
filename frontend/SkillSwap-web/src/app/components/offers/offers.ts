import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Offer, OfferService } from '../../services/offer';
import { Skill, SkillService } from '../../services/skill';
import { Student, StudentService } from '../../services/student';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-offers',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './offers.html',
  styleUrls: ['./offers.css']
})
export class OffersComponent implements OnInit {
  // Estado del backend (asíncrono): signals
  offers = signal<Offer[]>([]);
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
    private offerService: OfferService,
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

    this.loadOffers();
  }

  loadOffers(): void {
    this.loading.set(true);
    const studentId = this.onlyMine ? this.myStudentId : this.studentFilter;

    this.offerService.getAll({
      skillName: this.skillNameFilter,
      category: this.categoryFilter,
      studentId,
      includeInactive: this.includeInactive
    }).subscribe({
      next: (data) => {
        this.offers.set(data);
        this.loading.set(false);
      },
      error: (err) => this.handleError(err, 'No se pudieron cargar las ofertas.')
    });
  }

  clearFilters(): void {
    this.skillNameFilter = '';
    this.categoryFilter = '';
    this.studentFilter = null;
    this.includeInactive = false;
    this.loadOffers();
  }

  // El estudiante solo puede modificar sus propias ofertas
  canManage(offer: Offer): boolean {
    return this.isAdmin || offer.studentId === this.myStudentId;
  }

  openCreate(): void {
    this.resetForm();
    this.form.studentId = this.myStudentId;
    this.showForm = true;
  }

  openEdit(offer: Offer): void {
    this.clearMessages();
    this.editingId = offer.id;
    this.form = {
      studentId: offer.studentId,
      skillId: offer.skillId,
      description: offer.description,
      isActive: offer.isActive
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

    const data = { studentId: f.studentId, skillId: f.skillId, description: f.description, isActive: f.isActive };
    const call = this.editingId
      ? this.offerService.update(this.editingId, { id: this.editingId, ...data })
      : this.offerService.create(data);

    call.subscribe({
      next: () => {
        this.successMessage.set(this.editingId ? 'Oferta actualizada correctamente.' : 'Oferta publicada correctamente.');
        this.resetForm(false);
        this.loadOffers();
      },
      error: (err) => this.handleError(err, 'No se pudo guardar la oferta.')
    });
  }

  delete(offer: Offer): void {
    if (!confirm(`¿Eliminar la oferta de "${offer.skillName}"?`)) return;
    this.clearMessages();

    this.offerService.delete(offer.id).subscribe({
      next: () => {
        this.successMessage.set('Oferta eliminada correctamente.');
        this.loadOffers();
      },
      error: (err) => this.handleError(err, 'No se pudo eliminar la oferta.')
    });
  }

  resetForm(clearMessages = true): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.emptyForm();
    if (clearMessages) this.clearMessages();
  }

  private emptyForm() {
    return { studentId: null as number | null, skillId: null as number | null, description: '', isActive: true };
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
