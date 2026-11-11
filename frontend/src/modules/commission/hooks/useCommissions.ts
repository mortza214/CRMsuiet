import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  CommissionEntry,
  CommissionListParams,
  CommissionStatus,
  CreateCommissionRulePayload,
  PagedResult,
} from "@/modules/commission/types";

export function useCommissions(params: CommissionListParams) {
  return useQuery({
    queryKey: ["commissions", "list", params],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<CommissionEntry>>
      >("/commissions", {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          status: params.status || undefined,
          employeeId: params.employeeId || undefined,
          fromUtc: params.fromUtc || undefined,
          toUtc: params.toUtc || undefined,
        },
      });
      return response.data.data as PagedResult<CommissionEntry>;
    },
    placeholderData: keepPreviousData,
  });
}

export function useMyCommissions(status?: CommissionStatus) {
  return useQuery({
    queryKey: ["commissions", "my", status],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<CommissionEntry[]>>(
        "/commissions/my",
        { params: { status: status || undefined } },
      );
      return response.data.data ?? [];
    },
  });
}

export function useApproveCommission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.put<ApiEnvelope<CommissionEntry>>(
        `/commissions/${id}/approve`,
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["commissions"] });
      await queryClient.invalidateQueries({ queryKey: ["wallet"] });
    },
  });
}

export function useRejectCommission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.put<ApiEnvelope<CommissionEntry>>(
        `/commissions/${id}/reject`,
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["commissions"] });
    },
  });
}

export function useUpdateCommissionShares() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      shares,
    }: {
      id: string;
      shares: Array<{ userId?: string | null; percentage: number; isCompanyShare: boolean }>;
    }) => {
      const response = await api.put<ApiEnvelope<CommissionEntry>>(
        `/commissions/${id}/shares`,
        { shares },
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["commissions"] });
    },
  });
}

export function useCreateCommissionRule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: CreateCommissionRulePayload) => {
      const response = await api.post<ApiEnvelope<unknown>>(
        "/commissions/rules",
        payload,
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["commissions"] });
    },
  });
}
