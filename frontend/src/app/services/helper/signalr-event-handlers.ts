import { EntityCollectionService } from '@ngrx/data';
import { QuoteAIInsightPayload } from '../../models/quote-ai-insights.model';
import { QuotePayload } from '../../models/quote-payload.model';
import { SignalREventHandlerMap } from './signalr-event-handler-map';
import { ForexAIInsightPayload } from '../../models/forex-ai-insights.model';
import { ForexPayload } from '../../models/forex-payload.model';

export interface SignalRHandlerServices {
    quoteSvc: EntityCollectionService<QuotePayload>;
    quoteInsightSvc: EntityCollectionService<QuoteAIInsightPayload>;
    forexSvc: EntityCollectionService<ForexPayload>;
    forexAiInsightSvc: EntityCollectionService<ForexAIInsightPayload>;
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
        'quote.ai-insight.generated': (p: QuoteAIInsightPayload) => { deps.quoteInsightSvc.upsertOneInCache({ ...p }); },
        'forex.created': (p: ForexPayload) => { deps.forexSvc.upsertOneInCache({ ...p }); },
        'forex.updated': (p: ForexPayload) => { deps.forexSvc.upsertOneInCache({ ...p }); },
        'fx.ai-insight.generated': (p: ForexAIInsightPayload) => { deps.forexAiInsightSvc.upsertOneInCache({ ...p }); }
    };
}
