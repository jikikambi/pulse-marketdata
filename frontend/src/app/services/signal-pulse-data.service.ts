import { inject, Injectable, signal } from "@angular/core";
import { BaseService } from "./base.service";
import { HttpClient } from "@angular/common/http";

@Injectable({ providedIn: 'root' })
export class SignalPulseDataService<T> extends BaseService {

    protected readonly http = inject(HttpClient);

    readonly data = signal<T[]>([]);

    protected load = (endpoint: string) => {

        this.http.get<T[]>(`${this.apiUrl}${endpoint}`).subscribe({

            next: data => this.data.set(data),
            error: err => {
                
                console.error(`[${this.constructor.name}] Failed`, err);
                this.data.set([]);
            }
        });
    }

    protected get = (endpoint: string) => this.http.get<T[]>(`${this.apiUrl}${endpoint}`);
}