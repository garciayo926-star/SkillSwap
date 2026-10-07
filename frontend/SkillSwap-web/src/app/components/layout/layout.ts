import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth';

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

  // Menú principal; los elementos con "roles" solo se muestran a esos roles
  private navItems: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/students', label: 'Estudiantes', icon: '🎓', roles: ['Master', 'Technical'] },
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
    this.visibleItems = this.navItems.filter(item => !item.roles || item.roles.includes(this.userRole));
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
