import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ApiEnvelope } from "@/modules/auth/types";
import type { InvoiceListItem } from "@/modules/billing/types";
import type { CustomerDetail, CustomerListItem } from "@/modules/customers/types";
import type {
  CustomerService,
  OrderDetail,
  OrderListItem,
  ServiceRequestSummary,
} from "@/modules/orders/types";
import { portalApi } from "@/modules/portal/api";
import { usePortalAuth } from "@/modules/portal/store";
import type {
  PortalCreateTicketPayload,
  PortalOnlinePayment,
  PortalTicket,
  PortalTicketDetail,
  PortalTicketListItem,
  PortalWalletBalance,
  PortalWalletTransaction,
} from "@/modules/portal/types";
import type { Department } from "@/modules/tickets/types";

export function usePortalProfile() {
  return useQuery({
    queryKey: ["portal", "profile"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<CustomerDetail>>(
        "/portal/customers/me",
      );
      return response.data.data as CustomerDetail;
    },
  });
}

export function usePortalSubCustomers(enabled = true) {
  return useQuery({
    queryKey: ["portal", "sub-customers"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<CustomerDetail[]>>(
        "/portal/customers/my-sub-customers",
      );
      return response.data.data ?? [];
    },
    enabled,
  });
}

export function usePortalOrders() {
  return useQuery({
    queryKey: ["portal", "orders"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<OrderListItem[]>>(
        "/portal/orders",
      );
      return response.data.data ?? [];
    },
  });
}

export function usePortalOrder(id: string | undefined) {
  return useQuery({
    queryKey: ["portal", "orders", id],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<OrderDetail>>(
        `/portal/orders/${id}`,
      );
      return response.data.data as OrderDetail;
    },
    enabled: Boolean(id),
  });
}

export function usePortalActiveServices() {
  return useQuery({
    queryKey: ["portal", "active-services"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<CustomerService[]>>(
        "/portal/sales/services",
      );
      return response.data.data ?? [];
    },
  });
}

export function usePortalServiceRequests() {
  return useQuery({
    queryKey: ["portal", "service-requests"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<ServiceRequestSummary[]>>(
        "/portal/sales/requests",
      );
      return response.data.data ?? [];
    },
  });
}

export function usePortalInvoices() {
  return useQuery({
    queryKey: ["portal", "invoices"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<InvoiceListItem[]>>(
        "/portal/invoices",
      );
      return response.data.data ?? [];
    },
  });
}

export function usePortalWallet() {
  return useQuery({
    queryKey: ["portal", "wallet"],
    queryFn: async () => {
      const [balanceResponse, transactionsResponse] = await Promise.all([
        portalApi.get<ApiEnvelope<PortalWalletBalance>>("/wallets/my-balance"),
        portalApi.get<ApiEnvelope<PortalWalletTransaction[]>>(
          "/wallets/my-transactions",
        ),
      ]);

      return {
        balance: balanceResponse.data.data,
        transactions: transactionsResponse.data.data ?? [],
      };
    },
  });
}

export function usePortalPayInvoiceFromWallet() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (invoiceId: string) => {
      const response = await portalApi.post<ApiEnvelope<PortalWalletBalance>>(
        "/wallets/pay-invoice",
        { invoiceId },
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["portal"] });
    },
  });
}

export function usePortalInitiateOnline() {
  return useMutation({
    mutationFn: async (invoiceId: string) => {
      const response = await portalApi.post<ApiEnvelope<PortalOnlinePayment>>(
        "/payments/saman/initiate",
        {
          invoiceId,
          callbackUrl: `${window.location.origin}/portal/invoices`,
        },
      );
      return response.data.data as PortalOnlinePayment;
    },
  });
}

export function usePortalTickets() {
  return useQuery({
    queryKey: ["portal", "tickets"],
    queryFn: async () => {
      const response = await portalApi.get<
        ApiEnvelope<PortalTicketListItem[]>
      >("/portal/tickets");
      return response.data.data ?? [];
    },
  });
}

export function usePortalTicket(id: string | undefined) {
  return useQuery({
    queryKey: ["portal", "tickets", id],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<PortalTicketDetail>>(
        `/portal/tickets/${id}`,
      );
      return response.data.data as PortalTicketDetail;
    },
    enabled: Boolean(id),
    refetchInterval: 30_000,
  });
}

export function usePortalCreateTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: PortalCreateTicketPayload) => {
      const response = await portalApi.post<ApiEnvelope<PortalTicket>>(
        "/portal/tickets",
        payload,
      );
      return response.data.data as PortalTicket;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["portal", "tickets"] });
    },
  });
}

export function usePortalAddTicketMessage(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (message: string) => {
      const response = await portalApi.post<ApiEnvelope<PortalTicketDetail>>(
        `/portal/tickets/${id}/messages`,
        { message, isInternalNote: false },
      );
      return response.data.data as PortalTicketDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["portal", "tickets"] });
    },
  });
}

export function usePortalDepartments() {
  return useQuery({
    queryKey: ["portal", "departments"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<Department[]>>(
        "/departments",
      );
      return response.data.data ?? [];
    },
    staleTime: 10 * 60_000,
  });
}

export function usePortalServices() {
  return useQuery({
    queryKey: ["portal", "services"],
    queryFn: async () => {
      const response = await portalApi.get<
        ApiEnvelope<{ items: Array<{ id: string; name: string; code: string }> }>
      >("/catalog/services", {
        params: { page: 1, pageSize: 100, isActive: true },
      });
      return response.data.data?.items ?? [];
    },
    staleTime: 10 * 60_000,
  });
}

export function usePortalUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: {
      phone?: string | null;
      email?: string | null;
      address?: string | null;
    }) => {
      const response = await portalApi.put<ApiEnvelope<CustomerDetail>>(
        "/portal/customers/me",
        payload,
      );
      return response.data.data as CustomerDetail;
    },
    onSuccess: async (customer) => {
      usePortalAuth.getState().setCustomer(customer);
      await queryClient.invalidateQueries({ queryKey: ["portal", "profile"] });
    },
  });
}

export function usePortalCustomerSearch() {
  return useQuery({
    queryKey: ["portal", "customer-search"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<CustomerListItem[]>>(
        "/portal/customers/my-sub-customers",
      );
      return response.data.data ?? [];
    },
  });
}
