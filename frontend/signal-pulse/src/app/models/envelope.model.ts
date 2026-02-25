export interface SignalREventEnvelope<T = any> {
    eventId: string;
    type: string;
    sequence: number;     // backend ordering value
    timestamp: string;    // ISO 8601 string
    payload: T;
}