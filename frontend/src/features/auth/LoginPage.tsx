import { useState } from 'react';
import type { FormEvent } from 'react';

interface LoginPageProps {
  onLoginSuccess: () => Promise<void> | void;
}

export function LoginPage({ onLoginSuccess }: LoginPageProps) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const response = await fetch('/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({
          username: username.trim(),
          password,
        }),
      });

      if (!response.ok) {
        setError('Forkert brugernavn eller adgangskode.');
        return;
      }

      await onLoginSuccess();
    } catch {
      setError('Kunne ikke logge ind. Prøv igen.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-card card border-0 shadow-sm">
        <div className="card-body">
          <h1 className="h3 mb-3">Log ind</h1>
          <p className="text-muted">Du skal logge ind for at bruge pallesortering.</p>

          <form onSubmit={submitLogin}>
            <div className="mb-3">
              <label htmlFor="username" className="form-label">Brugernavn</label>
              <input
                id="username"
                className="form-control"
                autoComplete="username"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                required
              />
            </div>
            <div className="mb-3">
              <label htmlFor="password" className="form-label">Adgangskode</label>
              <input
                id="password"
                type="password"
                className="form-control"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </div>
            {error && <div className="alert alert-danger py-2" role="alert">{error}</div>}
            <button type="submit" className="btn btn-primary w-100" disabled={submitting}>
              {submitting ? 'Logger ind...' : 'Log ind'}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}
