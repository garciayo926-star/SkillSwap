import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

// Adjunta el token JWT a cada petición y gestiona sesiones expiradas o sin permiso.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  const token = (typeof window !== 'undefined' && window.localStorage)
    ? localStorage.getItem('token')
    : null;

  // No se adjunta el token a las peticiones de login/registro
  const isAuthRequest = req.url.includes('/api/Auth/');

  const authReq = (token && !isAuthRequest)
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // 401: token ausente, inválido o caducado -> se cierra la sesión y se vuelve al login
      if (error.status === 401 && !isAuthRequest) {
        if (typeof window !== 'undefined' && window.localStorage) {
          localStorage.clear();
        }
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};
