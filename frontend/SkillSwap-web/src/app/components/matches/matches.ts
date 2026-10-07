import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ExchangeService, Match } from '../../services/exchange';
import { Student, StudentService } from '../../services/student';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-matches',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './matches.html',
  styleUrls: ['./matches.css']
})
export class MatchesComponent implements OnInit {
  // Estado del backend (asíncrono): signals
  matches = signal<Match[]>([]);
  students = signal<Student[]>([]);
  loading = signal(false);
  searched = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  // Estudiante para el que se buscan coincidencias
  selectedStudentId: number | null = null;
  includePartial = false;

  // Habilidades elegidas para proponer el intercambio (por estudiante coincidente)
  selectedOffered: Record<number, number | null> = {};
  selectedRequested: Record<number, number | null> = {};

  isAdmin = false;

  constructor(
    private exchangeService: ExchangeService,
    private studentService: StudentService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.selectedStudentId = this.authService.getStudentId();

    if (this.isAdmin) {
      this.studentService.getAll().subscribe({ next: (data) => this.students.set(data) });
    }

    if (this.selectedStudentId) {
      this.loadMatches();
    }
  }

  loadMatches(): void {
    this.clearMessages();
    if (!this.selectedStudentId) {
      this.matches.set([]);
      return;
    }

    this.loading.set(true);
    this.exchangeService.getMatches(this.selectedStudentId, this.includePartial).subscribe({
      next: (data) => {
        // Preselecciona la primera habilidad de cada lado
        const offered: Record<number, number | null> = {};
        const requested: Record<number, number | null> = {};
        data.forEach(m => {
          offered[m.studentId] = m.wantedSkillsFromMe[0]?.skillId ?? null;
          requested[m.studentId] = m.offeredSkillsToMe[0]?.skillId ?? null;
        });
        this.selectedOffered = offered;
        this.selectedRequested = requested;
        this.matches.set(data);
        this.loading.set(false);
        this.searched.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'No se pudieron cargar las coincidencias.');
      }
    });
  }

  propose(match: Match): void {
    this.clearMessages();
    if (!this.selectedStudentId) return;

    const offeredSkillId = this.selectedOffered[match.studentId] ?? null;
    const requestedSkillId = this.selectedRequested[match.studentId] ?? null;

    if (!offeredSkillId && !requestedSkillId) {
      this.errorMessage.set('Selecciona al menos una habilidad para el intercambio.');
      return;
    }

    this.exchangeService.propose({
      initiatorStudentId: this.selectedStudentId,
      receiverStudentId: match.studentId,
      offeredSkillId,
      requestedSkillId
    }).subscribe({
      next: () => {
        this.successMessage.set(`Intercambio propuesto a ${match.fullName}. Puedes seguirlo en la sección Intercambios.`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'No se pudo proponer el intercambio.');
      }
    });
  }

  goToExchanges(): void {
    this.router.navigate(['/exchanges']);
  }

  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }
}
