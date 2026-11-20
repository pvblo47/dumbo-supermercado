import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withInMemoryScrolling, withViewTransitions } from '@angular/router';

import { routes } from './app.routes';
import { prefersReducedMotion } from './shared/motion';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(),
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    // Cada página nueva parte desde arriba; al volver atrás se recupera la posición anterior.
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled' }), withViewTransitions({
      skipInitialTransition: true,
      // Con movimiento reducido la página cambia sin animación.
      onViewTransitionCreated: ({ transition }) => { if (prefersReducedMotion()) transition.skipTransition(); },
    })),
  ]
};
