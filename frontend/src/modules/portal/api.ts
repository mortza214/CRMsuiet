import axios from "axios";
import type { ApiEnvelope } from "@/modules/auth/types";
import { usePortalAuth } from "@/modules/portal/store";

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5080/api";

export const portalApi = axios.create({
  baseURL: apiBaseUrl,
  timeout: 30_000,
  headers: { "Content-Type": "application/json" },
});

portalApi.interceptors.request.use((config) => {
  const token = usePortalAuth.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

portalApi.interceptors.response.use(
  (response) => response,
  (error) => {
    if (
      axios.isAxiosError(error) &&
      error.response?.status === 401 &&
      !error.config?.url?.includes("/portal/auth/")
    ) {
      usePortalAuth.getState().logout();
      if (window.location.pathname !== "/portal/login") {
        window.location.assign("/portal/login");
      }
    }

    return Promise.reject(error);
  },
);

export function getPortalErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as ApiEnvelope<unknown> | undefined;
    return (
      payload?.message ||
      payload?.errors?.find((item) => item.trim().length > 0) ||
      "خطایی در ارتباط با سرور رخ داد."
    );
  }

  return error instanceof Error ? error.message : "خطای ناشناخته‌ای رخ داد.";
}

export async function openPortalInvoicePdf(invoiceId: string): Promise<void> {
  const popup = window.open("about:blank", "_blank");

  try {
    const response = await portalApi.get(
      `/portal/invoices/${invoiceId}/pdf`,
      { responseType: "blob" },
    );
    const url = URL.createObjectURL(response.data as Blob);

    if (popup) {
      popup.location.href = url;
    } else {
      window.open(url, "_blank");
    }
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
  } catch (error) {
    popup?.close();
    throw error;
  }
}
