import { describe, it, expect, vi, beforeEach } from 'vitest';
import { buildSignalREventHandlers } from './signalr-event-handlers';

describe('buildSignalREventHandlers', () => {

  let quoteSvc: any;
  let insightSvc: any;

  beforeEach(() => {
    quoteSvc = {
      upsertOneInCache: vi.fn()
    };

    insightSvc = {
      upsertOneInCache: vi.fn()
    };
  });

  it('should handle quote.created', () => {

    const handlers = buildSignalREventHandlers({ quoteSvc, insightSvc });

    const payload = {
      id: '1',
      symbol: 'AAPL',
      price: 150,
      changePercent: 1.5,
      timestamp: '2026-03-12T00:00:00Z'
    };

    handlers['quote.created']!(payload);

    expect(quoteSvc.upsertOneInCache).toHaveBeenCalledTimes(1);
    expect(quoteSvc.upsertOneInCache).toHaveBeenCalledWith({ ...payload });
  });

  it('should handle quote.updated', () => {

    const handlers = buildSignalREventHandlers({ quoteSvc, insightSvc });

    const payload = {
      id: '2',
      symbol: 'TSLA',
      price: 700,
      changePercent: -0.8,
      timestamp: '2026-03-12T00:00:00Z'
    };

    handlers['quote.updated']!(payload);

    expect(quoteSvc.upsertOneInCache).toHaveBeenCalledTimes(1);
    expect(quoteSvc.upsertOneInCache).toHaveBeenCalledWith({ ...payload });
  });

  it('should handle quote.ai.insight', () => {

    const handlers = buildSignalREventHandlers({ quoteSvc, insightSvc });

    const payload = {
      id: 'ai1',
      symbol: 'NVDA',
      price: 900,
      sentiment: 'bullish',
      direction: 'up',
      volatility: 'medium',
      rationale: 'Strong earnings',
      observedAt: '2026-03-12T00:00:00Z'
    };

    handlers['quote.ai.insight']!(payload);

    expect(insightSvc.upsertOneInCache).toHaveBeenCalledTimes(1);
    expect(insightSvc.upsertOneInCache).toHaveBeenCalledWith({ ...payload });
  });

  it('should clone payload before sending to service', () => {

    const handlers = buildSignalREventHandlers({ quoteSvc, insightSvc });

    const payload = {
      id: '3',
      symbol: 'MSFT',
      price: 400,
      changePercent: 0.2,
      timestamp: '2026-03-12T00:00:00Z'
    };

    handlers['quote.created']!(payload);

    const passed = quoteSvc.upsertOneInCache.mock.calls[0][0];

    expect(passed).not.toBe(payload); // ensure clone
    expect(passed).toEqual(payload);
  });

});