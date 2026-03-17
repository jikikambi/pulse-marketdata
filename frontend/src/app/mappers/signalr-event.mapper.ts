import { QuoteAIInsightPayload } from "../models/quote-ai-insights.model";
import { SignalREventEnvelope } from "../models/signalr-event-envelope.model";
import { QuotePayload } from "../models/quote-payload.model";
import { ForexAIInsightPayload } from "../models/forex-ai-insights.model";
import { ForexPayload } from "../models/forex-payload.model";

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

        case 'quote.ai-insight.generated': {
            const p = evt.payload;
            const payload: QuoteAIInsightPayload = {
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

        case 'forex.created':
        case 'forex.updated': {
            const p = evt.payload; 
            const payload : ForexPayload = { 
                id: p.id,
                fromSymbol: p.fromSymbol,
                toSymbol: p.toSymbol,
                open: p.open,
                high: p.high,
                low: p.low,
                close: p.close,
                forexDate: p.forexDate,
                timestamp: p.timestamp               
            }

            return { ...evt, payload };
        
        }

        case 'fx.ai-insight.generated': {
            const p = evt.payload;
            const payload: ForexAIInsightPayload = {
                id: p.id,
                fromSymbol: p.fromSymbol,
                toSymbol: p.toSymbol,
                open: p.open,
                high: p.high,
                low: p.low,
                close: p.close,
                forexDate: p.forexDate,
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