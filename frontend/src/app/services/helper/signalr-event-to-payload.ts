import { ForexAIInsightPayload } from "../../models/forex-ai-insights.model";
import { ForexPayload } from "../../models/forex-payload.model";
import { QuoteAIInsightPayload } from "../../models/quote-ai-insights.model";
import { QuotePayload } from "../../models/quote-payload.model";
import { SignalREventType } from "../../models/signalr-event-type.model";

export type PayloadFor<K extends SignalREventType> =
    K extends 'quote.created' ? QuotePayload :
    K extends 'quote.updated' ? QuotePayload :
    K extends 'quote.ai-insight.generated' ? QuoteAIInsightPayload :
    K extends 'forex.created' ? ForexPayload :
    K extends 'forex.updated' ? ForexPayload :
    K extends 'fx.ai-insight.generated' ? ForexAIInsightPayload :
    never;