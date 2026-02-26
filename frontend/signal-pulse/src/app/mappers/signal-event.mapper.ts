import { AIInsightPayload } from "../models/ai-insights.model";
import { SignalREventEnvelope } from "../models/signalr-event-envelope.model";
import { QuoteCreatedPayload } from "../models/quote-created.model";
import { QuoteUpdatedPayload } from "../models/quote-updated.model";

export function mapSignalREvent(evt: SignalREventEnvelope): SignalREventEnvelope<any> {

    switch (evt.type) {

        case 'quote.created': {
            const p = evt.payload;
            const payload: QuoteCreatedPayload = {
                id: p.id,
                symbol: p.symbol,
                price: p.price,
                timestamp: p.timestamp
            };

            return { ...evt, payload };
        }

        case 'quote.updated': {
            const p = evt.payload;
            const payload: QuoteUpdatedPayload = {
                id: p.id,
                symbol: p.symbol,
                price: p.price,
                timestamp: p.timestamp
            };

            return { ...evt, payload };
        }

        case 'quote.ai.insight': {
            const p = evt.payload;
            const payload: AIInsightPayload = {
                id: p.id,
                symbol: p.symbol,
                price: p.price,
                sentiment: p.sentiment,
                direction: p.direction,
                volatility: p.volatility,
                rationale: p.rationale,
                timestamp: p.timestamp
            };

            return { ...evt, payload };
        }

        default:
            return evt; // raw passthrough, still typed as envelope
    }
}