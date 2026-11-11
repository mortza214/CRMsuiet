export type PricingMode = "UnitBased" | "Fixed" | "Additive";
export type BillingPeriod = "Monthly" | "OneTime" | "Yearly";
export type AddonPricingMode = "Fixed" | "PerUnit";

export interface ServicePrice {
  id: string;
  price: number;
  effectiveFrom: string;
  effectiveFromJalali: string;
  effectiveTo?: string | null;
  effectiveToJalali?: string | null;
  currency: string;
}

export interface ServiceAddon {
  id: string;
  name: string;
  price: number;
  unit: string;
  isOptional: boolean;
  createdAt: string;
  createdAtJalali: string;
  serviceId?: string | null;
  code?: string | null;
  pricingMode: AddonPricingMode;
  isActive: boolean;
  sortOrder: number;
  description?: string | null;
}

export interface ServiceFormLink {
  id: string;
  formDefinitionId: string;
  formCode?: string | null;
  formTitle?: string | null;
  isRequired: boolean;
  sortOrder: number;
}

export interface ServiceListItem {
  id: string;
  code: string;
  name: string;
  categoryId?: string | null;
  categoryName?: string | null;
  serviceType: string;
  unit: string;
  isActive: boolean;
  currentPrice?: number | null;
  priceEffectiveFrom?: string | null;
  priceEffectiveFromJalali?: string | null;
  createdAt: string;
  createdAtJalali: string;
  pricingMode: PricingMode;
}

export interface ServiceDetail {
  id: string;
  code: string;
  name: string;
  categoryId: string;
  categoryName: string;
  serviceType: string;
  unit: string;
  description?: string | null;
  isActive: boolean;
  currentPrice?: number | null;
  currentPriceEffectiveFrom?: string | null;
  currentPriceEffectiveFromJalali?: string | null;
  prices: ServicePrice[];
  createdAt: string;
  createdAtJalali: string;
  pricingMode: PricingMode;
  billingPeriod: BillingPeriod;
  commissionPercent?: number | null;
  attributeSchemaJson?: string | null;
  workflowDefinitionId?: string | null;
  addons?: ServiceAddon[] | null;
  forms?: ServiceFormLink[] | null;
  canEdit: boolean;
  editBlockedReason?: string | null;
}

export interface ServiceCategory {
  id: string;
  name: string;
  parentId?: string | null;
}

export interface PricePreviewAddon {
  addonId: string;
  name: string;
  unitPrice: number;
  quantity: number;
  total: number;
}

export interface PricePreview {
  serviceId: string;
  serviceName: string;
  pricingMode: PricingMode;
  quantity: number;
  unitPrice: number;
  addonsTotal: number;
  total: number;
  overridePrice?: number | null;
  effectivePrice: number;
  addons: PricePreviewAddon[];
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
