import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { QuotePayload } from "../models/quote-payload.model";
import { QuoteService } from "./quote.service";
import { Observable } from "rxjs";

@Injectable()
export class QuoteDataService extends DefaultDataService<QuotePayload> {

    constructor(private svc: QuoteService, httpUrlGenerator: HttpUrlGenerator) {

        super('quote', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<QuotePayload[]> {

        return this.svc.getQuotes();
    }
}