import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { QuoteAIInsightPayload } from '../models/quote-ai-insights.model';
import { QuoteAiInsightService } from './quote-ai-insight.service';
import { API_ENDPOINTS } from './constants';
import { provideHttpClient } from '@angular/common/http';

describe('QuoteAIInsightService', () => {

    let service: QuoteAiInsightService;
    let httpMock: HttpTestingController;

    const mockInsights: QuoteAIInsightPayload[] = [
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

    beforeEach(() => {

        TestBed.configureTestingModule({
            providers: [QuoteAiInsightService, provideHttpClient(), provideHttpClientTesting()]
        });

        service = TestBed.inject(QuoteAiInsightService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should load AI insights and update signal', () => {

        service.loadAIInsights();

        const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.quoteInsights}`);
        expect(req.request.method).toBe('GET');

        req.flush(mockInsights);

        expect(service.data()).toEqual(mockInsights);
    });

    it('should set empty array on error', () => {

        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { });

        service.loadAIInsights();

        const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.quoteInsights}`);

        req.flush('error', { status: 500, statusText: 'Server Error' });

        expect(service.data()).toEqual([]);
        expect(consoleSpy).toHaveBeenCalled();

        consoleSpy.mockRestore();
    });

    it('getAIInsights should call the correct API endpoint', () => {

        service.getAIInsights().subscribe(res => {
            expect(res).toEqual(mockInsights);
        });

        const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.quoteInsights}`);
        expect(req.request.method).toBe('GET');

        req.flush(mockInsights);
    });
});