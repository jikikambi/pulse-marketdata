import { HttpClient } from "@angular/common/http";
import { inject, Injectable, signal } from "@angular/core";
import { QuotePayload } from "../models/quote-payload.model";
import { BaseService } from "./base.service";
import { API_ENDPOINTS } from "./constants";

@Injectable({providedIn: 'root'})
export class QuoteService extends BaseService {

    private readonly http = inject(HttpClient);

    /** Writable signal for asset list */
    readonly quotes = signal<QuotePayload[]>([]);

    constructor() {
        super();
    }

    loadQuotes() {

        this.http.get<QuotePayload[]>(`${this.apiUrl}${API_ENDPOINTS.quotes}`).subscribe({
            
            next: quotes => this.quotes.set(quotes),

            error: err => {
                
                console.error('[QuoteService] Failed to load quotes', err);
                this.quotes.set([]);
            }
        });
    }

    // --- API methods ---

    getQuotes = () => this.http.get<QuotePayload[]>(`${this.apiUrl}${API_ENDPOINTS.quotes}`);
} 