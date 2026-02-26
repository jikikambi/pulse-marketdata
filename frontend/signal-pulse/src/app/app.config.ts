import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';

import { routes } from './app.routes';
import { provideStore } from '@ngrx/store';
import { provideEntityData, withEffects } from '@ngrx/data';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { customeDataServices, entityConfig } from './entity-metadata';
import { provideCustomDataService } from './services/provide-custom-data-services';
import { SpSignalrSyncService } from './services/spulse-signalr-sync.service';
import { ConfigService } from './services/config.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    provideHttpClient(),

    provideRouter(routes),

    // --- NgRx Store setup ---
    provideStore(),

    // --- NgRx Data setup ---
    provideEntityData(entityConfig, withEffects()),

    // --- Register all NgRx Data services + auto-start SignalR sync + config ---
    provideCustomDataService(customeDataServices, [ConfigService], [SpSignalrSyncService]),

    // --- DevTools ---
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() })
  ]
};