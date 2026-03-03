import { EntityCollectionService } from '@ngrx/data';
import { AIInsightPayload } from '../../models/ai-insights.model';
import { QuotePayload } from '../../models/quote-payload.model';
import { SignalREventType } from '../../models/signalr-event-type.model';
import { PayloadFor } from './signalr-event-to-payload';

export interface SignalRHandlerServices {
    quoteSvc: EntityCollectionService<QuotePayload>;
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

        'quote.created': (p: QuotePayload) => { deps.quoteSvc.upsertOneInCache(p); },
        'quote.updated': (p: QuotePayload) => { deps.quoteSvc.upsertOneInCache(p); },
        'quote.ai.insight': (p: AIInsightPayload) => { deps.insightSvc.upsertOneInCache(p); }
    };
}
