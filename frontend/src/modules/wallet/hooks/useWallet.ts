import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import type {
  MyWalletData,
  WalletBalance,
  WalletTransaction,
} from "@/modules/wallet/types";

export function useMyWallet() {
  return useQuery({
    queryKey: ["wallet", "my"],
    queryFn: async () => {
      const [balanceResponse, transactionsResponse] = await Promise.all([
        api.get<ApiEnvelope<WalletBalance>>("/wallets/my-balance"),
        api.get<ApiEnvelope<WalletTransaction[]>>("/wallets/my-transactions"),
      ]);

      return {
        balance: balanceResponse.data.data as WalletBalance,
        transactions: transactionsResponse.data.data ?? [],
      } satisfies MyWalletData;
    },
  });
}

export function useRequestWithdraw() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (amount: number) => {
      const response = await api.post<ApiEnvelope<unknown>>(
        "/wallets/withdraw",
        { amount },
      );
      return response.data.data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["wallet"] });
      await queryClient.invalidateQueries({ queryKey: ["commissions"] });
    },
  });
}
