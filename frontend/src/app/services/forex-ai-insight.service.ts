import { Injectable } from "@angular/core";
import { ForexAIInsightPayload } from "../models/forex-ai-insights.model";
import { API_ENDPOINTS } from "./constants";
import { SignalPulseDataService } from "./signal-pulse-data.service";

@Injectable({ providedIn: 'root' })

export class ForexAiInsightService extends SignalPulseDataService<ForexAIInsightPayload> {

    loadAIInsights = () => this.load(API_ENDPOINTS.forexInsights);

    getAIInsights = () => this.get(API_ENDPOINTS.forexInsights);
}