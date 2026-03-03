export interface QuoteCreatedPayload  {
    id: string;
    symbol: string;
    price: number;
    changePercent: number;
    timestamp: string;
}