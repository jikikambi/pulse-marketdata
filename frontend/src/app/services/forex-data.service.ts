import { Injectable } from "@angular/core";
import { DefaultDataService, HttpUrlGenerator } from "@ngrx/data";
import { ForexPayload } from "../models/forex-payload.model";
import { ForexService } from "./forex.service";
import { Observable } from "rxjs";

@Injectable({ providedIn: 'root' })
export class ForexDataService extends DefaultDataService<ForexPayload> {

    constructor(private svc: ForexService, httpUrlGenerator: HttpUrlGenerator) {
        
        super('forex', svc['http'], httpUrlGenerator);
    }

    override getAll(): Observable<ForexPayload[]> {

        return this.svc.getForex();
    }
}