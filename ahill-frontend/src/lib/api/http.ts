import { browser } from '$app/environment';

// Базовый URL бэкенда (ASP.NET Core).
const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:8080';

const TOKEN_KEY = 'vetcrm_token';

export function getToken(): string | null {
  return browser ? localStorage.getItem(TOKEN_KEY) : null;
}

export function setToken(token: string | null) {
  if (!browser) return;
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

/**
 * Обёртка над fetch: подставляет JWT в заголовок Authorization,
 * обрабатывает 401 (сброс токена + переход на /login) и ошибки сервера.
 */
export async function api<T = unknown>(path: string, opts: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((opts.headers as Record<string, string>) ?? {})
  };
  const token = getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(`${BASE}${path}`, { ...opts, headers });

  // 401 на запросе С токеном = истёкшая/недействительная сессия → выход.
  // 401 без токена (например, /auth/login) = неверные данные → пробрасываем сообщение сервера ниже.
  if (res.status === 401 && token) {
    setToken(null);
    if (browser && location.pathname !== '/login') location.href = '/login';
    throw new Error('Сессия истекла. Пожалуйста, войдите снова.');
  }

  if (!res.ok) {
    let message = `Ошибка сервера (${res.status})`;
    try {
      const body = await res.json();
      if (body?.message) message = body.message;
    } catch {
      /* тело не JSON — оставляем общее сообщение */
    }
    throw new Error(message);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
