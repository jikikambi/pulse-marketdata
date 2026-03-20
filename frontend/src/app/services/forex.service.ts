import { Injectable } from "@angular/core";
import { ForexPayload } from "../models/forex-payload.model";
import { API_ENDPOINTS } from "./constants";
import { SignalPulseDataService } from "./signal-pulse-data.service";

@Injectable({ providedIn: 'root' })
export class ForexService extends SignalPulseDataService<ForexPayload> {

    loadForex = () => this.load(API_ENDPOINTS.forex);

    getForex = () => this.get(API_ENDPOINTS.forex);
}