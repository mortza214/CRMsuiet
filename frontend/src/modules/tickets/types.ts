export type TicketPriority = "Low" | "Medium" | "High" | "Critical";

export type TicketStatus =
  | "Open"
  | "InProgress"
  | "PendingCustomer"
  | "Resolved"
  | "Closed";

export type TicketSource = "Portal" | "Phone" | "Internal";
export type TicketSenderType = "Customer" | "Employee";

export interface TicketMessage {
  id: string;
  senderId: string;
  senderType: TicketSenderType;
  message: string;
  isInternalNote?: boolean | null;
  createdAt: string;
  createdAtJalali: string;
}

export interface TicketStatusHistory {
  id: string;
  fromStatus?: string | null;
  toStatus: TicketStatus;
  changedByUserId?: string | null;
  changedAt: string;
  changedAtJalali: string;
  note?: string | null;
}

export interface TicketListItem {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerName?: string | null;
  departmentId: string;
  departmentName?: string | null;
  assignedToUserId?: string | null;
  assignedToName?: string | null;
  subject: string;
  priority: TicketPriority;
  status: TicketStatus;
  source: TicketSource;
  messageCount: number;
  createdAt: string;
  createdAtJalali: string;
}

export interface TicketDetail extends TicketListItem {
  serviceId?: string | null;
  serviceName?: string | null;
  messages: TicketMessage[];
  statusHistory: TicketStatusHistory[];
}

export interface TicketListParams {
  page: number;
  pageSize: number;
  search?: string;
  status?: TicketStatus;
  priority?: TicketPriority;
}

export interface CreateTicketPayload {
  customerId: string;
  serviceId?: string | null;
  subject: string;
  departmentId: string;
  priority: TicketPriority;
  source: TicketSource;
  message: string;
}

export interface Department {
  id: string;
  name: string;
  parentId?: string | null;
}

export interface DepartmentSummary {
  id: string;
  name: string;
}

export interface TicketAssignee {
  id: string;
  username: string;
  fullName: string;
  isActive: boolean;
  departments: DepartmentSummary[];
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
