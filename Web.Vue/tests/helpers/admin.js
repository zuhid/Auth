// Seeded admin account (Auth/Dataload/Entities/User.csv). The password is a local seed credential;
// override it with E2E_ADMIN_PASSWORD when running against a differently seeded database.
import { expect } from '@playwright/test';

export const API_BASE = 'http://localhost:5101';
export const ADMIN_EMAIL = 'admin@example.com';
export const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'P@ssw0rd';

/** Logs in as the seeded admin through the API and returns the JWT. Fails the test if login fails. */
export async function loginAsAdmin(request) {
  const res = await request.post(`${API_BASE}/account/login`, {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  expect(res.ok(), `Admin login failed (${res.status()}): ${await res.text()}. Set E2E_ADMIN_PASSWORD if the seed differs.`).toBeTruthy();
  const { token } = await res.json();
  expect(token, 'Admin login returned no token').toBeTruthy();
  return token;
}
