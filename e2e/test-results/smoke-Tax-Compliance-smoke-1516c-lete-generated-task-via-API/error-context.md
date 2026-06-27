# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: smoke.spec.ts >> Tax Compliance smoke flow >> login through UI and complete generated task via API
- Location: tests\smoke.spec.ts:6:7

# Error details

```
Error: page.goto: net::ERR_CONNECTION_REFUSED at http://localhost:4200/login
Call log:
  - navigating to "http://localhost:4200/login", waiting until "load"

```

# Test source

```ts
  1   | import { expect, test } from '@playwright/test';
  2   | 
  3   | const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:8080/api';
  4   | 
  5   | test.describe('Tax Compliance smoke flow', () => {
  6   |   test('login through UI and complete generated task via API', async ({ page, request }) => {
> 7   |     await page.goto('/login');
      |                ^ Error: page.goto: net::ERR_CONNECTION_REFUSED at http://localhost:4200/login
  8   |     await page.getByLabel(/email/i).fill('admin@taxplatform.local');
  9   |     await page.getByLabel(/password/i).fill('Admin123!');
  10  |     await page.getByRole('button', { name: /sign in|log in/i }).click();
  11  |     await expect(page).toHaveURL(/dashboard/);
  12  | 
  13  |     const loginResponse = await request.post(`${apiBaseUrl}/auth/login`, {
  14  |       data: { email: 'admin@taxplatform.local', password: 'Admin123!' }
  15  |     });
  16  |     expect(loginResponse.ok()).toBeTruthy();
  17  |     const loginPayload = await loginResponse.json();
  18  |     const token = loginPayload.data.accessToken as string;
  19  |     const authHeaders = { Authorization: `Bearer ${token}` };
  20  | 
  21  |     const orgResponse = await request.post(`${apiBaseUrl}/organizations`, {
  22  |       headers: authHeaders,
  23  |       data: {
  24  |         name: `Playwright Org ${Date.now()}`,
  25  |         code: `PW${Date.now().toString().slice(-4)}`,
  26  |         description: 'Playwright smoke org',
  27  |         isActive: true
  28  |       }
  29  |     });
  30  |     expect(orgResponse.ok()).toBeTruthy();
  31  |     const organization = await orgResponse.json();
  32  | 
  33  |     const jurisdictionResponse = await request.post(`${apiBaseUrl}/jurisdictions`, {
  34  |       headers: authHeaders,
  35  |       data: {
  36  |         name: 'Playwright Jurisdiction',
  37  |         countryCode: 'US',
  38  |         regionCode: 'NY',
  39  |         filingAuthority: 'NY Tax Dept',
  40  |         isActive: true
  41  |       }
  42  |     });
  43  |     expect(jurisdictionResponse.ok()).toBeTruthy();
  44  |     const jurisdiction = await jurisdictionResponse.json();
  45  | 
  46  |     const templateResponse = await request.post(`${apiBaseUrl}/compliance-templates`, {
  47  |       headers: authHeaders,
  48  |       data: {
  49  |         name: `Playwright Template ${Date.now()}`,
  50  |         filingType: 'VAT',
  51  |         description: 'Playwright template',
  52  |         reminderDaysBeforeDue: 5,
  53  |         isActive: true
  54  |       }
  55  |     });
  56  |     expect(templateResponse.ok()).toBeTruthy();
  57  |     const template = await templateResponse.json();
  58  | 
  59  |     const legalEntityResponse = await request.post(`${apiBaseUrl}/legal-entities`, {
  60  |       headers: authHeaders,
  61  |       data: {
  62  |         organizationId: organization.id,
  63  |         name: 'Playwright Legal Entity',
  64  |         registrationNumber: `PW-REG-${Date.now()}`,
  65  |         taxIdentifier: `PW-TAX-${Date.now()}`,
  66  |         isActive: true
  67  |       }
  68  |     });
  69  |     expect(legalEntityResponse.ok()).toBeTruthy();
  70  |     const legalEntity = await legalEntityResponse.json();
  71  | 
  72  |     const ruleResponse = await request.post(`${apiBaseUrl}/compliance-task-rules`, {
  73  |       headers: authHeaders,
  74  |       data: {
  75  |         legalEntityId: legalEntity.id,
  76  |         jurisdictionId: jurisdiction.id,
  77  |         complianceTemplateId: template.id,
  78  |         title: 'Playwright Monthly Rule',
  79  |         description: 'Generated in Playwright smoke test',
  80  |         recurrenceType: 1,
  81  |         dueDayOfMonth: 20,
  82  |         isActive: true
  83  |       }
  84  |     });
  85  |     expect(ruleResponse.ok()).toBeTruthy();
  86  | 
  87  |     const generateResponse = await request.post(`${apiBaseUrl}/compliance-task-occurrences/generate`, {
  88  |       headers: authHeaders
  89  |     });
  90  |     expect(generateResponse.ok()).toBeTruthy();
  91  | 
  92  |     const occurrencesResponse = await request.get(`${apiBaseUrl}/compliance-task-occurrences?page=1&pageSize=5`, {
  93  |       headers: authHeaders
  94  |     });
  95  |     expect(occurrencesResponse.ok()).toBeTruthy();
  96  |     const occurrencesPayload = await occurrencesResponse.json();
  97  |     expect(occurrencesPayload.items.length).toBeGreaterThan(0);
  98  | 
  99  |     const occurrenceId = occurrencesPayload.items[0].id;
  100 |     const completeResponse = await request.post(`${apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/status`, {
  101 |       headers: authHeaders,
  102 |       data: { status: 4 }
  103 |     });
  104 |     expect(completeResponse.ok()).toBeTruthy();
  105 | 
  106 |     await page.goto('/task-occurrences');
  107 |     await expect(page.getByText(/task occurrences/i)).toBeVisible();
```