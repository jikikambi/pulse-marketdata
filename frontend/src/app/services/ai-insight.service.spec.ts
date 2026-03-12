import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AIInsightService } from './ai-insight.service';
import { AIInsightPayload } from '../models/ai-insights.model';

describe('AIInsightService', () => {

    let service: AIInsightService;
    let httpMock: HttpTestingController;

    const mockInsights: AIInsightPayload[] = [
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
            providers: [AIInsightService, provideHttpClientTesting()]
        });

        service = TestBed.inject(AIInsightService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should load AI insights and update signal', () => {

        service.loadAIInsights();

        const req = httpMock.expectOne(`${service.apiUrl}/signalpulse/insights`);
        expect(req.request.method).toBe('GET');

        req.flush(mockInsights);

        expect(service.aiinsight()).toEqual(mockInsights);
    });

    it('should set empty array on error', () => {

        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { });

        service.loadAIInsights();

        const req = httpMock.expectOne(`${service.apiUrl}/signalpulse/insights`);

        req.flush('error', { status: 500, statusText: 'Server Error' });

        expect(service.aiinsight()).toEqual([]);
        expect(consoleSpy).toHaveBeenCalled();

        consoleSpy.mockRestore();
    });

    it('getAIInsights should call the correct API endpoint', () => {

        service.getAIInsights().subscribe(res => {
            expect(res).toEqual(mockInsights);
        });

        const req = httpMock.expectOne(`${service.apiUrl}/signalpulse/insights`);
        expect(req.request.method).toBe('GET');

        req.flush(mockInsights);
    });
});