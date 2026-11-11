import type { OrderStatus } from "@/modules/orders/types";

export const orderStatusMeta: Record<
  OrderStatus,
  { label: string; color: string }
> = {
  Draft: { label: "پیش‌نویس", color: "default" },
  PendingPayment: { label: "در انتظار پرداخت", color: "orange" },
  Paid: { label: "پرداخت شده", color: "blue" },
  Provisioning: { label: "در حال تحویل", color: "purple" },
  Completed: { label: "تکمیل شده", color: "green" },
  Canceled: { label: "لغو شده", color: "red" },
  Pending: { label: "در انتظار بررسی", color: "gold" },
};

export const orderStatusOptions = (
  Object.keys(orderStatusMeta) as OrderStatus[]
).map((value) => ({
  value,
  label: orderStatusMeta[value].label,
}));

export const nextOrderStatuses: Record<OrderStatus, OrderStatus[]> = {
  Draft: ["Pending", "PendingPayment", "Canceled"],
  Pending: ["PendingPayment", "Provisioning", "Canceled"],
  PendingPayment: ["Paid", "Canceled"],
  Paid: ["Provisioning"],
  Provisioning: ["Completed"],
  Completed: [],
  Canceled: [],
};

export function getOrderStatusLabel(status: OrderStatus): string {
  return orderStatusMeta[status]?.label ?? status;
}
