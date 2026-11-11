import { api } from "@/shared/api/client";

export async function openInvoicePdf(invoiceId: string): Promise<void> {
  const popup = window.open("about:blank", "_blank");

  try {
    const response = await api.get(`/invoices/${invoiceId}/pdf`, {
      responseType: "blob",
    });

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
