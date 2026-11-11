export type OrderStatus =
  | "Draft"
  | "PendingPayment"
  | "Paid"
  | "Canceled"
  | "Provisioning"
  | "Completed"
  | "Pending";

export type OrderType = "New" | "Renewal" | "Upgrade";
export type BillingCycle = "OneTime" | "Monthly" | "Yearly";

export interface OrderItem {
  id: string;
  serviceId?: string | null;
  dataCenterItemId?: string | null;
  addonId?: string | null;
  itemName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  billingCycle: BillingCycle;
  createdAt: string;
  createdAtJalali: string;
  unitPriceSnapshot?: number;
  addonsJson?: string | null;
  attributesJson?: string | null;
  overridePrice?: number | null;
  overrideReason?: string | null;
  overrideBy?: string | null;
  effectivePrice?: number;
  commissionBase?: number;
}

export interface OrderStatusHistory {
  id: string;
  fromStatus?: string | null;
  toStatus: OrderStatus;
  changedByUserId?: string | null;
  changedAt: string;
  changedAtJalali: string;
  note?: string | null;
}

export interface OrderListItem {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName?: string | null;
  salesExpertId: string;
  orderDate: string;
  orderDateJalali: string;
  status: OrderStatus;
  orderType: OrderType;
  totalAmount: number;
  discountAmount: number;
  payableAmount: number;
  itemCount: number;
}

export interface OrderDetail {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName?: string | null;
  salesExpertId: string;
  orderDate: string;
  orderDateJalali: string;
  status: OrderStatus;
  orderType: OrderType;
  totalAmount: number;
  discountAmount: number;
  payableAmount: number;
  notes?: string | null;
  items: OrderItem[];
  statusHistory: OrderStatusHistory[];
  createdAt: string;
  createdAtJalali: string;
}

export interface CatalogService {
  id: string;
  code: string;
  name: string;
  categoryName?: string | null;
  serviceType: string;
  unit: string;
  isActive: boolean;
  currentPrice?: number | null;
  pricingMode?: string;
  billingPeriod?: string;
  attributeSchemaJson?: string | null;
  commissionPercent?: number | null;
}

export interface CatalogAddon {
  id: string;
  name: string;
  price: number;
  unit: string;
  isOptional: boolean;
}

export interface OrderDraftItem {
  key: string;
  serviceId?: string;
  addonId?: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  billingCycle: BillingCycle;
  attributesJson?: string | null;
  addons?: Array<{ addonId: string; quantity: number; name: string; unitPrice: number }>;
  effectivePrice?: number;
}

export interface OrderListParams {
  page: number;
  pageSize: number;
  search?: string;
  status?: OrderStatus;
}

export interface CustomerService {
  id: string;
  customerId: string;
  serviceId: string;
  orderId: string;
  serviceName: string;
  monthlyAmount: number;
  status: string;
  activatedAt: string;
  activatedAtJalali: string;
  nextInvoiceAt?: string | null;
  nextInvoiceAtJalali?: string | null;
  commissionBase: number;
}

export interface ServiceRequestTask {
  id: string;
  stateName: string;
  status: string;
  assignedUserId?: string | null;
  assignedDepartmentId?: string | null;
  createdAt: string;
  createdAtJalali: string;
}

export interface ServiceRequestSummary {
  orderId: string;
  orderNumber: string;
  customerId: string;
  customerName?: string | null;
  serviceName?: string | null;
  totalAmount: number;
  orderStatus: string;
  createdAt: string;
  createdAtJalali: string;
  workflowInstanceId?: string | null;
  workflowCode?: string | null;
  workflowStateCode?: string | null;
  workflowStateName?: string | null;
  workflowStatus?: string | null;
  salesExpertId?: string | null;
  currentAssigneeName?: string | null;
}

export interface ServiceRequestDetail extends ServiceRequestSummary {
  customerMobile?: string | null;
  serviceId?: string | null;
  quantity: number;
  unitPriceSnapshot: number;
  addonsTotal: number;
  effectivePrice: number;
  attributesJson?: string | null;
  tasks: ServiceRequestTask[];
}

export interface CreateServiceRequestPayload {
  customerId: string;
  serviceId: string;
  attributesJson: string;
  requiresFeasibility: boolean;
}

export interface ServiceRequestResult {
  orderId: string;
  orderNumber: string;
  workflowInstanceId?: string | null;
  workflowCode?: string | null;
  totalAmount: number;
  requiresFeasibility: boolean;
}

export interface CreateOrderPayload {
  customerId: string;
  orderType: OrderType;
  orderDateUtc?: string | null;
  notes?: string | null;
  items: Array<{
    serviceId?: string | null;
    dataCenterItemId?: string | null;
    addonId?: string | null;
    quantity: number;
    billingCycle: BillingCycle;
    attributesJson?: string | null;
    addons?: Array<{ addonId: string; quantity: number }>;
  }>;
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
