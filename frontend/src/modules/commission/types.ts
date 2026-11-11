export type CommissionStatus =
  | "Calculated"
  | "Payable"
  | "Rejected"
  | "Paid"
  | "Pending"
  | "Approved";

export type CommissionPayoutType = "OneTime" | "Monthly";

export interface CommissionSchedule {
  id: string;
  dueDate: string;
  dueDateJalali: string;
  amount: number;
  isPaid: boolean;
  paidAt?: string | null;
}

export interface CommissionShare {
  id: string;
  userId?: string | null;
  percentage: number;
  amount: number;
  isCompanyShare: boolean;
}

export interface CommissionEntry {
  id: string;
  orderId: string;
  orderNumber?: string | null;
  orderItemId: string;
  employeeId: string;
  employeeName?: string | null;
  amount: number;
  adjustmentAmount: number;
  netAmount: number;
  status: CommissionStatus;
  payoutType: CommissionPayoutType;
  scheduleCount: number;
  schedules: CommissionSchedule[];
  customerName?: string | null;
  serviceName?: string | null;
  paidInstallments?: number;
  shares?: CommissionShare[];
  createdAt: string;
  createdAtJalali: string;
}

export interface CommissionListParams {
  page: number;
  pageSize: number;
  status?: CommissionStatus;
  employeeId?: string;
  fromUtc?: string;
  toUtc?: string;
}

export interface CreateCommissionRulePayload {
  serviceId: string;
  serviceType?: string | null;
  percentage: number;
  fixedAmount?: number | null;
  payoutType: CommissionPayoutType;
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
