import { test, expect } from '@playwright/test';

test('app loads successfully', async ({ page }) => {
  await page.goto('/');
  console.log(await page.url());
  await expect(page).toHaveTitle(/SignalPulse/);
});