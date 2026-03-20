import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { API_ENDPOINTS } from './constants';
import { ForexService } from './forex.service';
import { provideHttpClient } from '@angular/common/http';

describe('ForexService', () => {
  let service: ForexService;
  let httpMock: HttpTestingController;

  const mockForex = [
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
        },
    {
            id: '2',
            fromSymbol: 'GBP',
            toSymbol: 'USD',
            open: 1.25,
            high: 1.3,
            low: 1.2,
            close: 1.28,
            forexDate: '2026-03-12T00:00:00Z',
            sentiment: 'bearish',
            direction: 'down',
            volatility: 'high',
            rationale: 'Economic data disappointment',
            observedAt: '2026-03-12T00:00:00Z'
        }
  ];

  beforeEach(async () => {
    
    TestBed.configureTestingModule({
      providers: [ForexService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ForexService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should load forex and update the signal', () => {
    service.loadForex();

    const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.forex}`);
    expect(req.request.method).toBe('GET');

    req.flush(mockForex);

    expect(service.data()).toEqual(mockForex);
    httpMock.verify();
  });

  it('should handle error and set empty forex', () => {
    service.loadForex();

    const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.forex}`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    expect(service.data()).toEqual([]);
    httpMock.verify();
  });
});