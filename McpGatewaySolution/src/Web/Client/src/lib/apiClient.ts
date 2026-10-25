const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

function getCsrfToken(): string | null {
  const match = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

let isRefreshing = false;
let refreshSubscribers: (() => void)[] = [];

const onRefreshed = (): void => {
  refreshSubscribers.forEach((cb) => cb());
  refreshSubscribers = [];
};

const subscribeRefresh = (cb: () => void): void => {
  refreshSubscribers.push(cb);
};

async function tryRefresh(): Promise<boolean> {
  const res = await fetch(`${API_BASE}/api/v1/auth/refresh`, {
    method: "POST",
    credentials: "include",
    headers: {
      "X-XSRF-TOKEN": getCsrfToken() ?? "",
    },
  });
  return res.ok;
}

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const csrfToken = getCsrfToken();

  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(csrfToken ? { "X-XSRF-TOKEN": csrfToken } : {}),
      ...init?.headers,
    },
  });

  if (res.status !== 401) {
    if (!res.ok) throw new Error(`API error: ${res.status}`);
    if (res.status === 204) return undefined as T;
    return res.json() as Promise<T>;
  }

  if (!isRefreshing) {
    isRefreshing = true;
    try {
      const ok = await tryRefresh();
      if (!ok) {
        window.location.href = "/login";
        throw new Error("Session expired");
      }
      onRefreshed();
    } finally {
      isRefreshing = false;
    }
  }

  return new Promise<T>((resolve, reject) => {
    subscribeRefresh(async () => {
      try {
        const retryRes = await fetch(`${API_BASE}${path}`, {
          ...init,
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
            ...(getCsrfToken() ? { "X-XSRF-TOKEN": getCsrfToken()! } : {}),
            ...init?.headers,
          },
        });
        if (!retryRes.ok) throw new Error(`API error: ${retryRes.status}`);
        if (retryRes.status === 204) resolve(undefined as T);
        else resolve((await retryRes.json()) as T);
      } catch (err) {
        reject(err);
      }
    });
  });
}
