import { Page, expect } from '@playwright/test';
import type { AppFixtures } from './app.fixture';
import type { ApiFixtures } from './api.fixture';

export type DashboardFixtures = {
    dashboard: {
        expectHotStocksVisible: () => Promise<void>;
        expectWinnersVisible: () => Promise<void>;
        expectLosersVisible: () => Promise<void>;
        expectMarketSentimentVisible: () => Promise<void>;
        expectInsightFeedVisible: () => Promise<void>;
        expectQuotesGridVisible: () => Promise<void>;
    };
};

export const dashboardFixture = async ({ page, app: _app, mockSignalPulseApi: _mock }: { page: Page } & AppFixtures & ApiFixtures, use: (dashboard: DashboardFixtures['dashboard']) => Promise<void>) => {
    
    const dashboard = {
        expectHotStocksVisible: async () => {
            await expect(page.locator('mat-card.hot-card mat-card-title')).toHaveText('🔥 Hot Stocks');
        },
        expectWinnersVisible: async () => {
            await expect(page.locator('mat-card.half-card:has-text("🟢 Winners")')).toBeVisible();
        },
        expectLosersVisible: async () => {
            await expect(page.locator('mat-card.half-card:has-text("🔴 Losers")')).toBeVisible();
        },
        expectMarketSentimentVisible: async () => {
            await expect(page.locator('mat-card.sentiment-card mat-card-title')).toHaveText('📊 Market Sentiment');
        },
        expectInsightFeedVisible: async () => {
            await expect(page.locator('mat-card.insight-card mat-card-title')).toHaveText('🧠 AI Insight Feed');
        },
        expectQuotesGridVisible: async () => {
            await expect(page.locator('mat-card.quote-list-card ag-grid-angular')).toBeVisible();
        }
    };

    await use(dashboard);
};