import { TestBed } from '@angular/core/testing';
import { Injectable } from '@angular/core';
import { EntityCollectionDataService, EntityDataService } from '@ngrx/data';
import { describe, it, expect, vi, beforeEach } from 'vitest';

import { provideCustomDataService } from './provide-custom-data-services';

describe('provideCustomDataService', () => {

    let registerSpy = vi.fn();

    @Injectable()
    class MockQuoteDataService { }

    type MockEntityService = new (...args: any[]) => EntityCollectionDataService<any>;

    @Injectable()
    class MockSyncService {
        static constructed = false;

        constructor() {
            MockSyncService.constructed = true;
        }
    }

    @Injectable()
    class MockConfigService {
        static constructed = false;

        constructor() {
            MockConfigService.constructed = true;
        }
    }

    beforeEach(async () => {

        registerSpy = vi.fn();

        await TestBed.configureTestingModule({

            providers: [

                { provide: EntityDataService, useValue: { registerService: registerSpy } },

                provideCustomDataService(
                    [
                        {
                            entityName: 'Quote',
                            dataService: MockQuoteDataService as unknown as MockEntityService
                        }
                    ],
                    [MockConfigService],
                    [MockSyncService]
                )

            ]

        });

        // Trigger APP Intializers
        TestBed.inject(EntityDataService);
    });

    it('registers entity services', () => {

        expect(registerSpy).toHaveBeenCalledTimes(1);

        const [entityName, instance] = registerSpy.mock.calls[0];

        expect(entityName).toBe('Quote');
        expect(instance).toBeInstanceOf(MockQuoteDataService);

    });

    it('instantiates sync services', () => {

        expect(MockSyncService.constructed).toBe(true);

    });

    it('instantiates config services', () => {

        expect(MockConfigService.constructed).toBe(true);

    });
});