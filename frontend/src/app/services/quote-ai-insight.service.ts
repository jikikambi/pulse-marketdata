import { Injectable } from "@angular/core";
import { QuoteAIInsightPayload } from "../models/quote-ai-insights.model";
import { API_ENDPOINTS } from "./constants";
import { SignalPulseDataService } from "./signal-pulse-data.service";

@Injectable({ providedIn: 'root' })
export class QuoteAiInsightService extends SignalPulseDataService<QuoteAIInsightPayload> {

    loadAIInsights = () => this.load(API_ENDPOINTS.quoteInsights);

    getAIInsights = () => this.get(API_ENDPOINTS.quoteInsights);
}