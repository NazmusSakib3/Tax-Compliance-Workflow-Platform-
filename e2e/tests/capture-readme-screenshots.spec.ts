import { expect, test } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:8080/api';
const screenshotsDir = path.resolve(__dirname, '../../docs/screenshots');

async function signIn(page: import('@playwright/test').Page) {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill('admin@taxplatform.local');
  await page.getByLabel(/password/i).fill('Admin123!');
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/dashboard/);
}

async function seedDemoData(request: import('@playwright/test').APIRequestContext) {
  const loginResponse = await request.post(`${apiBaseUrl}/auth/login`, {
    data: { email: 'admin@taxplatform.local', password: 'Admin123!' }
  });
  expect(loginResponse.ok()).toBeTruthy();
  const { data } = await loginResponse.json();
  const authHeaders = { Authorization: `Bearer ${data.accessToken as string}` };

  const orgResponse = await request.post(`${apiBaseUrl}/organizations`, {
    headers: authHeaders,
    data: {
      name: `Demo Org ${Date.now()}`,
      code: `DEMO${Date.now().toString().slice(-4)}`,
      description: 'README screenshot demo organization',
      isActive: true
    }
  });
  expect(orgResponse.ok()).toBeTruthy();
  const organization = await orgResponse.json();
  const orgHeaders = { ...authHeaders, 'X-Organization-Id': organization.id as string };

  const jurisdictionResponse = await request.post(`${apiBaseUrl}/jurisdictions`, {
    headers: orgHeaders,
    data: {
      name: 'United States - New York',
      countryCode: 'US',
      regionCode: 'NY',
      filingAuthority: 'NY Department of Taxation',
      isActive: true
    }
  });
  expect(jurisdictionResponse.ok()).toBeTruthy();
  const jurisdiction = await jurisdictionResponse.json();

  const templateResponse = await request.post(`${apiBaseUrl}/compliance-templates`, {
    headers: orgHeaders,
    data: {
      name: 'Quarterly VAT Return',
      filingType: 'VAT',
      description: 'Standard quarterly VAT filing template',
      reminderDaysBeforeDue: 7,
      isActive: true
    }
  });
  expect(templateResponse.ok()).toBeTruthy();
  const template = await templateResponse.json();

  const legalEntityResponse = await request.post(`${apiBaseUrl}/legal-entities`, {
    headers: orgHeaders,
    data: {
      organizationId: organization.id,
      name: 'Acme Holdings LLC',
      registrationNumber: `REG-${Date.now()}`,
      taxIdentifier: `TAX-${Date.now()}`,
      isActive: true
    }
  });
  expect(legalEntityResponse.ok()).toBeTruthy();
  const legalEntity = await legalEntityResponse.json();

  const ruleResponse = await request.post(`${apiBaseUrl}/compliance-task-rules`, {
    headers: orgHeaders,
    data: {
      legalEntityId: legalEntity.id,
      jurisdictionId: jurisdiction.id,
      complianceTemplateId: template.id,
      title: 'Monthly VAT Filing',
      description: 'Recurring monthly VAT compliance task',
      recurrenceType: 1,
      dueDayOfMonth: 15,
      isActive: true
    }
  });
  expect(ruleResponse.ok()).toBeTruthy();

  const generateResponse = await request.post(`${apiBaseUrl}/compliance-task-occurrences/generate`, {
    headers: orgHeaders
  });
  expect(generateResponse.ok()).toBeTruthy();

  return { organization, orgHeaders };
}

test.describe('README screenshots', () => {
  test.beforeAll(() => {
    fs.mkdirSync(screenshotsDir, { recursive: true });
  });

  test('capture application screenshots', async ({ page, request }) => {
    test.setTimeout(180_000);

    await page.setViewportSize({ width: 1440, height: 900 });

    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(screenshotsDir, '01-login.png'), fullPage: true });

    await page.goto('/forgot-password');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(screenshotsDir, '02-forgot-password.png'), fullPage: true });

    await seedDemoData(request);

    await signIn(page);
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: /compliance dashboard/i })).toBeVisible({ timeout: 15_000 });
    await page.waitForLoadState('networkidle');

    const pages: Array<{ file: string; route: string; waitFor?: RegExp }> = [
      { file: '03-dashboard.png', route: '/dashboard', waitFor: /compliance dashboard/i },
      { file: '04-organizations.png', route: '/organizations', waitFor: /organizations/i },
      { file: '05-legal-entities.png', route: '/legal-entities', waitFor: /legal entities/i },
      { file: '06-jurisdictions.png', route: '/jurisdictions', waitFor: /jurisdictions/i },
      { file: '07-compliance-templates.png', route: '/compliance-templates', waitFor: /templates/i },
      { file: '08-task-rules.png', route: '/task-rules', waitFor: /task rules/i },
      { file: '09-task-occurrences.png', route: '/task-occurrences', waitFor: /task occurrences/i },
      { file: '10-audit-log.png', route: '/audit-log', waitFor: /audit log/i },
      { file: '11-users.png', route: '/users', waitFor: /users/i },
      { file: '12-account-security.png', route: '/account/security', waitFor: /account security|multi-factor/i }
    ];

    for (const entry of pages) {
      await page.goto(entry.route);
      if (entry.waitFor) {
        await expect(page.getByText(entry.waitFor).first()).toBeVisible({ timeout: 15_000 });
      }
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: path.join(screenshotsDir, entry.file), fullPage: true });
    }

    await page.goto('/task-occurrences');
    await expect(page.getByText(/task occurrences/i).first()).toBeVisible();
    const detailLink = page.getByRole('link', { name: /open details/i }).first();
    if (await detailLink.count()) {
      await detailLink.click();
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: path.join(screenshotsDir, '13-task-occurrence-detail.png'), fullPage: true });
    }

    await page.goto('http://localhost:8080/swagger');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(screenshotsDir, '14-api-swagger.png'), fullPage: true });
  });
});
