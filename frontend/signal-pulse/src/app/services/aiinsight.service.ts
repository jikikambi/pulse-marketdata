import { HttpClient } from "@angular/common/http";
import { inject, Injectable, signal } from "@angular/core";
import { AIInsightPayload } from "../models/ai-insights.model";
import { BaseService } from "./base.service";

@Injectable({providedIn: 'root'})
export class AIInsightService extends BaseService {

    private readonly http = inject(HttpClient);

    readonly aiinsight = signal<AIInsightPayload[]>([]);

    constructor() {
        super();
    }

    loadAIInsights() {

        this.http.get<AIInsightPayload[]>(`${this.apiUrl}/signalpulse/insights`).subscribe({
           
            next: insights => this.aiinsight.set(insights),

            error: err => {

                console.error('[QuoteService] Failed to load ai-insights', err);
                this.aiinsight.set([]);
            }
        });
    }

    // --- API methods --- 
    
    getAIInsights = () => this.http.get<AIInsightPayload[]>(`${this.apiUrl}/signalpulse/insights`);
}