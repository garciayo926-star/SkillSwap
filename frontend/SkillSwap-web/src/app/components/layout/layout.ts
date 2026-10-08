import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth';
import { ROLE_LABELS } from '../../services/admin';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  roles?: string[];
}

@Component({
  selector: 'app-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.html',
  styleUrls: ['./layout.css']
})
export class LayoutComponent implements OnInit {
  username = '';
  userRole = '';
  roleLabel = '';

  // Menú principal; los elementos con "roles" solo se muestran a esos roles.
  // "Usuarios/Estudiantes" queda para administración (no para el Estudiante).
  private navItems: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/students', label: 'Usuarios', icon: '🎓', roles: ['Administrador', 'Moderador'] },
    { path: '/skills', label: 'Habilidades', icon: '🧩' },
    { path: '/offers', label: 'Ofertas', icon: '🎁' },
    { path: '/requests', label: 'Solicitudes', icon: '📚' },
    { path: '/matches', label: 'Coincidencias', icon: '🔗' },
    { path: '/exchanges', label: 'Intercambios', icon: '🔄' }
  ];

  visibleItems: NavItem[] = [];

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername() || 'Usuario';
    this.userRole = this.authService.getUserRole();
    this.roleLabel = ROLE_LABELS[this.userRole] || this.userRole;
    this.visibleItems = this.navItems.filter(item => !item.roles || item.roles.includes(this.userRole));
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
