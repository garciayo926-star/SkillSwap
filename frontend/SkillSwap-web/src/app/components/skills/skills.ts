import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Skill, SkillService } from '../../services/skill';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-skills',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './skills.html',
  styleUrls: ['./skills.css']
})
export class SkillsComponent implements OnInit {
  // Estado que llega del backend (asíncrono): signals para refrescar la vista
  skills = signal<Skill[]>([]);
  categories = signal<string[]>([]);
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  // Filtros y formulario: propiedades normales ligadas con [(ngModel)]
  searchFilter = '';
  categoryFilter = '';

  showForm = false;
  editingId: number | null = null;
  formName = '';
  formCategory = '';

  isAdmin = false;

  constructor(
    private skillService: SkillService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.loadCategories();
    this.loadSkills();
  }

  loadSkills(): void {
    this.loading.set(true);
    this.skillService.getAll({ search: this.searchFilter, category: this.categoryFilter }).subscribe({
      next: (data) => {
        this.skills.set(data);
        this.loading.set(false);
      },
      error: (err) => this.handleError(err, 'No se pudieron cargar las habilidades.')
    });
  }

  loadCategories(): void {
    this.skillService.getCategories().subscribe({
      next: (data) => this.categories.set(data)
    });
  }

  clearFilters(): void {
    this.searchFilter = '';
    this.categoryFilter = '';
    this.loadSkills();
  }

  openCreate(): void {
    this.resetForm();
    this.showForm = true;
  }

  openEdit(skill: Skill): void {
    this.clearMessages();
    this.editingId = skill.id;
    this.formName = skill.name;
    this.formCategory = skill.category;
    this.showForm = true;
  }

  save(): void {
    this.clearMessages();
    if (!this.formName.trim() || !this.formCategory.trim()) {
      this.errorMessage.set('El nombre y la categoría son obligatorios.');
      return;
    }

    const data = { name: this.formName.trim(), category: this.formCategory.trim() };
    const call = this.editingId
      ? this.skillService.update(this.editingId, { id: this.editingId, ...data })
      : this.skillService.create(data);

    call.subscribe({
      next: () => {
        this.successMessage.set(this.editingId ? 'Habilidad actualizada correctamente.' : 'Habilidad creada correctamente.');
        this.resetForm(false);
        this.loadCategories();
        this.loadSkills();
      },
      error: (err) => this.handleError(err, 'No se pudo guardar la habilidad.')
    });
  }

  delete(skill: Skill): void {
    if (!confirm(`¿Eliminar la habilidad "${skill.name}"?`)) return;
    this.clearMessages();

    this.skillService.delete(skill.id).subscribe({
      next: () => {
        this.successMessage.set('Habilidad eliminada correctamente.');
        this.loadCategories();
        this.loadSkills();
      },
      error: (err) => this.handleError(err, 'No se pudo eliminar la habilidad.')
    });
  }

  resetForm(clearMessages = true): void {
    this.showForm = false;
    this.editingId = null;
    this.formName = '';
    this.formCategory = '';
    if (clearMessages) this.clearMessages();
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
