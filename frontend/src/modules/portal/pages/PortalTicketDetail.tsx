import { useState } from "react";
import {
  ArrowRightOutlined,
  MessageOutlined,
  SendOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Empty,
  Input,
  Result,
  Space,
  Spin,
  Typography,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import {
  ticketPriorityMeta,
  ticketStatusMeta,
} from "@/modules/tickets/constants";
import type { TicketMessage } from "@/modules/tickets/types";
import { getPortalErrorMessage } from "@/modules/portal/api";
import {
  usePortalAddTicketMessage,
  usePortalTicket,
} from "@/modules/portal/hooks/usePortal";

export function PortalTicketDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [reply, setReply] = useState("");
  const ticket = usePortalTicket(id);
  const addMessage = usePortalAddTicketMessage(id);

  if (ticket.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (ticket.isError || !ticket.data) {
    return (
      <Result
        status="404"
        title="تیکت یافت نشد"
        extra={
          <Button type="primary" onClick={() => navigate("/portal/tickets")}>
            بازگشت به تیکت‌ها
          </Button>
        }
      />
    );
  }

  const data = ticket.data;
  const priority = ticketPriorityMeta[data.priority];
  const status = ticketStatusMeta[data.status];
  const messages = data.messages.filter((item) => !item.isInternalNote);

  const send = async () => {
    if (!reply.trim()) {
      message.warning("متن پاسخ را وارد کنید.");
      return;
    }

    try {
      await addMessage.mutateAsync(reply.trim());
      setReply("");
      message.success("پاسخ شما ثبت شد.");
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    }
  };

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/portal/tickets")}
            aria-label="بازگشت"
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} className="detail-title">
                {data.subject}
              </Typography.Title>
              <span
                className="portal-priority-tag"
                style={{ borderColor: priority.color }}
              >
                {priority.label}
              </span>
              <span className="portal-status-tag" style={{ color: status.color }}>
                {status.label}
              </span>
            </Space>
            <Typography.Text type="secondary">
              {data.ticketNumber} — {data.createdAtJalali}
            </Typography.Text>
          </div>
        </Space>
      </div>

      <Card className="portal-table-card" variant="borderless">
        <div className="portal-ticket-chat">
          {messages.length === 0 ? (
            <Empty description="هنوز پیامی ثبت نشده است." />
          ) : (
            messages.map((item) => (
              <PortalMessage key={item.id} message={item} />
            ))
          )}
        </div>

        <div className="ticket-reply-box">
          <Input.TextArea
            value={reply}
            onChange={(event) => setReply(event.target.value)}
            rows={4}
            maxLength={4000}
            showCount
            placeholder="پاسخ خود را بنویسید..."
          />
          <div className="ticket-reply-actions">
            <span />
            <Button
              type="primary"
              icon={<SendOutlined />}
              loading={addMessage.isPending}
              onClick={() => void send()}
            >
              ارسال پاسخ
            </Button>
          </div>
        </div>
      </Card>

      <Typography.Text type="secondary" className="portal-polling-note">
        <MessageOutlined /> پاسخ‌ها به‌صورت خودکار هر ۳۰ ثانیه به‌روزرسانی می‌شوند.
      </Typography.Text>
    </Space>
  );
}

function PortalMessage({ message }: { message: TicketMessage }) {
  const isCustomer = message.senderType === "Customer";

  return (
    <div
      className={`ticket-message-row ${
        isCustomer ? "ticket-message-customer" : "ticket-message-employee"
      }`}
    >
      <div className="ticket-bubble">
        <div className="ticket-bubble-meta">
          <strong>{isCustomer ? "شما" : "پشتیبانی"}</strong>
          <span>{message.createdAtJalali}</span>
        </div>
        <div className="ticket-bubble-text">{message.message}</div>
      </div>
    </div>
  );
}
