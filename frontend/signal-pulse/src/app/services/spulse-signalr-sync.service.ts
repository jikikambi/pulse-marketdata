import { effect, inject, Injectable } from "@angular/core";
import { toSignal } from '@angular/core/rxjs-interop';
import { BaseService } from "./base.service";
import { SpSignalrService } from "./spulse-signalr.service";
import { EntityCollectionServiceFactory } from "@ngrx/data";
import { QuoteCreatedPayload } from "../models/quote-created.model";
import { buildSignalREventHandlers } from "./helper/signalr-event-handlers";

@Injectable({ providedIn: 'root' })
export class SpSignalrSyncService extends BaseService {

    private readonly ecf = inject(EntityCollectionServiceFactory);

    private readonly quoteSvc = this.ecf.create<QuoteCreatedPayload>('Quote');

    private readonly signalr = inject(SpSignalrService);

    /** Live NgRx-backed projection */
    readonly quoteSig = toSignal(this.quoteSvc.entities$, { initialValue: [] as QuoteCreatedPayload[] })

    constructor() {
        super();
        this.initializeSignalR();
    }

    private async initializeSignalR() {

        console.log('[SpSignalrSyncService] initializing SignalR sync...');

        await this.signalr.start(`${this.signalrUrl}`);

        console.log('[SignalR] connected and syncing...');

        // load mapping table from helper
        const handlers = buildSignalREventHandlers({ quoteSvc: this.quoteSvc });

        // React to quote changes
        effect(async () => {

            const evt = this.signalr.genEvent();

            if (!evt) return;

            const handler = handlers[evt.type];

            if (handler) handler(evt.payload);
        });
    }
}