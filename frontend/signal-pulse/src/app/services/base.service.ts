import { Injectable } from "@angular/core";
import { environment } from "../../environments/development";

@Injectable({providedIn: 'root'})
export class BaseService {
    apiUrl = environment.apiBaseUrl;
    signalrUrl = environment.signalREndipoint;
}