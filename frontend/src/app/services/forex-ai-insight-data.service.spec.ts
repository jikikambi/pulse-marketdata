import { TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { of, firstValueFrom, throwError } from 'rxjs';
import { ForexAiInsightDataService } from './forex-ai-insight-data.service';
import { HttpUrlGenerator, DefaultDataService } from '@ngrx/data';
import { ForexAiInsightService } from './forex-ai-insight.service';
import { ForexAIInsightPayload } from '../models/forex-ai-insights.model';

describe('ForexAiInsightDataService', () => {

    let service: ForexAiInsightDataService;
    let forexAiInsightServiceMock: ForexAiInsightService;
    let httpUrlGeneratorMock: HttpUrlGenerator;

    beforeEach(() => {

        forexAiInsightServiceMock = {
            getAIInsights: vi.fn(),
        } as unknown as ForexAiInsightService;

        httpUrlGeneratorMock = {

            entityResource: vi.fn(() => ''),
            collectionResource: vi.fn(() => ''),
        } as unknown as HttpUrlGenerator;

        TestBed.configureTestingModule({

            providers: [
                ForexAiInsightDataService,
                { provide: ForexAiInsightService, useValue: forexAiInsightServiceMock },
                { provide: HttpUrlGenerator, useValue: httpUrlGeneratorMock },
            ],
        });

        service = TestBed.inject(ForexAiInsightDataService);
    });

    it('should create the service', () => {
        expect(service).toBeTruthy();
    });

    it('getAll should return ForexAIInsightPayload array from ForexAiInsightService', async () => {
        const mockData: ForexAIInsightPayload[] = [
            {
                id: '1',
                fromSymbol: 'USD',
                toSymbol: 'EUR',
                open: 1.0,
                high: 1.1,
                low: 0.9,
                close: 1.05,
                forexDate: '2026-03-20',
                sentiment: 'bullish',
                direction: 'up',
                volatility: 'low',
                rationale: 'Strong earnings',
                observedAt: '2026-03-12T00:00:00Z'
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
                sentiment: 'bullish',
                direction: 'up',
                volatility: 'low',
                rationale: 'Strong earnings',
                observedAt: '2026-03-12T00:00:00Z'
            },
        ];

        (forexAiInsightServiceMock.getAIInsights as any).mockReturnValue(of(mockData));

        const result = await firstValueFrom(service.getAll());

        expect(result).toEqual(mockData);

        expect(forexAiInsightServiceMock.getAIInsights).toHaveBeenCalled();
    });

    it('getAll should NOT call DefaultDataService.getAll', async () => {

        const superSpy = vi.spyOn(DefaultDataService.prototype, 'getAll');

        (forexAiInsightServiceMock.getAIInsights as any).mockReturnValue(of([]));

        await firstValueFrom(service.getAll());

        expect(superSpy).not.toHaveBeenCalled();
    });

    it('getAll should propagate error if ForexAiInsightService.getAIInsights fails', async () => {

        const error = new Error('Network error');

        (forexAiInsightServiceMock.getAIInsights as any).mockReturnValue(throwError(() => error));

        await expect(firstValueFrom(service.getAll())).rejects.toThrow('Network error');
    });
});