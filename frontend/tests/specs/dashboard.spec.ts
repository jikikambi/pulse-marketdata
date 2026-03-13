import { test } from '../fixtures';

test('dashboard cards render', async ({ dashboard }) => {
    await dashboard.expectHotStocksVisible();
    await dashboard.expectWinnersVisible();
    await dashboard.expectLosersVisible();
    await dashboard.expectMarketSentimentVisible();
    await dashboard.expectInsightFeedVisible();
    await dashboard.expectQuotesGridVisible();
});