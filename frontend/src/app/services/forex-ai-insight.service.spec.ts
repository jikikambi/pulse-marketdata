import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { ForexAIInsightPayload } from '../models/forex-ai-insights.model';
import { API_ENDPOINTS } from './constants';
import { ForexAiInsightService } from '../services/forex-ai-insight.service';
import { provideHttpClient } from '@angular/common/http';

describe('ForexAiInsightService', () => {

    let service: ForexAiInsightService;
    let httpMock: HttpTestingController;

    const mockInsights: ForexAIInsightPayload[] = [
        {
            id: '1',
            fromSymbol: 'EUR',
            toSymbol: 'USD',
            open: 1.1,
            high: 1.2,
            low: 1.0,
            close: 1.15,
            forexDate: '2026-03-12T00:00:00Z',
            sentiment: 'bullish',
            direction: 'up',
            volatility: 'low',
            rationale: 'Strong earnings',
            observedAt: '2026-03-12T00:00:00Z'
        }
    ];

    beforeEach(() => {

        TestBed.configureTestingModule({
            providers: [ForexAiInsightService, provideHttpClient(), provideHttpClientTesting()]
        });

        service = TestBed.inject(ForexAiInsightService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should load AI insights and update signal', () => {

        service.loadAIInsights();

        const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.forexInsights}`);
        expect(req.request.method).toBe('GET');

        req.flush(mockInsights);

        expect(service.data()).toEqual(mockInsights);
    });

    it('should set empty array on error', () => {

        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { });

        service.loadAIInsights();

        const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.forexInsights}`);

        req.flush('error', { status: 500, statusText: 'Server Error' });

        expect(service.data()).toEqual([]);
        expect(consoleSpy).toHaveBeenCalled();

        consoleSpy.mockRestore();
    });

    it('getAIInsights should call the correct API endpoint', () => {

        service.getAIInsights().subscribe(res => {
            expect(res).toEqual(mockInsights);
        });

        const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.forexInsights}`);
        expect(req.request.method).toBe('GET');

        req.flush(mockInsights);
    });
});