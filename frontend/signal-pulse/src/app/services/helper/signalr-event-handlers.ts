import { EntityCollectionService } from '@ngrx/data';
import { AIInsightPayload } from '../../models/ai-insights.model';
import { QuoteCreatedPayload } from '../../models/quote-created.model';
import { QuoteUpdatedPayload } from '../../models/quote-updated.model';

/**
 * Handlers for SignalR events.
 * This keeps growing independently from the sync service.
 */
export function buildSignalREventHandlers(deps: { quoteSvc: EntityCollectionService<QuoteCreatedPayload>; }) {

    return {
        'quote.created': (p: QuoteCreatedPayload) => { deps.quoteSvc.upsertOneInCache(p); },

        'quote.updated': (p: QuoteUpdatedPayload) => { deps.quoteSvc.upsertOneInCache(p); },

         // TODO: write insight logic
        'quote.ai.insight': (p: AIInsightPayload) => { console.log('AI insight received:', p); }

    } as Record<string, (payload: any) => void>;
}
