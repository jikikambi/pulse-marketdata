import { AIInsightPayload } from "../models/ai-insights.model";
import { SignalREventEnvelope } from "../models/signalr-event-envelope.model";
import { QuotePayload } from "../models/quote-payload.model";

export function mapSignalREvent(evt: SignalREventEnvelope): SignalREventEnvelope<any> {

    switch (evt.type) {

        case 'quote.created':
        case 'quote.updated': {
            const p = evt.payload;
            const payload: QuotePayload = {
                id: p.id,
                symbol: p.symbol,
                price: p.price,
                timestamp: p.timestamp,
                changePercent: p.changePercent
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
                observedAt: p.timestamp
            };

            return { ...evt, payload };
        }

        default:
            return evt; // raw passthrough, still typed as envelope
    }
}