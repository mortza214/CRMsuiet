import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import type { AuthResult, AuthUser } from "@/modules/auth/types";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  login: (result: AuthResult) => void;
  setAuth: (result: AuthResult) => void;
  logout: () => void;
  hasPermission: (permission: string) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      user: null,

      login: (result) =>
        set({
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
          user: result.user,
        }),

      setAuth: (result) =>
        set({
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
          user: result.user,
        }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          user: null,
        }),

      hasPermission: (permission) => {
        const permissions = get().user?.permissions ?? [];
        return permissions.includes("*") || permissions.includes(permission);
      },
    }),
    {
      name: "crmsuite.auth",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
      }),
    },
  ),
);
