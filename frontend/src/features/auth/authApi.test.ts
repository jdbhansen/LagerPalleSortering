import { afterEach, describe, expect, it, vi } from 'vitest';
import { fetchAuthState, loginUser, logoutCurrentUser } from './authApi';
import { authApiRoutes } from './authApiRoutes';

describe('authApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('fetchAuthState returnerer authenticated=true ved gyldigt svar', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ authenticated: true, username: 'tester' }),
    }));

    const state = await fetchAuthState();

    expect(state.authenticated).toBe(true);
    expect(state.username).toBe('tester');
  });

  it('fetchAuthState returnerer authenticated=false når endpoint fejler', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      json: async () => ({}),
    }));

    const state = await fetchAuthState();

    expect(state.authenticated).toBe(false);
    expect(state.username).toBe('');
  });

  it('logoutCurrentUser kalder logout endpoint med credentials', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal('fetch', fetchMock);

    await logoutCurrentUser();

    expect(fetchMock).toHaveBeenCalledWith(authApiRoutes.logout, {
      method: 'POST',
      credentials: 'include',
    });
  });

  it('loginUser sender login payload og returnerer response.ok', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal('fetch', fetchMock);

    const success = await loginUser({ username: 'admin', password: 'secret' });

    expect(success).toBe(true);
    expect(fetchMock).toHaveBeenCalledWith(authApiRoutes.login, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify({ username: 'admin', password: 'secret' }),
    });
  });
});
