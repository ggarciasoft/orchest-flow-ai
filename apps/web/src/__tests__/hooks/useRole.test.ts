import { decodeJwt, getRoleFromToken, UserRole } from '@/lib/auth';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Builds a minimal JWT (unsigned) with the given payload.
 * The token is structurally valid but has no real signature — sufficient for
 * client-side decoding tests.
 */
function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fakesig`;
}

// ---------------------------------------------------------------------------
// decodeJwt
// ---------------------------------------------------------------------------

describe('decodeJwt', () => {
  it('returns null for an empty string', () => {
    expect(decodeJwt('')).toBeNull();
  });

  it('returns null for a malformed token (wrong number of parts)', () => {
    expect(decodeJwt('only.two')).toBeNull();
  });

  it('decodes a valid JWT and returns the payload', () => {
    const token = makeJwt({ sub: 'user-1', email: 'alice@example.com' });
    const result = decodeJwt(token);
    expect(result).not.toBeNull();
    expect(result!['sub']).toBe('user-1');
    expect(result!['email']).toBe('alice@example.com');
  });
});

// ---------------------------------------------------------------------------
// getRoleFromToken
// ---------------------------------------------------------------------------

describe('getRoleFromToken', () => {
  let getItemSpy: jest.SpyInstance;

  beforeEach(() => {
    // Jest's jsdom provides a real localStorage — spy on getItem so tests are isolated
    getItemSpy = jest.spyOn(Storage.prototype, 'getItem');
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('returns null when no token is stored', () => {
    getItemSpy.mockReturnValue(null);
    expect(getRoleFromToken()).toBeNull();
  });

  it('returns null for a token without a role claim', () => {
    const token = makeJwt({ sub: 'user-1' });
    getItemSpy.mockReturnValue(token);
    expect(getRoleFromToken()).toBeNull();
  });

  it('returns Viewer role from the short "role" claim', () => {
    const token = makeJwt({ sub: 'user-1', role: 'Viewer' });
    getItemSpy.mockReturnValue(token);
    expect(getRoleFromToken()).toBe<UserRole>('Viewer');
  });

  it('returns Editor role from the short "role" claim', () => {
    const token = makeJwt({ sub: 'user-2', role: 'Editor' });
    getItemSpy.mockReturnValue(token);
    expect(getRoleFromToken()).toBe<UserRole>('Editor');
  });

  it('returns Admin role from the short "role" claim', () => {
    const token = makeJwt({ sub: 'user-3', role: 'Admin' });
    getItemSpy.mockReturnValue(token);
    expect(getRoleFromToken()).toBe<UserRole>('Admin');
  });

  it('returns role from the long ASP.NET Core URN claim when present', () => {
    const token = makeJwt({
      sub: 'user-4',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
    });
    getItemSpy.mockReturnValue(token);
    expect(getRoleFromToken()).toBe<UserRole>('Admin');
  });

  it('prefers the long URN claim over the short "role" claim', () => {
    const token = makeJwt({
      sub: 'user-5',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
      role: 'Viewer',
    });
    getItemSpy.mockReturnValue(token);
    // Long URN claim takes precedence (nullish coalescing order in auth.ts)
    expect(getRoleFromToken()).toBe<UserRole>('Admin');
  });
});
