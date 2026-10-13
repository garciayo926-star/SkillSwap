import { Component, ChangeDetectionStrategy, OnInit, PLATFORM_ID, Inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth';
import { ROLE_LABELS } from '../../services/admin';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  roles?: string[];
}

interface ThemeOption {
  id: string;
  label: string;
  color: string;
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
    { path: '/exchanges', label: 'Intercambios', icon: '🔄' },
    { path: '/calendar', label: 'Calendario', icon: '📅' },
    { path: '/reports', label: 'Reportes', icon: '🚩', roles: ['Administrador', 'Moderador'] }
  ];

  visibleItems: NavItem[] = [];

  // Temas de color disponibles para el selector del menú lateral
  themes: ThemeOption[] = [
    { id: 'emerald', label: 'Esmeralda', color: '#059669' },
    { id: 'blue', label: 'Azul', color: '#2563eb' },
    { id: 'purple', label: 'Púrpura', color: '#7c3aed' },
    { id: 'amber', label: 'Ámbar', color: '#d97706' }
  ];
  currentTheme = 'emerald';

  private readonly THEME_KEY = 'skillswap-theme';

  constructor(
    private authService: AuthService,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername() || 'Usuario';
    this.userRole = this.authService.getUserRole();
    this.roleLabel = ROLE_LABELS[this.userRole] || this.userRole;
    this.visibleItems = this.navItems.filter(item => !item.roles || item.roles.includes(this.userRole));

    // localStorage/document solo existen en el navegador (no en SSR)
    if (isPlatformBrowser(this.platformId)) {
      const saved = localStorage.getItem(this.THEME_KEY);
      this.setTheme(saved && this.themes.some(t => t.id === saved) ? saved : this.currentTheme);
    }
  }

  // Aplica el tema en <html> (data-theme) y lo recuerda para la próxima visita
  setTheme(themeId: string): void {
    this.currentTheme = themeId;
    if (isPlatformBrowser(this.platformId)) {
      document.documentElement.setAttribute('data-theme', themeId);
      localStorage.setItem(this.THEME_KEY, themeId);
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
