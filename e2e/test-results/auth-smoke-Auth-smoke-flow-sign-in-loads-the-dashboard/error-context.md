# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: auth-smoke.spec.ts >> Auth smoke flow >> sign in loads the dashboard
- Location: tests\auth-smoke.spec.ts:4:7

# Error details

```
Error: page.goto: net::ERR_CONNECTION_REFUSED at http://localhost:4200/login
Call log:
  - navigating to "http://localhost:4200/login", waiting until "load"

```

# Test source

```ts
  1  | import { expect, test } from '@playwright/test';
  2  | 
  3  | test.describe('Auth smoke flow', () => {
  4  |   test('sign in loads the dashboard', async ({ page }) => {
> 5  |     await page.goto('/login');
     |                ^ Error: page.goto: net::ERR_CONNECTION_REFUSED at http://localhost:4200/login
  6  |     await page.getByLabel(/email/i).fill('admin@taxplatform.local');
  7  |     await page.getByLabel(/password/i).fill('Admin123!');
  8  |     await page.getByRole('button', { name: /sign in/i }).click();
  9  | 
  10 |     await expect(page).toHaveURL(/dashboard/);
  11 |     await expect(page.getByText(/dashboard/i).first()).toBeVisible();
  12 |   });
  13 | 
  14 |   test('login page exposes password recovery entry point', async ({ page }) => {
  15 |     await page.goto('/login');
  16 |     await page.getByRole('link', { name: /forgot password/i }).click();
  17 | 
  18 |     await expect(page).toHaveURL(/forgot-password/);
  19 |     await expect(page.getByRole('heading', { name: /request a reset token/i })).toBeVisible();
  20 |     await expect(page.getByLabel(/email/i)).toBeVisible();
  21 |     await expect(page.getByRole('button', { name: /request reset token/i })).toBeVisible();
  22 |   });
  23 | });
  24 | 
```