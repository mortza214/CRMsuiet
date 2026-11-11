import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  AddonPricingMode,
  BillingPeriod,
  PagedResult,
  PricePreview,
  PricingMode,
  ServiceAddon,
  ServiceCategory,
  ServiceDetail,
  ServiceFormLink,
  ServiceListItem,
  ServicePrice,
} from "@/modules/catalog/types";

export function useCatalogServices(params: {
  page: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
  pricingMode?: PricingMode;
}) {
  return useQuery({
    queryKey: ["catalog", "services", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<ServiceListItem>>>(
        "/catalog/services",
        {
          params: {
            page: params.page,
            pageSize: params.pageSize,
            search: params.search?.trim() || undefined,
            isActive: params.isActive,
            pricingMode: params.pricingMode || undefined,
          },
        },
      );
      return response.data.data as PagedResult<ServiceListItem>;
    },
    placeholderData: keepPreviousData,
  });
}

export function useCatalogService(id: string | undefined) {
  return useQuery({
    queryKey: ["catalog", "service", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceDetail>>(
        `/catalog/services/${id}`,
      );
      return response.data.data as ServiceDetail;
    },
    enabled: Boolean(id),
  });
}

export function useServiceCategories() {
  return useQuery({
    queryKey: ["catalog", "categories"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceCategory[]>>(
        "/catalog/categories",
      );
      return response.data.data ?? [];
    },
    staleTime: 5 * 60_000,
  });
}

export function useCreateCatalogService() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: Record<string, unknown>) => {
      const response = await api.post<ApiEnvelope<ServiceDetail>>(
        "/catalog/services",
        payload,
      );
      return response.data.data as ServiceDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useUpdateCatalogService(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: Record<string, unknown>) => {
      const response = await api.put<ApiEnvelope<ServiceDetail>>(
        `/catalog/services/${id}`,
        payload,
      );
      return response.data.data as ServiceDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useServiceStatusAction(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (action: "activate" | "deactivate") => {
      const response = await api.post<ApiEnvelope<ServiceDetail>>(
        `/catalog/services/${id}/${action}`,
      );
      return response.data.data as ServiceDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useSetAttributeSchema(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (attributeSchemaJson: string | null) => {
      const response = await api.put<ApiEnvelope<ServiceDetail>>(
        `/catalog/services/${id}/attribute-schema`,
        { attributeSchemaJson },
      );
      return response.data.data as ServiceDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function usePriceHistory(id: string | undefined) {
  return useQuery({
    queryKey: ["catalog", "prices", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServicePrice[]>>(
        `/catalog/services/${id}/prices`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(id),
  });
}

export function useAddServicePrice(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      price: number;
      effectiveFromUtc: string;
      currency?: string;
    }) => {
      const response = await api.post<ApiEnvelope<ServicePrice>>(
        `/catalog/services/${id}/prices`,
        payload,
      );
      return response.data.data as ServicePrice;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useServiceAddons(id: string | undefined) {
  return useQuery({
    queryKey: ["catalog", "service-addons", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceAddon[]>>(
        `/catalog/services/${id}/addons`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(id),
  });
}

export function useServiceForms(id: string | undefined) {
  return useQuery({
    queryKey: ["catalog", "service-forms", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceFormLink[]>>(
        `/catalog/services/${id}/forms`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(id),
  });
}

export function useLinkServiceForm(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      formDefinitionId: string;
      isRequired: boolean;
      sortOrder: number;
    }) => {
      const response = await api.post<ApiEnvelope<ServiceFormLink>>(
        `/catalog/services/${id}/forms`,
        payload,
      );
      return response.data.data as ServiceFormLink;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useUnlinkServiceForm(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (formDefinitionId: string) => {
      await api.delete(`/catalog/services/${id}/forms/${formDefinitionId}`);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useCatalogAddons(serviceId?: string) {
  return useQuery({
    queryKey: ["catalog", "addons", serviceId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceAddon[]>>(
        "/catalog/addons",
      );
      const all = response.data.data ?? [];
      return serviceId
        ? all.filter((addon) => !addon.serviceId || addon.serviceId === serviceId)
        : all;
    },
  });
}

export function useCreateCatalogAddon() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      serviceId?: string | null;
      code: string;
      name: string;
      price: number;
      unit: string;
      pricingMode: AddonPricingMode;
      isActive: boolean;
      sortOrder: number;
      description?: string | null;
    }) => {
      const response = await api.post<ApiEnvelope<ServiceAddon>>(
        "/catalog/addons",
        { ...payload, isOptional: true },
      );
      return response.data.data as ServiceAddon;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useUpdateCatalogAddon() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      id,
      ...payload
    }: {
      id: string;
      serviceId?: string | null;
      code: string;
      name: string;
      price: number;
      unit: string;
      pricingMode: AddonPricingMode;
      isActive: boolean;
      sortOrder: number;
      description?: string | null;
    }) => {
      const response = await api.put<ApiEnvelope<ServiceAddon>>(
        `/catalog/addons/${id}`,
        { ...payload, isOptional: true },
      );
      return response.data.data as ServiceAddon;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function useDeactivateCatalogAddon() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post<ApiEnvelope<ServiceAddon>>(
        `/catalog/addons/${id}/deactivate`,
      );
      return response.data.data as ServiceAddon;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
  });
}

export function usePricePreview() {
  return useMutation({
    mutationFn: async (payload: {
      serviceId: string;
      quantity: number;
      attributesJson?: string | null;
      addons: Array<{ addonId: string; quantity: number }>;
    }) => {
      const response = await api.post<ApiEnvelope<PricePreview>>(
        "/pricing/preview",
        payload,
      );
      return response.data.data as PricePreview;
    },
  });
}

export function useAddOrderItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      orderId,
      ...payload
    }: {
      orderId: string;
      serviceId: string;
      quantity: number;
      billingCycle: string;
      attributesJson?: string | null;
      addons: Array<{ addonId: string; quantity: number }>;
    }) => {
      const response = await api.post<ApiEnvelope<unknown>>(
        `/orders/${orderId}/items`,
        payload,
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });
}

export function useOverrideOrderItemPrice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      orderId,
      itemId,
      overridePrice,
      reason,
    }: {
      orderId: string;
      itemId: string;
      overridePrice: number;
      reason: string;
    }) => {
      const response = await api.post<ApiEnvelope<unknown>>(
        `/orders/${orderId}/items/${itemId}/override-price`,
        { overridePrice, reason },
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });
}

export const pricingModeLabels: Record<PricingMode, string> = {
  UnitBased: "واحدی",
  Fixed: "ثابت",
  Additive: "افزودنی",
};

export const billingPeriodLabels: Record<BillingPeriod, string> = {
  Monthly: "ماهانه",
  OneTime: "یک‌بار",
  Yearly: "سالانه",
};
