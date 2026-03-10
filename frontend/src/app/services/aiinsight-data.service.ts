import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { AIInsightPayload } from "../models/ai-insights.model";
import { AIInsightService } from "./aiinsight.service";
import { Observable } from "rxjs";

@Injectable()
export class AIInsightDataService extends DefaultDataService<AIInsightPayload> {

    constructor(private svc: AIInsightService, httpUrlGenerator: HttpUrlGenerator) {
        super('aiinsight', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<AIInsightPayload[]> {
        return this.svc.getAIInsights();
    }
}