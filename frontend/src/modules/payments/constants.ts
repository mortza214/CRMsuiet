import type {
  PaymentMethod,
  TransactionStatus,
} from "@/modules/payments/types";

export const transactionStatusMeta: Record<
  TransactionStatus,
  { label: string; color: string }
> = {
  Pending: { label: "در انتظار", color: "orange" },
  Success: { label: "موفق", color: "green" },
  Failed: { label: "ناموفق", color: "red" },
  Refunded: { label: "برگشت داده شده", color: "purple" },
};

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  OnlineGateway: "درگاه آنلاین",
  ManualTransfer: "کارت به کارت",
  Cash: "نقدی",
  Wallet: "کیف پول",
};

export const manualPaymentMethods = [
  { value: "ManualTransfer", label: "کارت به کارت" },
  { value: "Cash", label: "نقدی" },
];
