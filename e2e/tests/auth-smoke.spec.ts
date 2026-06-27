import { expect, test } from '@playwright/test';

test.describe('Auth smoke flow', () => {
  test('sign in loads the dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill('admin@taxplatform.local');
    await page.getByLabel(/password/i).fill('Admin123!');
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page).toHaveURL(/dashboard/);
    await expect(page.getByText(/dashboard/i).first()).toBeVisible();
  });

  test('login page exposes password recovery entry point', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('link', { name: /forgot password/i }).click();

    await expect(page).toHaveURL(/forgot-password/);
    await expect(page.getByRole('heading', { name: /request a reset token/i })).toBeVisible();
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /request reset token/i })).toBeVisible();
  });
});
