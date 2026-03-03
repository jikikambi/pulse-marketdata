import { AIInsightPayload } from "../../models/ai-insights.model";
import { QuoteCreatedPayload } from "../../models/quote-created.model";
import { SignalREventType } from "../../models/signalr-event-type.model";

export type PayloadFor<K extends SignalREventType> =
    K extends 'quote.created' ? QuoteCreatedPayload :
    //K extends 'quote.updated' ? QuoteUpdatedPayload :
    K extends 'quote.ai.insight' ? AIInsightPayload :
    never;