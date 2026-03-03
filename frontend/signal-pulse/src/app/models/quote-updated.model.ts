export interface QuoteUpdatedPayload {
    id: string;
    symbol: string;
    price: number;
    timestamp: string;
    changePercent:number;
}