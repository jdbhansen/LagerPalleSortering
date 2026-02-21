import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LoginPage } from './LoginPage';

describe('LoginPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('kalder onLoginSuccess ved gyldig login', async () => {
    const onLoginSuccess = vi.fn();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ authenticated: true, username: 'tester' }),
    }));

    render(<LoginPage onLoginSuccess={onLoginSuccess} />);

    fireEvent.change(screen.getByLabelText('Brugernavn'), { target: { value: 'tester' } });
    fireEvent.change(screen.getByLabelText('Adgangskode'), { target: { value: 'secret-123' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log ind' }));

    await waitFor(() => {
      expect(onLoginSuccess).toHaveBeenCalledTimes(1);
    });
  });

  it('viser fejl ved ugyldigt login', async () => {
    const onLoginSuccess = vi.fn();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      json: async () => ({}),
    }));

    render(<LoginPage onLoginSuccess={onLoginSuccess} />);

    fireEvent.change(screen.getByLabelText('Brugernavn'), { target: { value: 'tester' } });
    fireEvent.change(screen.getByLabelText('Adgangskode'), { target: { value: 'wrong' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log ind' }));

    expect(await screen.findByText('Forkert brugernavn eller adgangskode.')).toBeInTheDocument();
    expect(onLoginSuccess).not.toHaveBeenCalled();
  });
});
