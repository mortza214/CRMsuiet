import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type { CustomerListItem } from "@/modules/customers/types";
import type {
  CatalogAddon,
  CatalogService,
  CreateOrderPayload,
  OrderDetail,
  OrderListItem,
  OrderListParams,
  OrderStatus,
  PagedResult,
  CustomerService,
  CreateServiceRequestPayload,
  ServiceRequestDetail,
  ServiceRequestResult,
  ServiceRequestSummary,
} from "@/modules/orders/types";

export function useOrders(params: OrderListParams) {
  return useQuery({
    queryKey: ["orders", "list", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<OrderListItem>>>(
        "/orders",
        {
          params: {
            page: params.page,
            pageSize: params.pageSize,
            search: params.search?.trim() || undefined,
            status: params.status || undefined,
          },
        },
      );

      return response.data.data as PagedResult<OrderListItem>;
    },
    placeholderData: keepPreviousData,
  });
}

export function useOrderDetails(id: string | undefined) {
  return useQuery({
    queryKey: ["orders", "detail", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<OrderDetail>>(`/orders/${id}`);
      return response.data.data as OrderDetail;
    },
    enabled: Boolean(id),
  });
}

export function useCreateOrder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: CreateOrderPayload) => {
      const response = await api.post<ApiEnvelope<OrderDetail>>(
        "/orders",
        payload,
      );
      return response.data.data as OrderDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });
}

export function useUpdateOrderStatus(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (status: OrderStatus) => {
      const response = await api.put<ApiEnvelope<OrderDetail>>(
        `/orders/${id}/status`,
        { status },
      );
      return response.data.data as OrderDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });
}

export function useApplyOrderDiscount(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (discountAmount: number) => {
      const response = await api.post<ApiEnvelope<OrderDetail>>(
        `/orders/${id}/discount`,
        { discountAmount },
      );
      return response.data.data as OrderDetail;
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
      const response = await api.post<ApiEnvelope<OrderDetail>>(
        `/orders/${orderId}/items/${itemId}/override-price`,
        { overridePrice, reason },
      );
      return response.data.data as OrderDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });
}

export function useServiceRequestDetail(orderId: string | undefined) {
  return useQuery({
    queryKey: ["sales", "service-request", orderId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceRequestDetail>>(
        `/sales/service-requests/${orderId}`,
      );
      return response.data.data as ServiceRequestDetail;
    },
    enabled: Boolean(orderId),
  });
}

export function useCustomerServiceRequests(customerId: string | undefined) {
  return useQuery({
    queryKey: ["sales", "customer-service-requests", customerId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceRequestSummary[]>>(
        `/sales/customers/${customerId}/service-requests`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(customerId),
  });
}

export function useRecentServiceRequests(salesExpertId?: string) {
  return useQuery({
    queryKey: ["sales", "recent-service-requests", salesExpertId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<ServiceRequestSummary[]>>(
        "/sales/service-requests",
        { params: { salesExpertId: salesExpertId || undefined, limit: 10 } },
      );
      return response.data.data ?? [];
    },
  });
}

export function useCustomerServices(customerId: string | undefined) {
  return useQuery({
    queryKey: ["customers", customerId, "services"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<CustomerService[]>>(
        `/customers/${customerId}/services`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(customerId),
  });
}

export function useCreateServiceRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateServiceRequestPayload) => {
      const response = await api.post<ApiEnvelope<ServiceRequestResult>>(
        "/sales/service-requests",
        payload,
      );
      return response.data.data as ServiceRequestResult;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });
}

export function useOrderCustomers(search: string) {
  return useQuery({
    queryKey: ["customers", "order-select", search],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<CustomerListItem>>
      >("/customers", {
        params: {
          page: 1,
          pageSize: 20,
          search: search.trim() || undefined,
        },
      });
      return response.data.data?.items ?? [];
    },
    staleTime: 30_000,
  });
}

export function useOrderServices(search: string) {
  return useQuery({
    queryKey: ["catalog", "services", search],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<CatalogService>>
      >("/catalog/services", {
        params: {
          page: 1,
          pageSize: 100,
          search: search.trim() || undefined,
          isActive: true,
        },
      });
      return response.data.data?.items ?? [];
    },
    staleTime: 60_000,
  });
}

export function useOrderServiceDetail(id: string | undefined) {
  return useQuery({
    queryKey: ["catalog", "service-detail", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<CatalogService>>(
        `/catalog/services/${id}`,
      );
      return response.data.data as CatalogService;
    },
    enabled: Boolean(id),
  });
}

export function useOrderServiceAddons(id: string | undefined) {
  return useQuery({
    queryKey: ["catalog", "service-addons", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<CatalogAddon[]>>(
        `/catalog/services/${id}/addons`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(id),
  });
}

export function useOrderPricePreview() {
  return useMutation({
    mutationFn: async (payload: {
      serviceId: string;
      quantity: number;
      attributesJson?: string | null;
      addons: Array<{ addonId: string; quantity: number }>;
    }) => {
      const response = await api.post<
        ApiEnvelope<{
          unitPrice: number;
          addonsTotal: number;
          total: number;
          effectivePrice: number;
        }>
      >("/pricing/preview", payload);
      return response.data.data;
    },
  });
}

export function useOrderAddons() {
  return useQuery({
    queryKey: ["catalog", "addons"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<CatalogAddon[]>>(
        "/catalog/addons",
      );
      return response.data.data ?? [];
    },
    staleTime: 60_000,
  });
}
