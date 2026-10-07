import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { LayoutComponent } from './components/layout/layout';
import { DashboardComponent } from './components/dashboard/dashboard';
import { StudentsComponent } from './components/students/students';
import { SkillsComponent } from './components/skills/skills';
import { OffersComponent } from './components/offers/offers';
import { RequestsComponent } from './components/requests/requests';
import { MatchesComponent } from './components/matches/matches';
import { ExchangesComponent } from './components/exchanges/exchanges';
import { roleGuard } from './guards/guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },

  // Rutas protegidas: comparten el menú lateral (LayoutComponent)
  {
    path: '',
    component: LayoutComponent,
    canActivate: [roleGuard],
    canActivateChild: [roleGuard],
    children: [
      // Ruta protegida general para usuarios logueados
      { path: 'dashboard', component: DashboardComponent },

      // Ruta protegida exclusiva para el rol Master
      { path: 'admin/analytics', component: DashboardComponent, data: { roles: ['Master'] } },

      // Ruta protegida para Technical o Master
      { path: 'admin/system-health', component: DashboardComponent, data: { roles: ['Technical', 'Master'] } },

      // CRUD de Estudiantes: solo administración (Technical o Master)
      { path: 'students', component: StudentsComponent, data: { roles: ['Technical', 'Master'] } },

      // CRUD disponibles para todos los roles (cada pantalla limita acciones según el rol)
      { path: 'skills', component: SkillsComponent },
      { path: 'offers', component: OffersComponent },
      { path: 'requests', component: RequestsComponent },
      { path: 'matches', component: MatchesComponent },
      { path: 'exchanges', component: ExchangesComponent },

      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: 'login' }
];
