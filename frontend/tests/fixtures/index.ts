import { test as base, expect } from '@playwright/test';

import { mockSignalPulseApi, ApiFixtures } from './api.fixture';
import { appFixture, AppFixtures } from './app.fixture';
import { dashboardFixture, DashboardFixtures } from './dashboard.fixture';

export const test = base.extend<ApiFixtures & AppFixtures & DashboardFixtures>({
    mockSignalPulseApi,
    app: appFixture,
    dashboard: dashboardFixture
});

export { expect };