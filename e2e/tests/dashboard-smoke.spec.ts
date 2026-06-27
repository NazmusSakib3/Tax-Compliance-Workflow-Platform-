import { expect, test } from '@playwright/test';

test.describe('Dashboard smoke flow', () => {
  test('admin can export compliance CSV from dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill('admin@taxplatform.local');
    await page.getByLabel(/password/i).fill('Admin123!');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/dashboard/);

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: /export csv report/i }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/compliance-status-\d{4}-\d{2}-\d{2}\.csv/);
  });
});
