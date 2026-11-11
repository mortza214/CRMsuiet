import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  PagedResult,
  WorkflowDefinition,
  WorkflowDefinitionSummary,
  WorkflowEntityType,
  WorkflowInstance,
  WorkflowListParams,
  WorkflowState,
  WorkflowTask,
  WorkflowTaskStatus,
  WorkflowTransition,
} from "@/modules/workflow/types";

export function useWorkflowDefinitions(params: WorkflowListParams) {
  return useQuery({
    queryKey: ["workflow", "definitions", params],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<PagedResult<WorkflowDefinitionSummary>>
      >("/workflow/definitions", {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          status: params.status || undefined,
          entityType: params.entityType || undefined,
          search: params.search?.trim() || undefined,
        },
      });
      return response.data.data as PagedResult<WorkflowDefinitionSummary>;
    },
    placeholderData: keepPreviousData,
  });
}

export function useWorkflowDefinition(id: string | undefined) {
  return useQuery({
    queryKey: ["workflow", "definition", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<WorkflowDefinition>>(
        `/workflow/definitions/${id}`,
      );
      return response.data.data as WorkflowDefinition;
    },
    enabled: Boolean(id),
  });
}

export function useCreateWorkflowDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      code: string;
      name: string;
      entityType: WorkflowEntityType;
      description?: string | null;
    }) => {
      const response = await api.post<ApiEnvelope<WorkflowDefinition>>(
        "/workflow/definitions",
        payload,
      );
      return response.data.data as WorkflowDefinition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow", "definitions"] });
    },
  });
}

export function useUpdateWorkflowDraft(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { name: string; description?: string | null }) => {
      const response = await api.put<ApiEnvelope<WorkflowDefinition>>(
        `/workflow/definitions/${id}`,
        payload,
      );
      return response.data.data as WorkflowDefinition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function usePublishWorkflowDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post<ApiEnvelope<WorkflowDefinition>>(
        `/workflow/definitions/${id}/publish`,
      );
      return response.data.data as WorkflowDefinition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useCloneWorkflowDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (sourceId: string) => {
      const response = await api.post<ApiEnvelope<WorkflowDefinition>>(
        `/workflow/definitions/${sourceId}/clone`,
        {},
      );
      return response.data.data as WorkflowDefinition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useReopenWorkflowDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post<ApiEnvelope<WorkflowDefinition>>(
        `/workflow/definitions/${id}/reopen`,
      );
      return response.data.data as WorkflowDefinition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useArchiveWorkflowDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post<ApiEnvelope<WorkflowDefinition>>(
        `/workflow/definitions/${id}/archive`,
      );
      return response.data.data as WorkflowDefinition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useAddWorkflowState(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: Omit<WorkflowState, "id">) => {
      const response = await api.post<ApiEnvelope<WorkflowState>>(
        `/workflow/definitions/${id}/states`,
        payload,
      );
      return response.data.data as WorkflowState;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow", "definition", id] });
    },
  });
}

export function useUpdateWorkflowState(
  id: string | undefined,
  stateId: string | undefined,
) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (
      payload: Omit<WorkflowState, "id"> & { stateId?: string },
    ) => {
      const targetStateId = stateId ?? payload.stateId;
      const { stateId: _stateId, ...body } = payload;
      const response = await api.put<ApiEnvelope<WorkflowState>>(
        `/workflow/definitions/${id}/states/${targetStateId}`,
        body,
      );
      return response.data.data as WorkflowState;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow", "definition", id] });
    },
  });
}

export function useRemoveWorkflowState(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (stateId: string) => {
      await api.delete(`/workflow/definitions/${id}/states/${stateId}`);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow", "definition", id] });
    },
  });
}

export function useAddWorkflowTransition(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: Omit<WorkflowTransition, "id">) => {
      const response = await api.post<ApiEnvelope<WorkflowTransition>>(
        `/workflow/definitions/${id}/transitions`,
        payload,
      );
      return response.data.data as WorkflowTransition;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow", "definition", id] });
    },
  });
}

export function useRemoveWorkflowTransition(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (transitionId: string) => {
      await api.delete(`/workflow/definitions/${id}/transitions/${transitionId}`);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow", "definition", id] });
    },
  });
}

export function useMyWorkflowTasks(
  status?: WorkflowTaskStatus,
  departmentId?: string,
) {
  return useQuery({
    queryKey: ["workflow", "tasks", status, departmentId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<WorkflowTask>>>(
        "/workflow/tasks/my",
        {
          params: {
            status: status || undefined,
            departmentId: departmentId || undefined,
            page: 1,
            pageSize: 100,
          },
        },
      );
      return response.data.data as PagedResult<WorkflowTask>;
    },
  });
}

export function useWorkflowInstance(id: string | undefined) {
  return useQuery({
    queryKey: ["workflow", "instance", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<WorkflowInstance>>(
        `/workflow/instances/${id}`,
      );
      return response.data.data as WorkflowInstance;
    },
    enabled: Boolean(id),
  });
}

export function useInstancesByEntity(
  entityType: WorkflowEntityType | undefined,
  entityId: string | undefined,
) {
  return useQuery({
    queryKey: ["workflow", "by-entity", entityType, entityId],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<WorkflowInstance[]>>(
        "/workflow/instances/by-entity",
        { params: { entityType, entityId } },
      );
      return response.data.data ?? [];
    },
    enabled: Boolean(entityType && entityId),
  });
}

export function useExecuteWorkflowTransition(instanceId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      transitionId: string;
      comment?: string | null;
      payloadJson?: string | null;
      idempotencyKey?: string | null;
    }) => {
      const response = await api.post<ApiEnvelope<WorkflowInstance>>(
        `/workflow/instances/${instanceId}/transition`,
        payload,
      );
      return response.data.data as WorkflowInstance;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useWorkflowExperts(enabled = true) {
  return useQuery({
    queryKey: ["workflow", "experts"],
    queryFn: async () => {
      const response = await api.get<
        ApiEnvelope<Array<{ id: string; fullName: string; departmentIds: string[] }>>
      >("/workflow/experts");
      return response.data.data ?? [];
    },
    enabled,
    staleTime: 5 * 60_000,
  });
}

export function useExecuteWorkflowAction(instanceId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      transitionId?: string;
      actionDataJson?: string | null;
    }) => {
      const response = await api.post<ApiEnvelope<WorkflowInstance>>(
        `/workflow/instances/${instanceId}/action`,
        payload,
      );
      return response.data.data as WorkflowInstance;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useCancelWorkflowInstance(instanceId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (reason?: string | null) => {
      const response = await api.post<ApiEnvelope<WorkflowInstance>>(
        `/workflow/instances/${instanceId}/cancel`,
        { reason },
      );
      return response.data.data as WorkflowInstance;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}

export function useReassignWorkflowTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      taskId,
      userId,
      roleId,
      departmentId,
    }: {
      taskId: string;
      userId?: string | null;
      roleId?: string | null;
      departmentId?: string | null;
    }) => {
      const response = await api.post<ApiEnvelope<WorkflowTask>>(
        `/workflow/tasks/${taskId}/reassign`,
        { userId, roleId, departmentId },
      );
      return response.data.data as WorkflowTask;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workflow"] });
    },
  });
}
