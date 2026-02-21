import { afterEach, describe, expect, it, vi } from 'vitest';
import { fetchAuthState, logoutCurrentUser } from './authApi';

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

    expect(fetchMock).toHaveBeenCalledWith('/auth/logout', {
      method: 'POST',
      credentials: 'include',
    });
  });
});
