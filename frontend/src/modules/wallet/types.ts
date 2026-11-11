export type WalletOwnerType = "Customer" | "Employee";

export type WalletTransactionType =
  | "Deposit"
  | "Withdraw"
  | "CommissionPayout"
  | "InvoicePayment";

export interface WalletBalance {
  id: string;
  ownerId: string;
  ownerType: WalletOwnerType;
  balance: number;
}

export interface WalletTransaction {
  id: string;
  walletId: string;
  amount: number;
  transactionType: WalletTransactionType;
  referenceId?: string | null;
  description?: string | null;
  balanceAfter: number;
  createdAt: string;
  createdAtJalali: string;
}

export interface MyWalletData {
  balance: WalletBalance;
  transactions: WalletTransaction[];
}
