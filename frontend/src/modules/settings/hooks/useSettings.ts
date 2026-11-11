import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  AppRole,
  AppUser,
  Department,
  GatewayConfig,
  IppanelSettings,
  PagedResult,
  SmsResult,
  PermissionGroup,
  SettingGroup,
  SettingValue,
  TaxRule,
  RolePayload,
  UserListParams,
  UserPayload,
} from "@/modules/settings/types";

export function useUsers(params: UserListParams, enabled = true) {
  return useQuery({
    queryKey: ["settings", "users", params],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PagedResult<AppUser>>>(
        "/users",
        {
          params: {
            page: params.page,
            pageSize: params.pageSize,
            search: params.search?.trim() || undefined,
          },
        },
      );
      return response.data.data as PagedResult<AppUser>;
    },
    enabled,
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: UserPayload) => {
      const response = await api.post<ApiEnvelope<AppUser>>("/users", payload);
      return response.data.data as AppUser;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "users"] });
    },
  });
}

export function useUpdateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      payload,
    }: {
      id: string;
      payload: UserPayload;
    }) => {
      const response = await api.put<ApiEnvelope<AppUser>>(
        `/users/${id}`,
        {
          fullName: payload.fullName,
          mobile: payload.mobile,
          email: payload.email ?? null,
          isActive: payload.isActive,
          newPassword: payload.password || null,
          roleIds: payload.roleIds,
          departmentIds: payload.departmentIds,
        },
      );
      return response.data.data as AppUser;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "users"] });
    },
  });
}

export function useRoles(enabled = true) {
  return useQuery({
    queryKey: ["settings", "roles"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<AppRole[]>>("/roles");
      return response.data.data ?? [];
    },
    enabled,
  });
}

export function useCreateRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: RolePayload) => {
      const response = await api.post<ApiEnvelope<AppRole>>("/roles", payload);
      return response.data.data as AppRole;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "roles"] });
    },
  });
}

export function useDepartments(enabled = true) {
  return useQuery({
    queryKey: ["settings", "departments"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<Department[]>>("/departments");
      return response.data.data ?? [];
    },
    enabled,
  });
}

export function useCreateDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { name: string; parentId?: string | null }) => {
      const response = await api.post<ApiEnvelope<Department>>(
        "/departments",
        payload,
      );
      return response.data.data as Department;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "departments"] });
    },
  });
}

export function useDeleteDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/departments/${id}`);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "departments"] });
    },
  });
}

export function usePermissions() {
  return useQuery({
    queryKey: ["settings", "permissions"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<PermissionGroup[]>>(
        "/permissions",
      );
      return response.data.data ?? [];
    },
    staleTime: 10 * 60_000,
  });
}

export function useUpdateRolePermissions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      roleId,
      permissionIds,
    }: {
      roleId: string;
      permissionIds: string[];
    }) => {
      const response = await api.put<ApiEnvelope<AppRole>>(
        `/roles/${roleId}/permissions`,
        { permissionIds },
      );
      return response.data.data as AppRole;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "roles"] });
    },
  });
}

export function useTestNotification() {
  return useMutation({
    mutationFn: async (payload: {
      channel: "Sms" | "Bale";
      recipient: string;
      message: string;
    }) => {
      const response = await api.post<ApiEnvelope<unknown>>(
        "/settings/notifications/test",
        payload,
      );
      return response.data;
    },
  });
}

export function useSettingsGroup(group: string) {
  return useQuery({
    queryKey: ["settings", "group", group],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<SettingGroup[]>>("/settings", {
        params: { group },
      });
      return (response.data.data ?? []).flatMap((settingGroup) => settingGroup.items);
    },
  });
}

export function useUpdateSettings(group: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (
      items: Array<{ key: string; value: string; group: string; isSecret?: boolean }>,
    ) => {
      const response = await api.put<ApiEnvelope<SettingValue[]>>("/settings", {
        items,
      });
      return response.data.data ?? [];
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "group", group] });
    },
  });
}

export function useGateways() {
  return useQuery({
    queryKey: ["settings", "gateways"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<GatewayConfig[]>>(
        "/settings/gateways",
      );
      return response.data.data ?? [];
    },
  });
}

export function useUpdateGateway() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: {
      provider: string;
      isActive: boolean;
      merchantId: string;
      terminalId: string;
    }) => {
      const response = await api.put<ApiEnvelope<GatewayConfig>>(
        "/settings/gateways",
        payload,
      );
      return response.data.data as GatewayConfig;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "gateways"] });
    },
  });
}

export function useResetTestData() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const response = await api.post<ApiEnvelope<unknown>>(
        "/system/reset-test-data",
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries();
    },
  });
}

export function useResetRequests() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const response = await api.post<ApiEnvelope<unknown>>(
        "/system/reset-requests",
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries();
    },
  });
}

export function useIppanelSettings() {
  return useQuery({
    queryKey: ["settings", "ippanel"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<IppanelSettings>>(
        "/settings/sms/ippanel",
      );
      return response.data.data as IppanelSettings;
    },
  });
}

export function useUpdateIppanelSettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      enabled: boolean;
      apiKey?: string | null;
      fromNumber: string;
      patternCode: string;
      baseUrl: string;
      patternParamName: string;
    }) => {
      const response = await api.put<ApiEnvelope<IppanelSettings>>(
        "/settings/sms/ippanel",
        payload,
      );
      return response.data.data as IppanelSettings;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "ippanel"] });
    },
  });
}

export function useTestIppanelSettings() {
  return useMutation({
    mutationFn: async (mobile: string) => {
      const response = await api.post<ApiEnvelope<SmsResult>>(
        "/settings/sms/ippanel/test",
        { mobile },
      );
      return response.data.data as SmsResult;
    },
  });
}

export function useTaxRules() {
  return useQuery({
    queryKey: ["settings", "tax-rules"],
    queryFn: async () => {
      const response = await api.get<ApiEnvelope<TaxRule[]>>(
        "/settings/tax-rules",
      );
      return response.data.data ?? [];
    },
  });
}

export function useCreateTaxRule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: {
      name: string;
      percentage: number;
      isActive: boolean;
      effectiveFromUtc: string;
    }) => {
      const response = await api.post<ApiEnvelope<TaxRule>>(
        "/settings/tax-rules",
        payload,
      );
      return response.data.data as TaxRule;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "tax-rules"] });
    },
  });
}

export function useUpdateTaxRule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      payload,
    }: {
      id: string;
      payload: {
        name: string;
        percentage: number;
        isActive: boolean;
        effectiveFromUtc: string;
      };
    }) => {
      const response = await api.put<ApiEnvelope<TaxRule>>(
        `/settings/tax-rules/${id}`,
        payload,
      );
      return response.data.data as TaxRule;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["settings", "tax-rules"] });
    },
  });
}
