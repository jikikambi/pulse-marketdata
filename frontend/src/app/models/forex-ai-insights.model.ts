export interface ForexAIInsightPayload {
    id: string;
    fromSymbol: string;
    toSymbol: string;
    open: number;
    high: number;
    low: number;
    close: number;
    forexDate: string;
    sentiment: string;
    direction: string;
    volatility: string;
    rationale: string;
    observedAt: string;
}