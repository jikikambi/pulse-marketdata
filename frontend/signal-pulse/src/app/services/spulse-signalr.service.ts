import { Injectable, signal } from "@angular/core";
import * as SignalR from '@microsoft/signalr';
import { SignalREvent } from "../models/signalr-event.model";
import { mapSignalREvent } from "../mappers/signal-event.mapper";
import { SignalREventEnvelope } from "../models/envelope.model";

@Injectable({ providedIn: 'root' })
export class SpSignalrService {

    private hub?: SignalR.HubConnection;

    /** Track seen events for dedupe with TTL */
    private readonly seen = new Map<string, number>();
    private readonly TTL_MS = 10 * 60 * 1000;

    /** Ordered queue for events waiting for missing sequence */
    private readonly queue = new Map<number, SignalREvent>();

    /** Last emitted sequence number */
    private lastSequence = 0;

    /** Expose events for Angular to subscribe */
    readonly genEvent = signal<SignalREvent | null>(null);

    private readonly eventsToSubscribe = [
        'quote.created',
        'quote.updated',
        'quote.ai.insight'
    ];

    /**
     * Starts a SignalR connection and registers listeners for client-related events from the server
     * @param hubUrl - The SignalR hub endpoint URL.
     * @param token  - Optional JWT token for authentication.
   */
    async start(hubUrl: string, token?: string) {

        this.hub = new SignalR.HubConnectionBuilder()
            .withUrl(hubUrl, { accessTokenFactory: () => token ?? '' })
            .withAutomaticReconnect()
            .configureLogging(SignalR.LogLevel.Information)
            .build();

        this.attachHandlers();

        await this.hub.start();

        this.hub.onreconnected(() => {
            this.resetState();
        });
    }

    attachHandlers = () => {

        for (const evtType of this.eventsToSubscribe) {
            //this.hub!.on(evtType, evt => this.handleEvent({ ...evt, type: evtType } as SignalREvent))
            this.hub!.on(evtType, evt => this.handleEvent(mapSignalREvent({ ...evt, type: evtType } as SignalREventEnvelope)));
        }
    }

    private handleEvent(evt: SignalREvent) {

        const now = Date.now();

        // --- TTL eviction ---
        for (const [id, ts] of this.seen)
            if (now - ts > this.TTL_MS) this.seen.delete(id);

        // --- Deduplication ---
        if (this.seen.has(evt.eventId)) return;

        this.seen.set(evt.eventId, now);

        // --- Sequence-based ordering ---
        const seq = evt.sequence;

        if (seq <= this.lastSequence) return;

        this.queue.set(seq, evt);

        this.flushQueue();
    }


    private flushQueue() {

        const sortedSeqs = Array.from(this.queue.keys()).sort((a, b) => a - b);

        for (const seq of sortedSeqs) {

            if (seq <= this.lastSequence) continue;

            const evt = this.queue.get(seq)!;

            this.queue.delete(seq);

            // Emit generic event
            this.genEvent.set(evt);

            this.lastSequence = seq;
        }
    }

    resetState() {
        this.seen.clear();
        this.queue.clear();
        this.lastSequence = 0;
        this.genEvent.set(null);
    }

    /**  Stops the SignalR connection if active. */
    stop() { return this.hub?.stop(); }
}