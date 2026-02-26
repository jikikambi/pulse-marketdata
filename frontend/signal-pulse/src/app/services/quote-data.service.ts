import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { QuoteCreatedPayload } from "../models/quote-created.model";
import { QuoteService } from "./quote.service";
import { Observable } from "rxjs";

@Injectable()
export class QuoteDataService extends DefaultDataService<QuoteCreatedPayload> {

    constructor(private svc: QuoteService, httpUrlGenerator: HttpUrlGenerator) {

        super('quote', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<QuoteCreatedPayload[]> {

        return this.svc.getQuotes();
    }
}