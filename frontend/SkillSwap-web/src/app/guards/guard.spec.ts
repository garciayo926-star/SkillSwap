import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRouteSnapshot, CanActivateFn, provideRouter, RouterStateSnapshot, UrlTree } from '@angular/router';
import { roleGuard } from './guard';

describe('roleGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => roleGuard(...guardParameters));

  const route = (roles?: string[]) => ({ data: roles ? { roles } : {} }) as unknown as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  // Simula una sesión válida (ahora requiere token, id y rol)
  const session = (role: string) => {
    localStorage.setItem('token', 'token-de-prueba');
    localStorage.setItem('userId', 'u1');
    localStorage.setItem('userRole', role);
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    localStorage.clear();
  });

  it('redirects to login when there is no session', () => {
    expect(executeGuard(route(), state)).toBeInstanceOf(UrlTree);
  });

  it('allows a logged user on routes without roles', () => {
    session('Estudiante');
    expect(executeGuard(route(), state)).toBe(true);
  });

  it('blocks an Estudiante on Administrador-only routes', () => {
    session('Estudiante');
    expect(executeGuard(route(['Administrador']), state)).toBeInstanceOf(UrlTree);
  });

  it('allows an Administrador on Administrador-only routes', () => {
    session('Administrador');
    expect(executeGuard(route(['Administrador']), state)).toBe(true);
  });
});
