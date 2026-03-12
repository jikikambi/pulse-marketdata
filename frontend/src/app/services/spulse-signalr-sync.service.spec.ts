import { TestBed, } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';
import { SpSignalrSyncService } from './spulse-signalr-sync.service';
import { EntityCollectionServiceFactory } from '@ngrx/data';
import { SpSignalrService } from './spulse-signalr.service';
import { ConfigService } from './config.service';
import { QuotePayload } from '../models/quote-payload.model';
import { AIInsightPayload } from '../models/ai-insights.model';
import { BehaviorSubject } from 'rxjs';

// --- Mock Entity Service ---
class MockEntityService<T> {

    private _data: T[] = [];

    readonly entities$ = new BehaviorSubject<T[]>(this._data);

    upsertOneInCache(entity: T) {

        const idx = this._data.findIndex((e: any) => e.id === (entity as any).id);

        if (idx > -1) this._data[idx] = entity;
        else this._data.push(entity);

        this.entities$.next([...this._data]);
    }
}

describe('SpSignalrSyncService', () => {

    let service: SpSignalrSyncService;
    let mockQuoteSvc: MockEntityService<QuotePayload>;
    let mockInsightSvc: MockEntityService<AIInsightPayload>;
    let mockSignalr: { start: any; genEvent: WritableSignal<any | null> };
    let mockCfg: { ready: WritableSignal<boolean>; config: { signalRHubUrl: string }; load: () => Promise<void> };

    beforeEach(async () => {

        mockQuoteSvc = new MockEntityService<QuotePayload>();
        mockInsightSvc = new MockEntityService<AIInsightPayload>();

        const mockFactory: Partial<EntityCollectionServiceFactory> = {

            create: (name: string) => {
                if (name === 'Quote') return mockQuoteSvc as any;
                if (name === 'AIInsight') return mockInsightSvc as any;
                throw new Error(`Unknown entity: ${name}`);
            }
        };

        mockSignalr = {

            start: vi.fn(async () => Promise.resolve()),
            genEvent: signal<null | any>(null)
        };

        mockCfg = {

            ready: signal(false),
            config: { signalRHubUrl: 'http://fakehub' },
            load: async () => { mockCfg.ready.set(true); }
        };

        await TestBed.configureTestingModule({
            providers: [
                SpSignalrSyncService,
                { provide: EntityCollectionServiceFactory, useValue: mockFactory },
                { provide: SpSignalrService, useValue: mockSignalr },
                { provide: ConfigService, useValue: mockCfg }
            ]
        }).compileComponents();

        service = TestBed.inject(SpSignalrSyncService);
    });

    it('should initialize signals as empty arrays', () => {

        expect(service.quoteSig()).toEqual([]);
        expect(service.aiInsightSig()).toEqual([]);
    });

    it('should start SignalR when config becomes ready', async () => {

        expect(mockSignalr.start).not.toHaveBeenCalled();

        // Trigger config ready effect: sets ready=true
        await mockCfg.load();

        // Wait for the effect to react to the signal change
        await new Promise(resolve => setTimeout(resolve, 10));

        expect(mockSignalr.start).toHaveBeenCalledWith('http://fakehub');
    });

    it('should update quote signal when a quote.created event is emitted', async () => {

        const quote: QuotePayload = { id: '1', symbol: 'MSFT', price: 340, changePercent: 0.5, timestamp: '2026-03-11T10:00:00Z' };

        const evt = { type: 'quote.created', payload: quote, eventId: 'evt1', sequence: 1, timestamp: new Date().toISOString() };

        mockSignalr.genEvent.set(evt);

        await new Promise(resolve => setTimeout(resolve, 10));

        expect(service.quoteSig()).toContainEqual(quote);
    });

    it('should update insight signal when a quote.ai.insight event is emitted', async () => {

        const insight: AIInsightPayload = {
            id: 'ins1', symbol: 'AAPL', price: 172, sentiment: 'bullish',
            direction: 'up', volatility: 'high', rationale: 'test', observedAt: '2026-03-11T10:00:00Z'
        };

        const evt = { type: 'quote.ai.insight', payload: insight, eventId: 'evt2', sequence: 1, timestamp: new Date().toISOString() };

        mockSignalr.genEvent.set(evt);

        await new Promise(resolve => setTimeout(resolve, 10));

        expect(service.aiInsightSig()).toContainEqual(insight);
    });
});