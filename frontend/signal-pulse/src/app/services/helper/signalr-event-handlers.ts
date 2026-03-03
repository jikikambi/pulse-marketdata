import { EntityCollectionService } from '@ngrx/data';
import { AIInsightPayload } from '../../models/ai-insights.model';
import { QuoteCreatedPayload } from '../../models/quote-created.model';
import { SignalREventType } from '../../models/signalr-event-type.model';
import { PayloadFor } from './signalr-event-to-payload';

export interface SignalRHandlerServices {
    quoteSvc: EntityCollectionService<QuoteCreatedPayload>;
    insightSvc: EntityCollectionService<AIInsightPayload>;
}

export interface QuoteDeps {
    quoteSvc: EntityCollectionService<any>;
}

export type SignalREventHandlerMap = {
    [K in SignalREventType]?: (payload: PayloadFor<K>) => void;
};

/**
 * Handlers for SignalR events.
 * This keeps growing independently from the sync service.
 */
export function buildSignalREventHandlers(deps: SignalRHandlerServices): SignalREventHandlerMap {

    return {

        //'quote.created': (p: QuoteCreatedPayload) => { deps.quoteSvc.upsertOneInCache(p); },
        'quote.created': (p: QuoteCreatedPayload) => {
            const normalized = {
                id: p.id ?? p.id,
                symbol: p.symbol ?? p.symbol,
                price: p.price ?? p.price,
                changePercent: p.changePercent ?? p.changePercent,
                timestamp: p.timestamp ?? p.timestamp
            };

            if (!normalized.id) {
                console.error("QuoteCreated missing ID", p);
                return;
            }

            deps.quoteSvc.upsertOneInCache(normalized);
        },

        // 'quote.updated': (p: QuoteUpdatedPayload) => {
        //     const normalized = {
        //         id: p.id ?? p.id,
        //         symbol: p.symbol ?? p.symbol,
        //         price: p.price ?? p.price,
        //         changePercent: p.changePercent ?? p.changePercent,
        //         timestamp: p.timestamp ?? p.timestamp
        //     };

        //     if (!normalized.id) {
        //         console.error("QuoteUpdated missing ID", p);
        //         return;
        //     }

        //     deps.quoteSvc.upsertOneInCache(normalized);
        // },

        //'quote.updated': (p: QuoteUpdatedPayload) => { deps.quoteSvc.upsertOneInCache(p); },

        'quote.ai.insight': (ai: AIInsightPayload) => {
            const normalized = {
                id: ai.id ?? ai.id,
                symbol: ai.symbol ?? ai.symbol,
                price: ai.price ?? ai.price,
                sentiment: ai.sentiment ?? ai.sentiment,
                direction: ai.direction ?? ai.direction,
                volatility: ai.volatility ?? ai.volatility,
                rationale: ai.rationale ?? ai.rationale,
                observedAt: ai.observedAt ?? ai.observedAt
            };

            if (!normalized.id) {
                console.error("AIInsight missing ID", ai);
                return;
            }

            deps.quoteSvc.upsertOneInCache(normalized);
        },

        //'quote.ai.insight': (p: AIInsightPayload) => { deps.insightSvc.upsertOneInCache(p); }
    };
}
