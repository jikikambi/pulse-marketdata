import { Page } from '@playwright/test';
import { mockQuotes, mockInsights } from '../mocks/signalpulse';

export type ApiFixtures = {
    mockSignalPulseApi: void;
};

export const mockSignalPulseApi = async ({ page }: { page: Page }, use: () => Promise<void>) => {

    await page.route('**/api/signalpulse/quotes', route =>
        route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([mockQuotes])
        })
    );

    await page.route('**/api/signalpulse/insights', route =>
        route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([mockInsights])
        })
    );

    await use();
};