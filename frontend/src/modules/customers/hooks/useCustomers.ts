import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  CustomerDetail,
  CustomerListParams,
  CustomerListItem,
  CustomerPayload,
  PagedResult,
  UserOption,
} from "@/modules/customers/types";

function toQueryParams(params: CustomerListParams) {
  return {
    page: params.page,
    pageSize: params.pageSize,
    search: params.search?.trim() || undefined,
    onlyDebtors: params.onlyDebtors || undefined,
    parentCustomerId: params.parentCustomerId || undefined,
  };
}

export function useCustomers(
  params: CustomerListParams,
  enabled = true,
) {
  return useQuery({
    queryKey: ["customers", "list", params],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<CustomerListItem>>
      >("/customers", { params: toQueryParams(params) });

      return response.data.data as PagedResult<CustomerListItem>;
    },
    placeholderData: keepPreviousData,
    enabled,
  });
}

export function useCustomerDetails(id: string | undefined) {
  return useQuery({
    queryKey: ["customers", "detail", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<CustomerDetail>>(
        `/customers/${id}`,
      );
      return response.data.data as CustomerDetail;
    },
    enabled: Boolean(id),
  });
}

export function useCreateCustomer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: CustomerPayload) => {
      const response = await api.post<ApiEnvelope<CustomerDetail>>(
        "/customers",
        payload,
      );
      return response.data.data as CustomerDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });
}

export function useUpdateCustomer(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: CustomerPayload) => {
      const response = await api.put<ApiEnvelope<CustomerDetail>>(
        `/customers/${id}`,
        payload,
      );
      return response.data.data as CustomerDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });
}

export function useCustomerOwners(enabled = true) {
  return useQuery({
    queryKey: ["users", "owners"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<UserOption>>>(
        "/users",
        { params: { page: 1, pageSize: 200, isActive: true } },
      );
      return response.data.data?.items ?? [];
    },
    staleTime: 5 * 60_000,
    enabled,
  });
}
