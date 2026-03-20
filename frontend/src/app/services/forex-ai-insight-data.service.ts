import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { ForexAIInsightPayload } from "../models/forex-ai-insights.model";
import { ForexAiInsightService } from "./forex-ai-insight.service";
import { Observable } from "rxjs";

@Injectable({providedIn: 'root'})
export class ForexAiInsightDataService extends DefaultDataService<ForexAIInsightPayload> {
    
    constructor(private svc: ForexAiInsightService, httpUrlGenerator: HttpUrlGenerator ) {
        super('forexInsight', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<ForexAIInsightPayload[]> {
        return this.svc.getAIInsights();
    }
}