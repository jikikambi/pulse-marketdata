import { TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { of, firstValueFrom, throwError } from 'rxjs';
import { HttpUrlGenerator, DefaultDataService } from '@ngrx/data';
import { QuoteDataService } from './quote-data.service';
import { QuoteService } from './quote.service';
import { QuotePayload } from '../models/quote-payload.model';

describe('QuoteDataService', () => {

    let service: QuoteDataService;
    let quoteServiceMock: QuoteService;
    let httpUrlGeneratorMock: HttpUrlGenerator;

    beforeEach(() => {

        quoteServiceMock = {
            getQuotes: vi.fn(),
        } as unknown as QuoteService;

        httpUrlGeneratorMock = {

            entityResource: vi.fn(() => ''),
            collectionResource: vi.fn(() => ''),
        } as unknown as HttpUrlGenerator;

        TestBed.configureTestingModule({

            providers: [
                QuoteDataService,
                { provide: QuoteService, useValue: quoteServiceMock },
                { provide: HttpUrlGenerator, useValue: httpUrlGeneratorMock },
            ],
        });

        service = TestBed.inject(QuoteDataService);
    });

    it('should create the service', () => {
        expect(service).toBeTruthy();
    });

    it('getAll should return QuotePayload array from QuoteService', async () => {

        const mockQuotes: QuotePayload[] = [

            { id: '1', symbol: 'MSFT', price: 340, changePercent: 0.5, timestamp: '2026-03-11T10:00:00Z' },

            { id: '2', symbol: 'AAPL', price: 172, changePercent: -0.3, timestamp: '2026-03-11T10:00:00Z' },
        ];

        (quoteServiceMock.getQuotes as any).mockReturnValue(of(mockQuotes));

        const result = await firstValueFrom(service.getAll());

        expect(result).toEqual(mockQuotes);

        expect(quoteServiceMock.getQuotes).toHaveBeenCalled();
    });

    it('getAll should NOT call DefaultDataService.getAll', async () => {

        const superSpy = vi.spyOn(DefaultDataService.prototype, 'getAll');

        (quoteServiceMock.getQuotes as any).mockReturnValue(of([]));

        await firstValueFrom(service.getAll());

        expect(superSpy).not.toHaveBeenCalled();
    });

    it('getAll should propagate error if QuoteService.getQuotes fails', async () => {

        const error = new Error('Network error');

        (quoteServiceMock.getQuotes as any).mockReturnValue(throwError(() => error));

        await expect(firstValueFrom(service.getAll())).rejects.toThrow('Network error');
    });
});