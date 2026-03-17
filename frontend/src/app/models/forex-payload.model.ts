export interface ForexPayload {
    id: string;
    fromSymbol: string;
    toSymbol: string;
    open: number;
    high: number;
    low: number;
    close: number;
    forexDate: string;
    timestamp: string;
}