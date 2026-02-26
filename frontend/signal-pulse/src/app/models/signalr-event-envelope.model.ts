import { SignalREventHandlerMap } from "../services/helper/signalr-event-handlers";

export interface SignalREventEnvelope<T = any> {
    eventId: string;
    // type: string; // e.g. "quote.created"
    type: keyof SignalREventHandlerMap;
    sequence: number;     // backend ordering value
    timestamp: string;    // ISO 8601 string
    payload: T;
}