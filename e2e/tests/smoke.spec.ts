import { expect, test } from '@playwright/test';

const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:8080/api';

test.describe('Tax Compliance smoke flow', () => {
  test('login through UI and complete generated task via API', async ({ page, request }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill('admin@taxplatform.local');
    await page.getByLabel(/password/i).fill('Admin123!');
    await page.getByRole('button', { name: /sign in|log in/i }).click();
    await expect(page).toHaveURL(/dashboard/);

    const loginResponse = await request.post(`${apiBaseUrl}/auth/login`, {
      data: { email: 'admin@taxplatform.local', password: 'Admin123!' }
    });
    expect(loginResponse.ok()).toBeTruthy();
    const loginPayload = await loginResponse.json();
    const token = loginPayload.data.accessToken as string;
    const authHeaders = { Authorization: `Bearer ${token}` };

    const orgResponse = await request.post(`${apiBaseUrl}/organizations`, {
      headers: authHeaders,
      data: {
        name: `Playwright Org ${Date.now()}`,
        code: `PW${Date.now().toString().slice(-4)}`,
        description: 'Playwright smoke org',
        isActive: true
      }
    });
    expect(orgResponse.ok()).toBeTruthy();
    const organization = await orgResponse.json();

    // The seeded admin is a platform admin with no default organization, so
    // organization-scoped writes require the same X-Organization-Id header the
    // web app sends after an organization is selected.
    const orgHeaders = { ...authHeaders, 'X-Organization-Id': organization.id as string };

    const jurisdictionResponse = await request.post(`${apiBaseUrl}/jurisdictions`, {
      headers: orgHeaders,
      data: {
        name: 'Playwright Jurisdiction',
        countryCode: 'US',
        regionCode: 'NY',
        filingAuthority: 'NY Tax Dept',
        isActive: true
      }
    });
    expect(jurisdictionResponse.ok()).toBeTruthy();
    const jurisdiction = await jurisdictionResponse.json();

    const templateResponse = await request.post(`${apiBaseUrl}/compliance-templates`, {
      headers: orgHeaders,
      data: {
        name: `Playwright Template ${Date.now()}`,
        filingType: 'VAT',
        description: 'Playwright template',
        reminderDaysBeforeDue: 5,
        isActive: true
      }
    });
    expect(templateResponse.ok()).toBeTruthy();
    const template = await templateResponse.json();

    const legalEntityResponse = await request.post(`${apiBaseUrl}/legal-entities`, {
      headers: orgHeaders,
      data: {
        organizationId: organization.id,
        name: 'Playwright Legal Entity',
        registrationNumber: `PW-REG-${Date.now()}`,
        taxIdentifier: `PW-TAX-${Date.now()}`,
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
        title: 'Playwright Monthly Rule',
        description: 'Generated in Playwright smoke test',
        recurrenceType: 1,
        dueDayOfMonth: 20,
        isActive: true
      }
    });
    expect(ruleResponse.ok()).toBeTruthy();

    const generateResponse = await request.post(`${apiBaseUrl}/compliance-task-occurrences/generate`, {
      headers: orgHeaders
    });
    expect(generateResponse.ok()).toBeTruthy();

    const occurrencesResponse = await request.get(`${apiBaseUrl}/compliance-task-occurrences?page=1&pageSize=5`, {
      headers: orgHeaders
    });
    expect(occurrencesResponse.ok()).toBeTruthy();
    const occurrencesPayload = await occurrencesResponse.json();
    expect(occurrencesPayload.items.length).toBeGreaterThan(0);

    const occurrenceId = occurrencesPayload.items[0].id;
    const completeResponse = await request.post(`${apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/status`, {
      headers: orgHeaders,
      data: { status: 4 }
    });
    expect(completeResponse.ok()).toBeTruthy();

    await page.goto('/task-occurrences');
    await expect(page.getByRole('heading', { name: 'Task Occurrences' })).toBeVisible();
  });
});
