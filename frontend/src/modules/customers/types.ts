export type CustomerType = "Natural" | "Legal";
export type CustomerOwnerType =
  | "CurrentUser"
  | "CompanyReferred"
  | "CompanyCustomer"
  | "FollowUp"
  | "Company"
  | "SalesExpert";
export type CustomerNotificationChannel = "Sms" | "Bale";

export interface CustomerTag {
  id: string;
  name: string;
  color: string;
}

export interface CustomerGroup {
  id: string;
  name: string;
  description?: string | null;
  createdAt: string;
  createdAtJalali: string;
}

export interface CustomerListItem {
  id: string;
  type: CustomerType;
  displayName: string;
  firstName?: string | null;
  lastName?: string | null;
  companyName?: string | null;
  mobile: string;
  nationalId: string;
  groupName?: string | null;
  isReseller: boolean;
  parentCustomerId?: string | null;
  ownerUserId?: string | null;
  ownerName?: string | null;
  tags: CustomerTag[];
  createdAt: string;
  createdAtJalali: string;
}

export interface CustomerDetail {
  id: string;
  type: CustomerType;
  firstName?: string | null;
  lastName?: string | null;
  companyName?: string | null;
  displayName: string;
  economicCode?: string | null;
  nationalId: string;
  mobile: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  ownerUserId?: string | null;
  parentCustomerId?: string | null;
  isReseller: boolean;
  group?: CustomerGroup | null;
  tags: CustomerTag[];
  userId?: string | null;
  createdAt: string;
  createdAtJalali: string;
  updatedAt?: string | null;
  updatedAtJalali?: string | null;
  ownerType?: CustomerOwnerType;
  ownerId?: string | null;
  preferredNotificationChannel?: CustomerNotificationChannel;
}

export interface CustomerListParams {
  page: number;
  pageSize: number;
  search?: string;
  onlyDebtors?: boolean;
  parentCustomerId?: string;
}

export interface CustomerPayload {
  type: CustomerType;
  firstName?: string | null;
  lastName?: string | null;
  companyName?: string | null;
  economicCode?: string | null;
  nationalId: string;
  mobile: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  ownerUserId?: string | null;
  parentCustomerId?: string | null;
  isReseller: boolean;
  groupId?: string | null;
  tagIds?: string[];
  ownerType?: CustomerOwnerType;
  preferredNotificationChannel?: CustomerNotificationChannel;
}

export interface UserOption {
  id: string;
  username: string;
  fullName: string;
  mobile: string;
  isActive: boolean;
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
