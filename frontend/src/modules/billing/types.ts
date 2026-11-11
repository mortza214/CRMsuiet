export type InvoiceType = "Official" | "Unofficial" | "Proforma" | "Monthly";

export type InvoiceStatus =
  | "Draft"
  | "Issued"
  | "PartiallyPaid"
  | "Paid"
  | "Canceled"
  | "PendingPayment"
  | "PendingVerification";

export interface InvoiceItem {
  id: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface InvoiceListItem {
  id: string;
  invoiceNumber: string;
  orderId: string;
  orderNumber?: string | null;
  customerId: string;
  customerName?: string | null;
  type: InvoiceType;
  issueDate: string;
  issueDateJalali: string;
  dueDate: string;
  dueDateJalali: string;
  totalAmount: number;
  paidAmount: number;
  status: InvoiceStatus;
}

export interface InvoiceDetail extends InvoiceListItem {
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  insuranceAmount: number;
  items: InvoiceItem[];
  createdAt: string;
  createdAtJalali: string;
  receiptImageUrl?: string | null;
}

export interface InvoiceListParams {
  page: number;
  pageSize: number;
  search?: string;
  status?: InvoiceStatus;
  customerId?: string;
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
