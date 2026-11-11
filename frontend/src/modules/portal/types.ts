import type { TicketDetail, TicketListItem, TicketPriority } from "@/modules/tickets/types";
import type { WalletBalance, WalletTransaction } from "@/modules/wallet/types";

export type PortalWalletBalance = WalletBalance;
export type PortalWalletTransaction = WalletTransaction;
export type PortalTicketListItem = TicketListItem;
export type PortalTicketDetail = TicketDetail;
export type PortalTicket = TicketDetail;

export interface PortalCreateTicketPayload {
  serviceId?: string | null;
  subject: string;
  departmentId: string;
  priority: TicketPriority;
  message: string;
}

export interface PortalOnlinePayment {
  transactionId: string;
  trackingCode: string;
  token: string;
  paymentUrl: string;
}
