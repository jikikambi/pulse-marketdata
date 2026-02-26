import { effect, inject, Injectable } from "@angular/core";
import { toSignal } from '@angular/core/rxjs-interop';
import { SpSignalrService } from "./spulse-signalr.service";
import { EntityCollectionServiceFactory } from "@ngrx/data";
import { QuoteCreatedPayload } from "../models/quote-created.model";
import { buildSignalREventHandlers } from "./helper/signalr-event-handlers";
import { ConfigService } from "./config.service";

@Injectable({ providedIn: 'root' })
export class SpSignalrSyncService {

    private readonly ecf = inject(EntityCollectionServiceFactory);

    private readonly quoteSvc = this.ecf.create<QuoteCreatedPayload>('Quote');

    private readonly cfgSvc = inject(ConfigService);

    private readonly signalr = inject(SpSignalrService);

    /** Live NgRx-backed projection */
    readonly quoteSig = toSignal(this.quoteSvc.entities$, { initialValue: [] as QuoteCreatedPayload[] })

    constructor() {

        console.log('[SpSignalrSyncService] ctor: setting up effects...');

        effect(async () => {

            if (!this.cfgSvc.ready()) return;

            console.log('[SpSignalrSyncService] Config ready, starting SignalR...');

            await this.signalr.start(this.cfgSvc.config!.signalRHubUrl);

            console.log('[SignalR] connected and syncing...');
        });

        effect(() => {

            const evt = this.signalr.genEvent();

            if (!evt) return;

            // load mapping table from helper
            const handlers = buildSignalREventHandlers({ quoteSvc: this.quoteSvc, /*insightSvc: this.insightSvc*/ });

            const handler = handlers[evt.type];

            if (handler) handler(evt.payload);
        });
        
        this.cfgSvc.load();
    }
}