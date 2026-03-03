import { AIInsightPayload } from "../../models/ai-insights.model";
import { QuotePayload } from "../../models/quote-payload.model";
import { SignalREventType } from "../../models/signalr-event-type.model";

export type PayloadFor<K extends SignalREventType> =
    K extends 'quote.created' ? QuotePayload :
    K extends 'quote.updated' ? QuotePayload :
    K extends 'quote.ai.insight' ? AIInsightPayload :
    never;