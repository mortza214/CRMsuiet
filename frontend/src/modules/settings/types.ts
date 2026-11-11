export interface SettingValue {
  key: string;
  value: string;
  group: string;
  isSecret: boolean;
}

export interface SettingGroup {
  group: string;
  items: SettingValue[];
}

export interface GatewayConfig {
  id: string;
  provider: string;
  isActive: boolean;
  merchantId: string;
  terminalId: string;
}

export interface IppanelSettings {
  enabled: boolean;
  apiKeyMasked?: string | null;
  fromNumber: string;
  patternCode: string;
  baseUrl: string;
  patternParamName: string;
  source: "Environment" | "Database" | "None" | string;
}

export interface SmsResult {
  isSuccess: boolean;
  messageId?: string | null;
  errorMessage?: string | null;
  rawResponse?: string | null;
}

export interface TaxRule {
  id: string;
  name: string;
  percentage: number;
  isActive: boolean;
  effectiveFrom: string;
  effectiveFromJalali: string;
}

export interface RoleSummary {
  id: string;
  name: string;
  displayName: string;
}

export interface DepartmentSummary {
  id: string;
  name: string;
}

export interface Department {
  id: string;
  name: string;
  parentId?: string | null;
  createdAt?: string;
  createdAtJalali?: string;
}

export interface RolePayload {
  name: string;
  displayName: string;
  permissionIds: string[];
}

export interface Permission {
  id: string;
  code: string;
  group: string;
  displayName: string;
}

export interface PermissionGroup {
  group: string;
  permissions: Permission[];
}

export interface AppRole {
  id: string;
  name: string;
  displayName: string;
  permissions: Permission[];
  createdAt: string;
  createdAtJalali: string;
}

export interface AppUser {
  id: string;
  username: string;
  fullName: string;
  mobile: string;
  email?: string | null;
  isActive: boolean;
  roles: RoleSummary[];
  departments: DepartmentSummary[];
  createdAt: string;
  createdAtJalali: string;
}

export interface UserPayload {
  username?: string;
  password?: string;
  fullName: string;
  mobile: string;
  email?: string | null;
  isActive: boolean;
  roleIds: string[];
  departmentIds: string[];
}

export interface UserListParams {
  page: number;
  pageSize: number;
  search?: string;
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
