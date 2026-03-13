import { Page, Locator, expect } from '@playwright/test';

export class AppPage {

    readonly page: Page;
    readonly title: Locator;
    readonly quotesCard: Locator;
    readonly dashboard: Locator;

    constructor(page: Page) {
        this.page = page;

        this.title = page.getByText('Signal Pulse');
        this.quotesCard = page.locator('mat-card.quotes-card');
        this.dashboard = page.locator('app-dashboard');
    }

    async goto() {
        await this.page.goto('/');
    }

    async expectTitleVisible() {
        await expect(this.title).toBeVisible();
    }

    async expectQuotesCardVisible() {
        await expect(this.quotesCard).toBeVisible();
    }

    async expectDashboardVisible() {
        await expect(this.dashboard).toBeVisible();
    }
}