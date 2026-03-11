import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { API_ENDPOINTS } from './constants';

describe('QuoteService', () => {
  let service: any;
  let httpMock: HttpTestingController;

  const mockQuotes = [
    { id: '1', symbol: 'MSFT', price: 340, changePercent: 0.5, timestamp: '2026-03-11T10:00:00Z' },
    { id: '2', symbol: 'AAPL', price: 172, changePercent: -0.3, timestamp: '2026-03-11T10:00:00Z' },
  ];

  beforeEach(async () => {
    const mod = await import('./quote.service');
    const QuoteService = mod.QuoteService;

    TestBed.configureTestingModule({
      providers: [QuoteService, provideHttpClientTesting()],
    });

    service = TestBed.inject(QuoteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should load quotes and update the signal', () => {
    service.loadQuotes();

    const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.quotes}`);
    expect(req.request.method).toBe('GET');

    req.flush(mockQuotes);

    expect(service.quotes()).toEqual(mockQuotes);
    httpMock.verify();
  });

  it('should handle error and set empty quotes', () => {
    service.loadQuotes();

    const req = httpMock.expectOne(`${service.apiUrl}${API_ENDPOINTS.quotes}`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    expect(service.quotes()).toEqual([]);
    httpMock.verify();
  });
});