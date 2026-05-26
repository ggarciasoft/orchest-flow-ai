/**
 * Unit tests for the tenants API client methods in api.ts.
 * Uses fetch mocking to verify correct endpoint + payload construction.
 */

// Store original fetch
const originalFetch = global.fetch;

beforeEach(() => {
  // Set up localStorage mock
  Object.defineProperty(window, 'localStorage', {
    value: { getItem: jest.fn(() => 'mock-token'), setItem: jest.fn(), removeItem: jest.fn() },
    writable: true,
  });
});

afterEach(() => {
  global.fetch = originalFetch;
  jest.clearAllMocks();
});

/** Helper to mock fetch with a JSON response. */
function mockFetch(body: unknown, status = 200) {
  const bodyText = JSON.stringify(body);
  global.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    text: async () => bodyText,
    json: async () => body,
  } as Response);
}

describe('api.tenants', () => {
  // Lazy-import api so fetch mock is in place at call time
  const getApi = () => require('@/lib/api').api;

  it('create sends POST /api/tenants with name', async () => {
    const expected = { id: 'tid-1', name: 'Acme', createdAt: '2026-01-01T00:00:00Z' };
    mockFetch(expected);

    const result = await getApi().tenants.create('Acme');

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/tenants'),
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ name: 'Acme' }) })
    );
    expect(result).toEqual(expected);
  });

  it('invite sends POST /api/tenants/{id}/invite with email and role', async () => {
    const expected = { id: 'inv-1', tenantId: 'tid-1', email: 'bob@x.com', role: 'Editor', token: 'tok', expiresAt: '2026-01-02T00:00:00Z' };
    mockFetch(expected);

    const result = await getApi().tenants.invite('tid-1', 'bob@x.com', 'Editor');

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/tenants/tid-1/invite'),
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ email: 'bob@x.com', role: 'Editor' }) })
    );
    expect(result).toEqual(expected);
  });

  it('acceptInvite sends POST /api/tenants/{id}/invite/accept with token and password', async () => {
    const expected = { message: 'Account created. You may now log in.' };
    mockFetch(expected);

    const result = await getApi().tenants.acceptInvite('tid-1', 'mytoken', 'mypassword');

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/tenants/tid-1/invite/accept'),
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ token: 'mytoken', password: 'mypassword' }) })
    );
    expect(result).toEqual(expected);
  });

  it('create throws error on non-2xx response', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 400,
      text: async () => JSON.stringify({ detail: 'Name too short' }),
      json: async () => ({ detail: 'Name too short' }),
    } as Response);

    await expect(getApi().tenants.create('')).rejects.toThrow('Name too short');
  });
});
