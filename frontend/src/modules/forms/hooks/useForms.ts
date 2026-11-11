import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  FormDefinitionStatus,
  FormEntityType,
  FormSubmission,
  FormSubmissionStatus,
  FormTemplate,
  PagedResult,
} from "@/modules/forms/types";

export function useFormTemplates(includeInactive = true) {
  return useQuery({
    queryKey: ["forms", "templates", includeInactive],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<FormTemplate[]>>(
        "/forms/templates",
        { params: { includeInactive } },
      );
      return response.data.data ?? [];
    },
  });
}

export function useFormTemplate(id: string | undefined) {
  return useQuery({
    queryKey: ["forms", "template", id],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<FormTemplate>>(
        `/forms/templates/${id}`,
      );
      return response.data.data as FormTemplate;
    },
    enabled: Boolean(id),
  });
}

export function useFormTemplateByCode(code: string | undefined) {
  return useQuery({
    queryKey: ["forms", "template-by-code", code],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<FormTemplate>>(
        `/forms/templates/by-code/${encodeURIComponent(code ?? "")}`,
      );
      return response.data.data as FormTemplate;
    },
    enabled: Boolean(code),
  });
}

export function useCreateFormTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      code: string;
      title: string;
      schemaJson: string;
      entityType: FormEntityType;
      isActive?: boolean;
      targetServiceId?: string | null;
      targetServiceCategoryId?: string | null;
    }) => {
      const response = await api.post<ApiEnvelope<FormTemplate>>(
        "/forms/templates",
        {
          ...payload,
          version: 1,
          isActive: payload.isActive ?? false,
        },
      );
      return response.data.data as FormTemplate;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["forms", "templates"] });
    },
  });
}

export function useUpdateFormTemplate(id: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      code: string;
      title: string;
      schemaJson: string;
      entityType: FormEntityType;
      targetServiceId?: string | null;
      targetServiceCategoryId?: string | null;
    }) => {
      const response = await api.put<ApiEnvelope<FormTemplate>>(
        `/forms/templates/${id}`,
        payload,
      );
      return response.data.data as FormTemplate;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["forms"] });
    },
  });
}

export function usePublishFormTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post<ApiEnvelope<FormTemplate>>(
        `/forms/templates/${id}/publish`,
      );
      return response.data.data as FormTemplate;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["forms"] });
    },
  });
}

export function useArchiveFormTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post<ApiEnvelope<FormTemplate>>(
        `/forms/templates/${id}/archive`,
      );
      return response.data.data as FormTemplate;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["forms"] });
    },
  });
}

export function useFormSubmissions(params: {
  page: number;
  pageSize: number;
  formTemplateId?: string;
  customerId?: string;
  entityType?: FormEntityType;
  entityId?: string;
  status?: FormSubmissionStatus;
}) {
  return useQuery({
    queryKey: ["forms", "submissions", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<FormSubmission>>>(
        "/forms/submissions",
        {
          params: {
            page: params.page,
            pageSize: params.pageSize,
            formTemplateId: params.formTemplateId || undefined,
            customerId: params.customerId || undefined,
            entityType: params.entityType || undefined,
            entityId: params.entityId || undefined,
            status: params.status || undefined,
          },
        },
      );
      return response.data.data as PagedResult<FormSubmission>;
    },
  });
}

export function useReviewFormSubmission() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      submissionId,
      approve,
      note,
    }: {
      submissionId: string;
      approve: boolean;
      note?: string;
    }) => {
      const response = await api.post<ApiEnvelope<FormSubmission>>(
        `/forms/submissions/${submissionId}/review`,
        { approve, note },
      );
      return response.data.data as FormSubmission;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["forms", "submissions"] });
    },
  });
}

export const formStatusLabels: Record<FormDefinitionStatus, string> = {
  Draft: "پیش‌نویس",
  Published: "منتشر شده",
  Archived: "آرشیو شده",
};

export const submissionStatusLabels: Record<FormSubmissionStatus, string> = {
  Draft: "پیش‌نویس",
  Submitted: "ارسال شده",
  Approved: "تأیید شده",
  Rejected: "رد شده",
};
