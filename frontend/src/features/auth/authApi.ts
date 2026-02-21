export interface AuthState {
  loading: boolean;
  authenticated: boolean;
  username: string;
}

export async function fetchAuthState(): Promise<AuthState> {
  try {
    const response = await fetch('/auth/me', { credentials: 'include' });
    if (!response.ok) {
      return {
        loading: false,
        authenticated: false,
        username: '',
      };
    }

    const payload = (await response.json()) as { authenticated?: boolean; username?: string };
    return {
      loading: false,
      authenticated: payload.authenticated === true,
      username: payload.username ?? '',
    };
  } catch {
    return {
      loading: false,
      authenticated: false,
      username: '',
    };
  }
}

export async function logoutCurrentUser(): Promise<void> {
  await fetch('/auth/logout', {
    method: 'POST',
    credentials: 'include',
  });
}

