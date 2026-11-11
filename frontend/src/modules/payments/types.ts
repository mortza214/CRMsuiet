export type PaymentMethod =
  | "OnlineGateway"
  | "ManualTransfer"
  | "Cash"
  | "Wallet";

export type TransactionStatus = "Pending" | "Success" | "Failed" | "Refunded";

export interface TransactionListItem {
  id: string;
  invoiceId?: string | null;
  invoiceNumber?: string | null;
  customerId: string;
  customerName?: string | null;
  amount: number;
  paymentMethod: PaymentMethod;
  gatewayName: string;
  referenceNumber?: string | null;
  trackingCode: string;
  status: TransactionStatus;
  paidAt?: string | null;
  paidAtJalali?: string | null;
  createdAt: string;
  createdAtJalali: string;
}

export interface TransactionListParams {
  page: number;
  pageSize: number;
  status?: TransactionStatus;
}

export interface ManualPaymentPayload {
  invoiceId?: string | null;
  customerId: string;
  amount: number;
  paymentMethod: PaymentMethod;
  referenceNumber?: string | null;
  receiptImageUrl?: string | null;
}

export interface WalletBalance {
  id: string;
  ownerId: string;
  ownerType: string;
  balance: number;
}

export interface OnlinePaymentInitiation {
  transactionId: string;
  trackingCode: string;
  token: string;
  paymentUrl: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
