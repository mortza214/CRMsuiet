import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { ApiEnvelope, AuthResult } from "@/modules/auth/types";
import { useAuthStore } from "@/modules/auth/store";

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5080/api";

export const api = axios.create({
  baseURL: apiBaseUrl,
  timeout: 30_000,
  headers: {
    "Content-Type": "application/json",
  },
});

type RetryableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

type ApiErrorHandler = (message: string, errors?: string[]) => void;

let apiErrorHandler: ApiErrorHandler | null = null;
let refreshPromise: Promise<string> | null = null;

export function setApiErrorHandler(handler: ApiErrorHandler | null) {
  apiErrorHandler = handler;
}

export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as ApiEnvelope<unknown> | undefined;
    return (
      payload?.message ||
      payload?.errors?.find((item) => item.trim().length > 0) ||
      (error.response?.status === 401
        ? "نشست شما منقضی شده است. دوباره وارد شوید."
        : "خطایی در ارتباط با سرور رخ داد.")
    );
  }

  return error instanceof Error ? error.message : "خطای ناشناخته‌ای رخ داد.";
}

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetryableConfig | undefined;
    const status = error.response?.status;
    const requestUrl = config?.url ?? "";

    if (
      status === 401 &&
      config &&
      !config._retry &&
      !requestUrl.includes("/auth/login") &&
      !requestUrl.includes("/auth/refresh")
    ) {
      const refreshToken = useAuthStore.getState().refreshToken;

      if (!refreshToken) {
        forceLogout();
        return Promise.reject(error);
      }

      config._retry = true;

      try {
        refreshPromise ??= refreshAccessToken(refreshToken).finally(() => {
          refreshPromise = null;
        });

        const accessToken = await refreshPromise;
        config.headers.Authorization = `Bearer ${accessToken}`;
        return api(config);
      } catch (refreshError) {
        forceLogout();
        return Promise.reject(refreshError);
      }
    }

    if (
      status !== 401 &&
      !requestUrl.includes("/auth/login") &&
      !requestUrl.includes("/auth/refresh")
    ) {
      const payload = error.response?.data as ApiEnvelope<unknown> | undefined;
      apiErrorHandler?.(
        payload?.message ?? getApiErrorMessage(error),
        payload?.errors,
      );
    }

    return Promise.reject(error);
  },
);

async function refreshAccessToken(refreshToken: string): Promise<string> {
  const response = await axios.post<ApiEnvelope<AuthResult>>(
    `${apiBaseUrl}/auth/refresh`,
    { refreshToken },
    { headers: { "Content-Type": "application/json" }, timeout: 30_000 },
  );

  const result = response.data.data;
  if (!result?.accessToken) {
    throw new Error("پاسخ Refresh Token معتبر نیست.");
  }

  useAuthStore.getState().setAuth(result);
  return result.accessToken;
}

function forceLogout() {
  useAuthStore.getState().logout();
  if (window.location.pathname !== "/login") {
    window.location.assign("/login");
  }
}
