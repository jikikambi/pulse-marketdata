import { EntityCollectionService } from '@ngrx/data';
import { AIInsightPayload } from '../../models/ai-insights.model';
import { QuotePayload } from '../../models/quote-payload.model';
import { SignalREventHandlerMap } from './signalr-event-handler-map';

export interface SignalRHandlerServices {
    quoteSvc: EntityCollectionService<QuotePayload>;
    insightSvc: EntityCollectionService<AIInsightPayload>;
}

export interface QuoteDeps {
    quoteSvc: EntityCollectionService<any>;
}

/**
 * Handlers for SignalR events.
 * This keeps growing independently from the sync service.
 */
export function buildSignalREventHandlers(deps: SignalRHandlerServices): SignalREventHandlerMap {

    return {

        'quote.created': (p: QuotePayload) => { deps.quoteSvc.upsertOneInCache({ ...p }); },
        'quote.updated': (p: QuotePayload) => { deps.quoteSvc.upsertOneInCache({ ...p }); },
        'quote.ai.insight': (p: AIInsightPayload) => { deps.insightSvc.upsertOneInCache({ ...p }); }
    };
}
