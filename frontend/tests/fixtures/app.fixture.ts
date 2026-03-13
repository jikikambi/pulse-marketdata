import { Page } from '@playwright/test';
import { AppPage } from '../pages/app.page';
import type { ApiFixtures } from './api.fixture';

export type AppFixtures = {
    app: AppPage;
};

export const appFixture = async ({ page, mockSignalPulseApi: _mock }: { page: Page } & ApiFixtures, use: (app: AppPage) => Promise<void>) => {
    
    const app = new AppPage(page);
    await app.goto();
    await use(app);
};