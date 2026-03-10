import { inject, Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class ConfigService {

    private readonly http = inject(HttpClient);

    private loaded = false;

    private _config!: { signalRHubUrl: string };

    /** Consumers wait for this */
    readonly ready = signal(false);

    get config() {

        if (!this.loaded) throw new Error('Config not loaded yet!');

        return this._config;
    }

    async load(): Promise<void> {
        if (this.loaded) return;

        this._config = await firstValueFrom(this.http.get<{ signalRHubUrl: string }>('/config'));

        console.log("[ConfigService] Loaded config:", this._config);

        this.loaded = true;
        this.ready.set(true);
    }
}
