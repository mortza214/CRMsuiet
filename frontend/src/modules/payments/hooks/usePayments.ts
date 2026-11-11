import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  ManualPaymentPayload,
  OnlinePaymentInitiation,
  PagedResult,
  TransactionListItem,
  TransactionListParams,
  WalletBalance,
} from "@/modules/payments/types";

export function useTransactions(params: TransactionListParams) {
  return useQuery({
    queryKey: ["payments", "transactions", params],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<TransactionListItem>>
      >("/payments/transactions", {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          status: params.status || undefined,
        },
      });
      return response.data.data as PagedResult<TransactionListItem>;
    },
    placeholderData: keepPreviousData,
  });
}

export function useCreateManualPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: ManualPaymentPayload) => {
      const response = await api.post<ApiEnvelope<unknown>>(
        "/payments/manual",
        payload,
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["payments"] });
      await queryClient.invalidateQueries({ queryKey: ["invoices"] });
    },
  });
}

export function useApproveManualPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (transactionId: string) => {
      const response = await api.put<ApiEnvelope<unknown>>(
        `/payments/manual/${transactionId}/approve`,
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["payments"] });
      await queryClient.invalidateQueries({ queryKey: ["invoices"] });
    },
  });
}

export function useCustomerWalletBalance(
  customerId: string | undefined,
  enabled = true,
) {
  return useQuery({
    queryKey: ["wallet", "customer-balance", customerId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<WalletBalance>>(
        `/wallets/customers/${customerId}/balance`,
      );
      return response.data.data as WalletBalance;
    },
    enabled: Boolean(customerId) && enabled,
  });
}

export function usePayInvoiceFromWallet() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      invoiceId,
      customerId,
    }: {
      invoiceId: string;
      customerId: string;
    }) => {
      const response = await api.post<ApiEnvelope<WalletBalance>>(
        "/wallets/pay-invoice",
        { invoiceId, customerId },
      );
      return response.data.data as WalletBalance;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["invoices"] });
      await queryClient.invalidateQueries({ queryKey: ["wallet"] });
      await queryClient.invalidateQueries({ queryKey: ["payments"] });
    },
  });
}

export function useInitiateSamanPayment() {
  return useMutation({
    mutationFn: async (invoiceId: string) => {
      const response = await api.post<ApiEnvelope<OnlinePaymentInitiation>>(
        "/payments/saman/initiate",
        {
          invoiceId,
          callbackUrl: `${window.location.origin}/payments`,
        },
      );
      return response.data.data as OnlinePaymentInitiation;
    },
  });
}
