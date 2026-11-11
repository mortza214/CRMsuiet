import type {
  TicketPriority,
  TicketStatus,
} from "@/modules/tickets/types";

export const ticketPriorityMeta: Record<
  TicketPriority,
  { label: string; color: string }
> = {
  Low: { label: "کم", color: "default" },
  Medium: { label: "متوسط", color: "blue" },
  High: { label: "بالا", color: "orange" },
  Critical: { label: "بحرانی", color: "red" },
};

export const ticketPriorityOptions = (
  Object.keys(ticketPriorityMeta) as TicketPriority[]
).map((value) => ({
  value,
  label: ticketPriorityMeta[value].label,
}));

export const ticketStatusMeta: Record<
  TicketStatus,
  { label: string; color: string }
> = {
  Open: { label: "باز", color: "blue" },
  InProgress: { label: "در حال بررسی", color: "processing" },
  PendingCustomer: { label: "منتظر پاسخ مشتری", color: "orange" },
  Resolved: { label: "حل شده", color: "green" },
  Closed: { label: "بسته شده", color: "default" },
};

export const ticketStatusOptions = (
  Object.keys(ticketStatusMeta) as TicketStatus[]
).map((value) => ({
  value,
  label: ticketStatusMeta[value].label,
}));

export const nextTicketStatuses: Record<TicketStatus, TicketStatus[]> = {
  Open: ["InProgress", "PendingCustomer", "Resolved"],
  InProgress: ["PendingCustomer", "Resolved"],
  PendingCustomer: ["InProgress", "Resolved"],
  Resolved: ["Closed", "InProgress"],
  Closed: [],
};

export const ticketSourceLabels = {
  Portal: "پورتال",
  Phone: "تلفن",
  Internal: "داخلی",
} as const;
