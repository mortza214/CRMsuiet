import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  CreateTicketPayload,
  Department,
  PagedResult,
  TicketAssignee,
  TicketDetail,
  TicketListItem,
  TicketListParams,
  TicketStatus,
} from "@/modules/tickets/types";

export function useTickets(params: TicketListParams) {
  return useQuery({
    queryKey: ["tickets", "list", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<TicketListItem>>>(
        "/tickets",
        {
          params: {
            page: params.page,
            pageSize: params.pageSize,
            search: params.search?.trim() || undefined,
            status: params.status || undefined,
            priority: params.priority || undefined,
          },
        },
      );
      return response.data.data as PagedResult<TicketListItem>;
    },
    placeholderData: keepPreviousData,
  });
}

export function useTicketDetails(id: string | undefined) {
  return useQuery({
    queryKey: ["tickets", "detail", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<TicketDetail>>(
        `/tickets/${id}`,
      );
      return response.data.data as TicketDetail;
    },
    enabled: Boolean(id),
    refetchInterval: 30_000,
  });
}

export function useCreateTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: CreateTicketPayload) => {
      const response = await api.post<ApiEnvelope<TicketDetail>>(
        "/tickets",
        payload,
      );
      return response.data.data as TicketDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
  });
}

export function useAddTicketMessage(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      message,
      isInternalNote,
    }: {
      message: string;
      isInternalNote: boolean;
    }) => {
      const response = await api.post<ApiEnvelope<TicketDetail>>(
        `/tickets/${id}/messages`,
        { message, isInternalNote },
      );
      return response.data.data as TicketDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
  });
}

export function useChangeTicketStatus(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (status: TicketStatus) => {
      const response = await api.put<ApiEnvelope<TicketDetail>>(
        `/tickets/${id}/status`,
        { status, note: null },
      );
      return response.data.data as TicketDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
  });
}

export function useAssignTicket(id: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (assignedToUserId: string | null) => {
      const response = await api.put<ApiEnvelope<TicketDetail>>(
        `/tickets/${id}/assign`,
        { assignedToUserId },
      );
      return response.data.data as TicketDetail;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
  });
}

export function useDepartments(enabled = true) {
  return useQuery({
    queryKey: ["departments"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<Department[]>>(
        "/departments",
      );
      return response.data.data ?? [];
    },
    staleTime: 10 * 60_000,
    enabled,
  });
}

export function useTicketAssignees(departmentId: string | undefined) {
  return useQuery({
    queryKey: ["tickets", "assignees", departmentId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<TicketAssignee>>>(
        "/users",
        { params: { page: 1, pageSize: 200, isActive: true } },
      );
      const users = response.data.data?.items ?? [];
      if (!departmentId) return users;
      return users.filter((user) =>
        user.departments.some((department) => department.id === departmentId),
      );
    },
    enabled: Boolean(departmentId),
    staleTime: 2 * 60_000,
  });
}
