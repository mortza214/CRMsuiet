import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  InvoiceDetail,
  InvoiceListItem,
  InvoiceListParams,
  InvoiceStatus,
  PagedResult,
} from "@/modules/billing/types";

export function useInvoices(params: InvoiceListParams, enabled = true) {
  return useQuery({
    queryKey: ["invoices", "list", params],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<InvoiceListItem>>
      >("/invoices", {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          search: params.search?.trim() || undefined,
          status: params.status || undefined,
          customerId: params.customerId || undefined,
        },
      });
      return response.data.data as PagedResult<InvoiceListItem>;
    },
    placeholderData: keepPreviousData,
    enabled,
  });
}

export function useInvoiceDetails(id: string | undefined) {
  return useQuery({
    queryKey: ["invoices", "detail", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<InvoiceDetail>>(
        `/invoices/${id}`,
      );
      return response.data.data as InvoiceDetail;
    },
    enabled: Boolean(id),
  });
}

export function useIssueInvoiceFromOrder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (orderId: string) => {
      const response = await api.post<ApiEnvelope<InvoiceDetail>>(
        `/invoices/from-order/${orderId}`,
        { type: "Official" },
      );
      return response.data.data as InvoiceDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["invoices"] });
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });
}

export function useUpdateInvoiceStatus(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (status: InvoiceStatus) => {
      const response = await api.put<ApiEnvelope<InvoiceDetail>>(
        `/invoices/${id}/status`,
        { status },
      );
      return response.data.data as InvoiceDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["invoices"] });
    },
  });
}
