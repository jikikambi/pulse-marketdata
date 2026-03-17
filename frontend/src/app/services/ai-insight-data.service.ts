import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { QuoteAIInsightPayload } from "../models/quote-ai-insights.model";
import { AIInsightService } from "./ai-insight.service";
import { Observable } from "rxjs";

@Injectable()
export class AIInsightDataService extends DefaultDataService<QuoteAIInsightPayload> {

    constructor(private svc: AIInsightService, httpUrlGenerator: HttpUrlGenerator) {
        super('aiinsight', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<QuoteAIInsightPayload[]> {
        return this.svc.getAIInsights();
    }
}