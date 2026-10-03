import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth'; // Asegúrate de ajustar la ruta a tu auth.service

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Obtenemos el rol actual del usuario almacenado en el AuthService o localStorage
  const userRole = authService.getUserRole();

  // Verificamos si la ruta exige roles específicos
  const allowedRoles = route.data['roles'] as Array<string>;

  if (!userRole) {
    // Si no hay sesión, redirigimos al login
    router.navigate(['/login']);
    return false;
  }

  // Si la ruta no especifica roles, con tal de que esté logueado puede entrar
  if (!allowedRoles || allowedRoles.length === 0) {
    return true;
  }

  // Comprobamos si el rol del usuario está permitido en esta ruta
  if (allowedRoles.includes(userRole)) {
    return true;
  }

  // Si no tiene el rol necesario, lo redirigimos al dashboard general o una página de acceso denegado
  router.navigate(['/dashboard']);
  return false;
};