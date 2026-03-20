import { TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { of, firstValueFrom, throwError } from 'rxjs';
import { ForexDataService } from './forex-data.service';
import { ForexService } from './forex.service';
import { HttpUrlGenerator, DefaultDataService } from '@ngrx/data';
import { ForexPayload } from '../models/forex-payload.model';

describe('ForexDataService', () => {

    let service: ForexDataService;
    let forexServiceMock: ForexService;
    let httpUrlGeneratorMock: HttpUrlGenerator;

    beforeEach(() => {

        forexServiceMock = {
            getForex: vi.fn(),
        } as unknown as ForexService;

        httpUrlGeneratorMock = {

            entityResource: vi.fn(() => ''),
            collectionResource: vi.fn(() => ''),
        } as unknown as HttpUrlGenerator;

        TestBed.configureTestingModule({

            providers: [
                ForexDataService,
                { provide: ForexService, useValue: forexServiceMock },
                { provide: HttpUrlGenerator, useValue: httpUrlGeneratorMock },
            ],
        });

        service = TestBed.inject(ForexDataService);
    });

    it('should create the service', () => {
        expect(service).toBeTruthy();
    });

    it('getAll should return ForexPayload array from ForexService', async () => {
        const mockData: ForexPayload[] = [
            {
                id: '1',
                fromSymbol: 'USD',
                toSymbol: 'EUR',
                open: 1.0,
                high: 1.1,
                low: 0.9,
                close: 1.05,
                forexDate: '2026-03-20',
                timestamp: '2026-03-20T12:00:00Z',
            },
            {
                id: '2',
                fromSymbol: 'GBP',
                toSymbol: 'USD',
                open: 1.3,
                high: 1.35,
                low: 1.25,
                close: 1.32,
                forexDate: '2026-03-20',
                timestamp: '2026-03-20T12:00:00Z',
            },
        ];

        (forexServiceMock.getForex as any).mockReturnValue(of(mockData));

        const result = await firstValueFrom(service.getAll());

        expect(result).toEqual(mockData);

        expect(forexServiceMock.getForex).toHaveBeenCalled();
    });

    it('getAll should NOT call DefaultDataService.getAll', async () => {

        const superSpy = vi.spyOn(DefaultDataService.prototype, 'getAll');

        (forexServiceMock.getForex as any).mockReturnValue(of([]));

        await firstValueFrom(service.getAll());

        expect(superSpy).not.toHaveBeenCalled();
    });

    it('getAll should propagate error if ForexService.getForex fails', async () => {

        const error = new Error('Network error');

        (forexServiceMock.getForex as any).mockReturnValue(throwError(() => error));

        await expect(firstValueFrom(service.getAll())).rejects.toThrow('Network error');
    });
});