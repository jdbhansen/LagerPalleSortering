import { authApiRoutes } from './authApiRoutes';

export interface AuthState {
  loading: boolean;
  authenticated: boolean;
  username: string;
}

export interface LoginPayload {
  username: string;
  password: string;
}

export async function loginUser(payload: LoginPayload): Promise<boolean> {
  const response = await fetch(authApiRoutes.login, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include',
    body: JSON.stringify(payload),
  });

  return response.ok;
}

export async function fetchAuthState(): Promise<AuthState> {
  try {
    const response = await fetch(authApiRoutes.me, { credentials: 'include' });
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
  await fetch(authApiRoutes.logout, {
    method: 'POST',
    credentials: 'include',
  });
}
