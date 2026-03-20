import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { QuoteAIInsightPayload } from "../models/quote-ai-insights.model";
import { QuoteAiInsightService } from "./quote-ai-insight.service";
import { Observable } from "rxjs";

@Injectable()
export class QuoteAiInsightDataService extends DefaultDataService<QuoteAIInsightPayload> {

    constructor(private svc: QuoteAiInsightService, httpUrlGenerator: HttpUrlGenerator) {
        super('quoteInsight', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<QuoteAIInsightPayload[]> {
        return this.svc.getAIInsights();
    }
}


