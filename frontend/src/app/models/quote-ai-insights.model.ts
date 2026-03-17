export interface QuoteAIInsightPayload {
    id: string;
    symbol: string;
    price: number;
    sentiment: string;
    direction: string;
    volatility: string;
    rationale: string;
    observedAt: string;
}