import type { WalletTransactionType } from "@/modules/wallet/types";

export const walletTransactionLabels: Record<WalletTransactionType, string> = {
  Deposit: "واریز",
  Withdraw: "برداشت",
  CommissionPayout: "واریز پورسانت",
  InvoicePayment: "پرداخت فاکتور",
};

export const walletTransactionColors: Record<WalletTransactionType, string> = {
  Deposit: "green",
  Withdraw: "red",
  CommissionPayout: "blue",
  InvoicePayment: "orange",
};
