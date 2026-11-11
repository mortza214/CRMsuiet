import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import { useCustomers } from "@/modules/customers/hooks/useCustomers";
import type {
  BadDebtor,
  Debtor,
  FinancialSummary,
  FinancialTrendPoint,
  LedgerEntry,
  ReportCustomer,
  SalesReportItem,
} from "@/modules/reports/types";

interface DateRangeParams {
  fromUtc: string;
  toUtc: string;
}

export function useFinancialSummary(params: DateRangeParams) {
  return useQuery({
    queryKey: ["reports", "financial-summary", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<FinancialSummary>>(
        "/reports/financial-summary",
        {
          params: {
            fromUtc: params.fromUtc,
            toUtc: params.toUtc,
          },
        },
      );
      return response.data.data as FinancialSummary;
    },
    placeholderData: keepPreviousData,
  });
}

export function useFinancialTrend(params: DateRangeParams) {
  return useQuery({
    queryKey: ["reports", "financial-trend", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<FinancialTrendPoint[]>>(
        "/reports/financial-trend",
        {
          params: {
            fromUtc: params.fromUtc,
            toUtc: params.toUtc,
          },
        },
      );
      return response.data.data ?? [];
    },
  });
}

export function useDebtors(asOfUtc: string) {
  return useQuery({
    queryKey: ["reports", "debtors", asOfUtc],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<Debtor[]>>(
        "/reports/debtors",
        { params: { asOfUtc } },
      );
      return response.data.data ?? [];
    },
  });
}

export function useBadDebtors(asOfUtc: string) {
  return useQuery({
    queryKey: ["reports", "bad-debtors", asOfUtc],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<BadDebtor[]>>(
        "/reports/bad-debtors",
        { params: { asOfUtc } },
      );
      return response.data.data ?? [];
    },
  });
}

export function useCustomerLedger(customerId: string | undefined) {
  return useQuery({
    queryKey: ["reports", "ledger", customerId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<LedgerEntry[]>>(
        `/reports/customers/${customerId}/ledger`,
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(customerId),
  });
}

export function useSalesReport(params: DateRangeParams) {
  return useQuery({
    queryKey: ["reports", "sales", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<SalesReportItem[]>>(
        "/reports/sales",
        {
          params: {
            fromUtc: params.fromUtc,
            toUtc: params.toUtc,
          },
        },
      );
      return response.data.data ?? [];
    },
  });
}

export function useReportCustomers(search: string) {
  const customers = useCustomers({
    page: 1,
    pageSize: 100,
    search: search.trim() || undefined,
  });

  return {
    ...customers,
    data: (customers.data?.items ?? []).map<ReportCustomer>((customer) => ({
      id: customer.id,
      displayName: customer.displayName,
      mobile: customer.mobile,
    })),
  };
}
