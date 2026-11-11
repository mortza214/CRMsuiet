export interface FinancialSummary {
  fromUtc: string;
  toUtc: string;
  totalSales: number;
  totalReceived: number;
  totalDebt: number;
  invoiceCount: number;
}

export interface FinancialTrendPoint {
  month: string;
  monthJalali: string;
  sales: number;
  received: number;
}

export interface Debtor {
  customerId: string;
  customerName: string;
  mobile?: string | null;
  outstandingAmount: number;
  invoiceCount: number;
}

export interface BadDebtor {
  customerId: string;
  customerName: string;
  mobile?: string | null;
  outstandingAmount: number;
  overdueInvoiceCount: number;
  maxOverdueDays: number;
}

export type LedgerEntryType = "Invoice" | "Payment" | "WalletDeposit";

export interface LedgerEntry {
  date: string;
  dateJalali: string;
  entryType: LedgerEntryType;
  reference?: string | null;
  description: string;
  debit: number;
  credit: number;
  balance: number;
}

export interface SalesReportItem {
  serviceType: string;
  orderCount: number;
  quantity: number;
  totalAmount: number;
}

export interface ReportCustomer {
  id: string;
  displayName: string;
  mobile: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
