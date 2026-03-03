import { HttpClient } from "@angular/common/http";
import { inject, Injectable, signal } from "@angular/core";
import { QuoteCreatedPayload } from "../models/quote-created.model";
import { BaseService } from "./base.service";

@Injectable({providedIn: 'root'})
export class QuoteService extends BaseService {

    private readonly http = inject(HttpClient);

    /** Writable signal for asset list */
    readonly quotes = signal<QuoteCreatedPayload[]>([]);

    constructor() {
        super();
    }

    loadQuotes() {

        this.http.get<QuoteCreatedPayload[]>(`${this.apiUrl}/signalpulse/quotes`).subscribe({
            
            next: quotes => this.quotes.set(quotes),

            error: err => {
                
                console.error('[QuoteService] Failed to load quotes', err);
                this.quotes.set([]);
            }
        });
    }

    // --- API methods ---

    getQuotes = () => this.http.get<QuoteCreatedPayload[]>(`${this.apiUrl}/signalpulse/quotes`);
} 