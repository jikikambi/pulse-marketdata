import { EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer, Type } from "@angular/core";
import { EntityCollectionDataService, EntityDataService } from "@ngrx/data";

export interface CustomDataServiceConfig<T = unknown> {

    entityName: string;

    dataService: Type<EntityCollectionDataService<T>>;
}

/**
 * DI bootstrap aggregator:
 * Provides the classes + registers them at app init.
 * Also ensures SignalR sync + config services are instantiated automatically.
 */
export function provideCustomDataService(services: ReadonlyArray<CustomDataServiceConfig<any>>,
     cfgService: Type<any>[] = [], 
     syncService: Type<any>[] = []) : EnvironmentProviders {

    return makeEnvironmentProviders([

        // 1. Make every data service injectable
        ...services.map(svc => svc.dataService),

         // 2. Make config services injectable
        ...cfgService,

        // 3. Make sync services injectable
        ...syncService,       

        // 4. Register each data service with NgRx EntityDataService once DI is ready
        provideAppInitializer(() => {

            const entityDataService = inject(EntityDataService);

            services.forEach(({ entityName, dataService }) => {

                const instance = inject(dataService) as EntityCollectionDataService<any>;

                entityDataService.registerService(entityName, instance);
            });

            // Instantiate sync services so constructors run
            syncService.forEach(svc => inject(svc));

            // Instantiate config services so constructors run
            cfgService.forEach(svc => inject(svc));
        })
    ]);
}