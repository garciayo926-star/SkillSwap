import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // El login no depende de la sesión, se puede pre-renderizar
  {
    path: 'login',
    renderMode: RenderMode.Prerender
  },
  // Las demás rutas usan localStorage (sesión y rol), por eso se renderizan solo en el navegador
  {
    path: '**',
    renderMode: RenderMode.Client
  }
];
