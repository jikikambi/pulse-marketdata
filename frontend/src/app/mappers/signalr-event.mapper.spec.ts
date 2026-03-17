import { describe, it, expect } from 'vitest';
import { mapSignalREvent } from './signalr-event.mapper';

describe('mapSignalREvent', () => {

  it('maps quote.created event payload correctly', () => {
    const evt: any = {
      eventId: '1',
      type: 'quote.created',
      sequence: 1,
      timestamp: '2026-03-12T00:00:00Z',
      payload: {
        id: 'q1',
        symbol: 'AAPL',
        price: 150,
        timestamp: '2026-03-12T00:00:00Z',
        changePercent: 1.2
      }
    };

    const result = mapSignalREvent(evt);

    expect(result.payload).toEqual({
      id: 'q1',
      symbol: 'AAPL',
      price: 150,
      timestamp: '2026-03-12T00:00:00Z',
      changePercent: 1.2
    });

    expect(result.type).toBe('quote.created');
  });

  it('maps quote.updated event payload correctly', () => {
    const evt: any = {
      eventId: '2',
      type: 'quote.updated',
      sequence: 2,
      timestamp: '2026-03-12T00:00:00Z',
      payload: {
        id: 'q2',
        symbol: 'TSLA',
        price: 700,
        timestamp: '2026-03-12T00:00:00Z',
        changePercent: -0.8
      }
    };

    const result = mapSignalREvent(evt);

    expect(result.payload.symbol).toBe('TSLA');
    expect(result.payload.price).toBe(700);
    expect(result.payload.changePercent).toBe(-0.8);
  });

  it('maps quote.ai-insight.generated payload correctly', () => {
    const evt: any = {
      eventId: '3',
      type: 'quote.ai-insight.generated',
      sequence: 3,
      timestamp: '2026-03-12T00:00:00Z',
      payload: {
        id: 'ai1',
        symbol: 'NVDA',
        price: 900,
        sentiment: 'bullish',
        direction: 'up',
        volatility: 0.5,
        rationale: 'Strong earnings',
        timestamp: '2026-03-12T00:00:00Z'
      }
    };

    const result = mapSignalREvent(evt);

    expect(result.payload).toEqual({
      id: 'ai1',
      symbol: 'NVDA',
      price: 900,
      sentiment: 'bullish',
      direction: 'up',
      volatility: 0.5,
      rationale: 'Strong earnings',
      observedAt: '2026-03-12T00:00:00Z'
    });
  });

  it('returns original event for unknown event type', () => {
    const evt: any = {
      eventId: '4',
      type: 'unknown.event',
      sequence: 4,
      timestamp: '2026-03-12T00:00:00Z',
      payload: { foo: 'bar' }
    };

    const result = mapSignalREvent(evt);

    expect(result).toBe(evt);
  });

});