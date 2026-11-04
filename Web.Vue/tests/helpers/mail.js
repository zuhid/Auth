// Shared helpers for reading mail captured by MailHog (http://localhost:8025).
// Mirrors the MailHog helpers in Auth.Tests/Integration/AccountTest.cs.
//
// Messages are always looked up by recipient (and, for SMS, by phone number and send time),
// never by clearing the whole mailbox, so parallel workers and the .NET suite don't interfere.
import { expect } from '@playwright/test';

export const MAILHOG_BASE = 'http://localhost:8025';

// SMS messages from the Notification service land in this inbox; the Subject holds the phone number.
export const PHONE_INBOX = 'phone@test.com';

const header = (headers, name) => {
  const key = Object.keys(headers ?? {}).find(k => k.toLowerCase() === name.toLowerCase());
  return key ? headers[key][0] : undefined;
};

const decodeQuotedPrintable = (input) => {
  const soft = input.replace(/=\r?\n/g, '');
  const bytes = [];
  for (let i = 0; i < soft.length; i++) {
    const hex = soft.slice(i + 1, i + 3);
    if (soft[i] === '=' && /^[0-9A-Fa-f]{2}$/.test(hex)) {
      bytes.push(parseInt(hex, 16));
      i += 2;
    } else {
      bytes.push(...Buffer.from(soft[i], 'utf8'));
    }
  }
  return Buffer.from(bytes).toString('utf8');
};

const decodeBody = (body, headers) => {
  const encoding = header(headers, 'Content-Transfer-Encoding')?.toLowerCase();
  if (encoding === 'base64') return Buffer.from(body.replace(/\s+/g, ''), 'base64').toString('utf8');
  if (encoding === 'quoted-printable') return decodeQuotedPrintable(body);
  return body;
};

// Decodes a MailHog message into plain text: the single body, or every MIME part joined together.
// Allow for small clock differences between this machine and the MailHog container.
const CLOCK_SKEW_MS = 2_000;
const isSince = (msg, since) => !since || msg.created.getTime() >= since.getTime() - CLOCK_SKEW_MS;

const decodeMessage = (item) => {
  const parts = item.MIME?.Parts;
  if (parts?.length) {
    return parts.map(p => decodeBody(p.Body ?? '', p.Headers)).join('\n');
  }
  return decodeBody(item.Content?.Body ?? '', item.Content?.Headers);
};

/**
 * Returns decoded messages addressed to `to`, newest first.
 * Each entry: { id, subject, created (Date), body }.
 */
export async function getMessages(request, to) {
  const res = await request.get(`${MAILHOG_BASE}/api/v2/search`, {
    params: { kind: 'to', query: to, limit: 50 },
  });
  if (!res.ok()) return [];
  const { items } = await res.json();
  return (items ?? [])
    .map(item => ({
      id: item.ID,
      subject: header(item.Content?.Headers, 'Subject') ?? '',
      created: new Date(item.Created),
      body: decodeMessage(item),
    }))
    .sort((a, b) => b.created - a.created);
}

/**
 * Polls MailHog until a message to `to` matches `pattern` (and the optional `filter`),
 * then returns the first capture group (or the whole match when there is none).
 */
export async function waitForMatch(request, to, pattern, { filter = () => true, timeout = 10_000, message } = {}) {
  let result = null;
  await expect.poll(async () => {
    for (const msg of await getMessages(request, to)) {
      if (!filter(msg)) continue;
      const match = msg.body.match(pattern);
      if (match) return (result = match[1] ?? match[0]);
    }
    return null;
  }, { timeout, message: message ?? `No message to ${to} matching ${pattern} arrived in MailHog` }).toBeTruthy();
  return result;
}

/** Waits for the verification email sent on register and returns its app path (/account/verify-email/...). */
export const waitForVerifyPath = (request, email) =>
  waitForMatch(request, email, /\/account\/verify-email\/[^"'\s<]+/i, {
    message: `Verification email for ${email} not received in MailHog`,
  });

/** Waits for a 2FA code emailed to `email` (rendered in the HTML template as <span class="code">123456</span>). */
export const waitForEmailCode = (request, email, { since } = {}) =>
  waitForMatch(request, email, /class="code">\s*(\d+)\s*</i, {
    filter: msg => isSince(msg, since),
    message: `2FA email code for ${email} not received in MailHog`,
  });

/**
 * Waits for an SMS code sent to `phone`. SMS messages share the phone@test.com inbox,
 * so match on the phone number in the Subject plus a send-time cutoff instead of clearing the inbox.
 */
export const waitForSmsCode = (request, phone, { since } = {}) => {
  const digits = String(phone).replace(/\D/g, '');
  return waitForMatch(request, PHONE_INBOX, /code is: (\d+)/i, {
    filter: msg => msg.subject.replace(/\D/g, '') === digits && isSince(msg, since),
    message: `SMS code for ${phone} not received in MailHog`,
  });
};
