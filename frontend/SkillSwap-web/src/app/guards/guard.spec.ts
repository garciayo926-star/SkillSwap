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
    localStorage.setItem('userId', 'u1');
    localStorage.setItem('userRole', 'Student');
    expect(executeGuard(route(), state)).toBe(true);
  });

  it('blocks a Student on Master-only routes', () => {
    localStorage.setItem('userId', 'u1');
    localStorage.setItem('userRole', 'Student');
    expect(executeGuard(route(['Master']), state)).toBeInstanceOf(UrlTree);
  });

  it('allows a Master on Master-only routes', () => {
    localStorage.setItem('userId', 'u1');
    localStorage.setItem('userRole', 'Master');
    expect(executeGuard(route(['Master']), state)).toBe(true);
  });
});
