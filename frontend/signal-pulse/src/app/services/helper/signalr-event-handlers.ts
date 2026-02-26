import { EntityCollectionService } from '@ngrx/data';
import { AIInsightPayload } from '../../models/ai-insights.model';
import { QuoteCreatedPayload } from '../../models/quote-created.model';
import { QuoteUpdatedPayload } from '../../models/quote-updated.model';
import { SignalREventType } from '../../models/signalr-event-type.model';
import { PayloadFor } from './signalr-event-to-payload';

export interface SignalRHandlerServices {
    quoteSvc: EntityCollectionService<QuoteCreatedPayload>;
    //insightSvc: EntityCollectionService<AIInsightPayload>;
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

        'quote.created': (p: QuoteCreatedPayload) => { deps.quoteSvc.upsertOneInCache(p); },

        'quote.updated': (p: QuoteUpdatedPayload) => { deps.quoteSvc.upsertOneInCache(p); },

        // TODO: write insight logic
        'quote.ai.insight': (p: AIInsightPayload) => { console.log('AI insight received:', p); }
    };
}
