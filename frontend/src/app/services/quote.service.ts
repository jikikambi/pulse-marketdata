import { Injectable } from "@angular/core";
import { QuotePayload } from "../models/quote-payload.model";
import { API_ENDPOINTS } from "./constants";
import { SignalPulseDataService } from "./signal-pulse-data.service";

@Injectable({ providedIn: 'root' })
export class QuoteService extends SignalPulseDataService<QuotePayload> {

    loadQuotes = () => this.load(API_ENDPOINTS.quotes);
    
    getQuotes = () => this.get(API_ENDPOINTS.quotes);
}