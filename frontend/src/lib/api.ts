const defaultApiBaseUrl = "http://localhost:5000";

export const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL || defaultApiBaseUrl
).replace(/\/+$/, "");

export async function apiRequest<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const response = await fetch(`${API_BASE_URL}${normalizedPath}`, {
    ...init,
    credentials: init?.credentials ?? "include",
  });

  const responseText = response.status === 204 ? "" : await response.text();
  let responseBody: unknown;

  if (responseText) {
    try {
      responseBody = JSON.parse(responseText);
    } catch {
      responseBody = responseText;
    }
  }

  if (!response.ok) {
    const errorMessage =
      typeof responseBody === "string"
        ? responseBody
        : typeof responseBody === "object" &&
            responseBody !== null &&
            "detail" in responseBody
          ? String(responseBody.detail)
          : typeof responseBody === "object" &&
              responseBody !== null &&
              "title" in responseBody
            ? String(responseBody.title)
            : `Request failed (${response.status})`;
    throw new Error(errorMessage);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return responseBody as T;
}
