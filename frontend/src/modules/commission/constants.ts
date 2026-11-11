import type {
  CommissionPayoutType,
  CommissionStatus,
} from "@/modules/commission/types";

export const commissionStatusMeta: Record<
  CommissionStatus,
  { label: string; color: string }
> = {
  Calculated: { label: "محاسبه شده", color: "orange" },
  Payable: { label: "قابل پرداخت", color: "blue" },
  Rejected: { label: "رد شده", color: "red" },
  Paid: { label: "پرداخت شده", color: "green" },
  Pending: { label: "در انتظار تأیید مالی", color: "gold" },
  Approved: { label: "تأییدشده", color: "cyan" },
};

export const commissionStatusOptions = (
  Object.keys(commissionStatusMeta) as CommissionStatus[]
).map((value) => ({
  value,
  label: commissionStatusMeta[value].label,
}));

export const payoutTypeLabels: Record<CommissionPayoutType, string> = {
  OneTime: "یک‌بار",
  Monthly: "ماهیانه",
};
