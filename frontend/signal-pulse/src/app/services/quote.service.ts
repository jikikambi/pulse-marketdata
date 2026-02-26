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

    // --- API methods ---

    getQuotes = () => this.http.get<QuoteCreatedPayload[]>(`${this.apiUrl}/signalpulse/quotes`);
}