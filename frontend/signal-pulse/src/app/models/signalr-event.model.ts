export interface SignalREvent<T = any> {
    eventId: string;    // Guid from .NET
    type: string;       // e.g. "quote.created"
    payload: T;         // event payload
    timestamp: string;  // ISO datetime from DateTimeOffset
    sequence: number;
} 
