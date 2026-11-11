import type { InvoiceStatus, InvoiceType } from "@/modules/billing/types";

export const invoiceStatusMeta: Record<
  InvoiceStatus,
  { label: string; color: string }
> = {
  Draft: { label: "پیش‌نویس", color: "default" },
  Issued: { label: "صادر شده", color: "blue" },
  PendingPayment: { label: "در انتظار پرداخت", color: "gold" },
  PendingVerification: { label: "در انتظار تأیید مالی", color: "purple" },
  PartiallyPaid: { label: "پرداخت جزئی", color: "orange" },
  Paid: { label: "تسویه شده", color: "green" },
  Canceled: { label: "لغو شده", color: "red" },
};

export const invoiceStatusOptions = (
  Object.keys(invoiceStatusMeta) as InvoiceStatus[]
).map((value) => ({
  value,
  label: invoiceStatusMeta[value].label,
}));

export const invoiceTypeMeta: Record<
  InvoiceType,
  { label: string; color: string }
> = {
  Official: { label: "رسمی", color: "green" },
  Unofficial: { label: "غیررسمی", color: "orange" },
  Proforma: { label: "Proforma", color: "blue" },
  Monthly: { label: "Monthly", color: "purple" },
};
