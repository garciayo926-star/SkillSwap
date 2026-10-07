import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { routes } from './app.routes';
import { provideClientHydration } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Detección de cambios sin zone.js (zoneless): la vista se actualiza mediante signals
    provideZonelessChangeDetection(),
    provideRouter(routes), provideClientHydration(),
    provideHttpClient(withFetch())
  ]
};
