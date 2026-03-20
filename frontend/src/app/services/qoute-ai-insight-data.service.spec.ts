import { TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { of, firstValueFrom, throwError } from 'rxjs';
import { HttpUrlGenerator, DefaultDataService } from '@ngrx/data';
import { QuoteAiInsightDataService } from './qoute-ai-insight-data.service';
import { QuoteAiInsightService } from './quote-ai-insight.service';
import { QuoteAIInsightPayload } from '../models/quote-ai-insights.model';

describe('QuoteAiInsightDataService', () => {

    let service: QuoteAiInsightDataService;
    let quoteAiInsightServiceMock: QuoteAiInsightService;
    let httpUrlGeneratorMock: HttpUrlGenerator;

    beforeEach(() => {

        quoteAiInsightServiceMock = {
            getAIInsights: vi.fn(),
        } as unknown as QuoteAiInsightService;

        httpUrlGeneratorMock = {

            entityResource: vi.fn(() => ''),
            collectionResource: vi.fn(() => ''),
        } as unknown as HttpUrlGenerator;

        TestBed.configureTestingModule({

            providers: [
                QuoteAiInsightDataService,
                { provide: QuoteAiInsightService, useValue: quoteAiInsightServiceMock },
                { provide: HttpUrlGenerator, useValue: httpUrlGeneratorMock },
            ],
        });

        service = TestBed.inject(QuoteAiInsightDataService);
    });

    it('should create the service', () => {
        expect(service).toBeTruthy();
    });

    it('getAll should return QuoteAIInsightPayload array from QuoteAiInsightService', async () => {

        const mockQuotes: QuoteAIInsightPayload[] = [
            {
                id: '1',
                symbol: 'AAPL',
                price: 150,
                sentiment: 'bullish',
                direction: 'up',
                volatility: 'low',
                rationale: 'Strong earnings',
                observedAt: '2026-03-12T00:00:00Z'
            }
        ];

        (quoteAiInsightServiceMock.getAIInsights as any).mockReturnValue(of(mockQuotes));

        const result = await firstValueFrom(service.getAll());

        expect(result).toEqual(mockQuotes);

        expect(quoteAiInsightServiceMock.getAIInsights).toHaveBeenCalled();
    });

    it('getAll should NOT call DefaultDataService.getAll', async () => {

        const superSpy = vi.spyOn(DefaultDataService.prototype, 'getAll');

        (quoteAiInsightServiceMock.getAIInsights as any).mockReturnValue(of([]));

        await firstValueFrom(service.getAll());

        expect(superSpy).not.toHaveBeenCalled();
    });

    it('getAll should propagate error if QuoteAiInsightService.getAIInsights fails', async () => {

        const error = new Error('Network error');

        (quoteAiInsightServiceMock.getAIInsights as any).mockReturnValue(throwError(() => error));

        await expect(firstValueFrom(service.getAll())).rejects.toThrow('Network error');
    });
});