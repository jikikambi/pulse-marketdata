import { Injectable, computed, inject } from '@angular/core';
import { SpSignalrSyncService } from '../services/spulse-signalr-sync.service';
import { QuoteAIInsightPayload } from '../models/quote-ai-insights.model';
import { QuotePayload } from '../models/quote-payload.model';

@Injectable({ providedIn: 'root' })
export class DashboardStore {

    private readonly sync = inject(SpSignalrSyncService);

    readonly quotes = this.sync.quoteSig;
    readonly insights = this.sync.aiInsightSig;

    readonly dashboardData = computed(() => this.computeDashboardData());

    readonly hotStocks = computed(() => this.dashboardData().hotStocks);

    readonly winners = computed(() => this.dashboardData().winners);

    readonly losers = computed(() => this.dashboardData().losers);

    readonly insightFeed = computed(() => this.dashboardData().insightFeed);

    readonly sentimentSummary = computed(() => this.dashboardData().sentimentSummary);

    private computeDashboardData() {

        const quotes = this.quotes();

        const insights = this.insights();

        const merged = quotes.map(q => ({
            ...q,
            insight: insights.find(i => i.symbol === q.symbol) || null,
            sentimentScore: this.getSentimentScore(q.symbol, insights),
            hotScore: this.computeHotScore(q.symbol, quotes, insights)
        }));

        return {

            hotStocks: [...merged].sort((a, b) => b.hotScore - a.hotScore).slice(0, 10),

            winners: [...merged].filter(x => x.changePercent > 0)
                .sort((a, b) => b.changePercent - a.changePercent)
                .slice(0, 5),

            losers: [...merged].filter(x => x.changePercent < 0)
                .sort((a, b) => b.changePercent - a.changePercent)
                .slice(0, 5),

            insightFeed: [...insights].sort((a, b) => new Date(b.observedAt).getTime() - new Date(a.observedAt).getTime()),

            sentimentSummary: {
                bullish: insights.filter(i => i.sentiment === 'bullish').length,
                neutral: insights.filter(i => i.sentiment === 'neutral').length,
                bearish: insights.filter(i => i.sentiment === 'bearish').length
            }
        };
    }

    private getSentimentScore(symbol: string, insights: QuoteAIInsightPayload[]): number {

        const insight = insights.find(i => i.symbol === symbol);

        if (!insight) return 0;

        switch (insight.sentiment.toLowerCase()) {
            case 'bullish': return 2;
            case 'bearish': return -2;
            default: return 0;
        }
    }

    private computeHotScore(symbol: string, quotes: QuotePayload[], insights: QuoteAIInsightPayload[]): number {

        const q = quotes.find(x => x.symbol === symbol);

        const insight = insights.find(i => i.symbol === symbol);

        if (!q) return 0;

        const momentum = q.changePercent;

        const sentiment = this.getSentimentScore(symbol, insights);

        const volatility = insight?.volatility === 'high' ? 1 : 0;

        const directionBonus =
            insight?.direction === 'up' ? 1 :
                insight?.direction === 'down' ? -1 : 0;

        return momentum * 2 + sentiment * 3 + volatility * 2 + directionBonus;
    }
}