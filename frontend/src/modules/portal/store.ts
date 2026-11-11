import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import type { CustomerDetail } from "@/modules/customers/types";

export interface PortalAuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  accessTokenExpiresAtJalali: string;
  customer: CustomerDetail;
}

interface PortalAuthState {
  accessToken: string | null;
  customer: CustomerDetail | null;
  login: (result: PortalAuthResult) => void;
  setCustomer: (customer: CustomerDetail) => void;
  logout: () => void;
}

export const usePortalAuth = create<PortalAuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      customer: null,
      login: (result) =>
        set({ accessToken: result.accessToken, customer: result.customer }),
      setCustomer: (customer) => set({ customer }),
      logout: () => set({ accessToken: null, customer: null }),
    }),
    {
      name: "crmsuite.portal.auth",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        accessToken: state.accessToken,
        customer: state.customer,
      }),
    },
  ),
);
