import { effect, inject, Injectable } from "@angular/core";
import { toSignal } from '@angular/core/rxjs-interop';
import { SpSignalrService } from "./spulse-signalr.service";
import { EntityCollectionServiceFactory } from "@ngrx/data";
import { QuotePayload } from "../models/quote-payload.model";
import { buildSignalREventHandlers } from "./helper/signalr-event-handlers";
import { ConfigService } from "./config.service";
import { QuoteAIInsightPayload } from "../models/quote-ai-insights.model";
import { ForexPayload } from "../models/forex-payload.model";
import { ForexAIInsightPayload } from "../models/forex-ai-insights.model";

@Injectable({ providedIn: 'root' })
export class SpSignalrSyncService {

    private readonly ecf = inject(EntityCollectionServiceFactory);

    private readonly quoteSvc = this.ecf.create<QuotePayload>('Quote');
    private readonly quoteInsightSvc = this.ecf.create<QuoteAIInsightPayload>('QuoteInsight');
    private readonly forexSvc = this.ecf.create<ForexPayload>('Forex');
    private readonly forexInsightSvc = this.ecf.create<ForexAIInsightPayload>('ForexInsight');

    private readonly cfgSvc = inject(ConfigService);

    private readonly signalr = inject(SpSignalrService);

    /** Live NgRx-backed projection */
    readonly quoteSig = toSignal(this.quoteSvc.entities$, { initialValue: [] as QuotePayload[] });

    readonly aiInsightSig = toSignal(this.quoteInsightSvc.entities$, { initialValue: [] as QuoteAIInsightPayload[] });

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
            const handlers = buildSignalREventHandlers({ quoteSvc: this.quoteSvc, quoteInsightSvc: this.quoteInsightSvc, forexSvc: this.forexSvc, forexAiInsightSvc: this.forexInsightSvc  });

            const handler = handlers[evt.type];

            if (handler) handler(evt.payload);
        });

        this.cfgSvc.load();
    }
}