import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login'; // Ajusta según el nombre exacto de tu archivo en login/
import { DashboardComponent } from './components/dashboard/dashboard'; // Ajusta según el nombre exacto en dashboard/
import { roleGuard } from './guards/guard'; // O './guards/auth.guard' según hayas nombrado el archivo

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  
  // Ruta protegida general para usuarios logueados
  { 
    path: 'dashboard', 
    component: DashboardComponent, 
    canActivate: [roleGuard] 
  },

  // Ruta protegida exclusiva para el rol Master
  { 
    path: 'admin/analytics', 
    component: DashboardComponent, 
    canActivate: [roleGuard],
    data: { roles: ['Master'] } 
  },

  // Ruta protegida para Technical o Master
  { 
    path: 'admin/system-health', 
    component: DashboardComponent, 
    canActivate: [roleGuard],
    data: { roles: ['Technical', 'Master'] } 
  },

  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];