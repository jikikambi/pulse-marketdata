import { test } from '../fixtures';

test.describe('App Component', () => {

  test('quotes load correctly', async ({ app }) => {

    await app.expectTitleVisible();

    await app.expectQuotesCardVisible();

    await app.expectDashboardVisible();
  });

});