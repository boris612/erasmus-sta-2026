import { auth0 } from '../auth';
import type { ProblemDetails } from './types';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7150';

function toCamelCase(value: string) {
  return value.length > 0 ? value[0].toLowerCase() + value.slice(1) : value;
}

function normalizeJson<T>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => normalizeJson(item)) as T;
  }

  if (value && typeof value === 'object' && !(value instanceof Date)) {
    const normalizedEntries = Object.entries(value as Record<string, unknown>).map(([key, entryValue]) => [
      toCamelCase(key),
      normalizeJson(entryValue)
    ]);

    return Object.fromEntries(normalizedEntries) as T;
  }

  return value;
}

function buildUrl(path: string, query?: Record<string, string | number | undefined | null>) {
  const url = new URL(path, apiBaseUrl.endsWith('/') ? apiBaseUrl : `${apiBaseUrl}/`);

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(key, String(value));
      }
    }
  }

  return url.toString();
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) {
      return undefined as T;
    }

    const payload = (await response.json()) as T;
    return normalizeJson(payload);
  }

  let problem: ProblemDetails | undefined;
  try {
    problem = normalizeJson((await response.json()) as ProblemDetails);
  } catch {
    problem = undefined;
  }

  const validationMessage = problem?.errors
    ? Object.entries(problem.errors)
        .flatMap(([field, messages]) => messages.map((message) => (field ? `${field}: ${message}` : message)))
        .join('\n')
    : undefined;

  throw new Error(validationMessage || problem?.detail || problem?.title || `HTTP ${response.status}`);
}

async function buildAuthHeaders() {
  if (!auth0.isAuthenticated.value) {
    return {};
  }

  const accessToken = await auth0.getAccessTokenSilently();
  return accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
}

export async function getJson<T>(path: string, query?: Record<string, string | number | undefined | null>) {
  const response = await fetch(buildUrl(path, query), {
    headers: await buildAuthHeaders()
  });
  return parseResponse<T>(response);
}

export async function postJson<TResponse, TBody>(path: string, body: TBody) {
  const response = await fetch(buildUrl(path), {
    method: 'POST',
    headers: {
      ...(await buildAuthHeaders()),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  });

  return parseResponse<TResponse>(response);
}

export async function putJson<TBody>(path: string, body: TBody) {
  const response = await fetch(buildUrl(path), {
    method: 'PUT',
    headers: {
      ...(await buildAuthHeaders()),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  });

  return parseResponse<void>(response);
}

export async function deleteJson(path: string) {
  const response = await fetch(buildUrl(path), {
    method: 'DELETE',
    headers: await buildAuthHeaders()
  });

  return parseResponse<void>(response);
}

export async function getFile(path: string, query?: Record<string, string | number | undefined | null>) {
  const response = await fetch(buildUrl(path, query), {
    headers: await buildAuthHeaders()
  });

  if (!response.ok) {
    await parseResponse<void>(response);
  }

  const contentDisposition = response.headers.get('Content-Disposition') ?? '';
  const fileNameMatch =
    contentDisposition.match(/filename\*=UTF-8''([^;]+)/i) ??
    contentDisposition.match(/filename="?([^";]+)"?/i);

  return {
    blob: await response.blob(),
    fileName: fileNameMatch?.[1] ? decodeURIComponent(fileNameMatch[1]) : 'download.bin'
  };
}
