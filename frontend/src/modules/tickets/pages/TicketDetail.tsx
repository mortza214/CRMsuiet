import { useState } from "react";
import {
  ArrowRightOutlined,
  DownOutlined,
  LockOutlined,
  SendOutlined,
  SwapOutlined,
  UserSwitchOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Checkbox,
  Descriptions,
  Dropdown,
  Empty,
  Input,
  Modal,
  Result,
  Select,
  Space,
  Spin,
  Tag,
  Typography,
  type MenuProps,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import {
  nextTicketStatuses,
  ticketPriorityMeta,
  ticketSourceLabels,
  ticketStatusMeta,
} from "@/modules/tickets/constants";
import {
  useAddTicketMessage,
  useAssignTicket,
  useChangeTicketStatus,
  useTicketAssignees,
  useTicketDetails,
} from "@/modules/tickets/hooks/useTickets";
import type {
  TicketMessage,
  TicketStatus,
} from "@/modules/tickets/types";

export function TicketDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canReply = hasPermission("Tickets.Ticket.Reply");
  const canClose = hasPermission("Tickets.Ticket.Close");
  const canAssign = hasPermission("Tickets.Ticket.Assign");
  const [reply, setReply] = useState("");
  const [isInternalNote, setIsInternalNote] = useState(false);
  const [assignOpen, setAssignOpen] = useState(false);
  const [assigneeId, setAssigneeId] = useState<string | undefined>();
  const ticket = useTicketDetails(id);
  const addMessage = useAddTicketMessage(id);
  const changeStatus = useChangeTicketStatus(id);
  const assignTicket = useAssignTicket(id);
  const assignees = useTicketAssignees(ticket.data?.departmentId);

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
        subTitle="تیکت موردنظر وجود ندارد یا دسترسی به آن برای دپارتمان شما مجاز نیست."
        extra={
          <Button type="primary" onClick={() => navigate("/tickets")}>
            بازگشت به تیکت‌ها
          </Button>
        }
      />
    );
  }

  const data = ticket.data;
  const status = ticketStatusMeta[data.status];
  const priority = ticketPriorityMeta[data.priority];
  const transitions = nextTicketStatuses[data.status] ?? [];

  const statusMenu: MenuProps = {
    items: transitions.map((next) => ({
      key: next,
      label: ticketStatusMeta[next]?.label ?? next,
    })),
    onClick: async ({ key }) => {
      try {
        await changeStatus.mutateAsync(key as TicketStatus);
        message.success("وضعیت تیکت تغییر کرد.");
      } catch {
        // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
      }
    },
  };

  const sendReply = async () => {
    if (!reply.trim()) {
      message.warning("متن پاسخ را وارد کنید.");
      return;
    }

    try {
      await addMessage.mutateAsync({
        message: reply.trim(),
        isInternalNote,
      });
      setReply("");
      setIsInternalNote(false);
      message.success("پاسخ ثبت شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const submitAssign = async () => {
    try {
      await assignTicket.mutateAsync(assigneeId ?? null);
      message.success(
        assigneeId ? "تیکت به کارشناس انتخاب‌شده ارجاع شد." : "ارجاع تیکت برداشته شد.",
      );
      setAssignOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const openAssign = () => {
    setAssigneeId(data.assignedToUserId ?? undefined);
    setAssignOpen(true);
  };

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="detail-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/tickets")}
            aria-label="بازگشت"
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} className="detail-title">
                {data.subject}
              </Typography.Title>
              <Tag color={priority.color} bordered={false}>
                {priority.label}
              </Tag>
              <Tag color={status.color} bordered={false}>
                {status.label}
              </Tag>
            </Space>
            <Typography.Text type="secondary" dir="ltr">
              {data.ticketNumber}
            </Typography.Text>
          </div>
        </Space>

        <Space size="middle" wrap>
          {canAssign && (
            <Button icon={<UserSwitchOutlined />} onClick={openAssign}>
              ارجاع به کارشناس
            </Button>
          )}
          {canClose && transitions.length > 0 && (
            <Dropdown menu={statusMenu} trigger={["click"]}>
              <Button loading={changeStatus.isPending} icon={<SwapOutlined />}>
                تغییر وضعیت <DownOutlined />
              </Button>
            </Dropdown>
          )}
        </Space>
      </div>

      <RowLayout>
        <Card className="ticket-info-card" variant="borderless">
          <Typography.Title level={4}>اطلاعات تیکت</Typography.Title>
          <Descriptions column={1} size="small" colon={false}>
            <Descriptions.Item label="مشتری">
              <Typography.Link
                onClick={() => navigate(`/customers/${data.customerId}`)}
              >
                {data.customerName || "مشاهده مشتری"}
              </Typography.Link>
            </Descriptions.Item>
            <Descriptions.Item label="دپارتمان">
              {data.departmentName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="کارشناس مسئول">
              {data.assignedToName || "ارجاع نشده"}
            </Descriptions.Item>
            <Descriptions.Item label="سرویس مرتبط">
              {data.serviceName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="منبع">
              {ticketSourceLabels[data.source] ?? data.source}
            </Descriptions.Item>
            <Descriptions.Item label="تاریخ ایجاد">
              {data.createdAtJalali}
            </Descriptions.Item>
          </Descriptions>
        </Card>

        <Card className="ticket-conversation-card" variant="borderless">
          <div className="ticket-conversation-header">
            <Typography.Title level={4}>گفتگو</Typography.Title>
            <Typography.Text type="secondary" className="polling-note">
              به‌روزرسانی خودکار هر ۳۰ ثانیه
            </Typography.Text>
          </div>

          <div className="ticket-messages">
            {data.messages.length === 0 ? (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description="هنوز پیامی ثبت نشده است."
              />
            ) : (
              data.messages.map((item) => (
                <MessageBubble key={item.id} message={item} />
              ))
            )}
          </div>

          {canReply && (
            <div className="ticket-reply-box">
              <Input.TextArea
                value={reply}
                onChange={(event) => setReply(event.target.value)}
                rows={4}
                placeholder="پاسخ خود را بنویسید..."
                maxLength={4000}
                showCount
              />
              <div className="ticket-reply-actions">
                <Checkbox
                  checked={isInternalNote}
                  onChange={(event) => setIsInternalNote(event.target.checked)}
                >
                  <LockOutlined /> ارسال به عنوان یادداشت داخلی (مشتری نمی‌بیند)
                </Checkbox>
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  loading={addMessage.isPending}
                  onClick={() => void sendReply()}
                >
                  ارسال پاسخ
                </Button>
              </div>
            </div>
          )}
        </Card>
      </RowLayout>

      <Modal
        title="ارجاع تیکت به کارشناس"
        open={assignOpen}
        onCancel={() => setAssignOpen(false)}
        onOk={() => void submitAssign()}
        okText="ثبت ارجاع"
        cancelText="انصراف"
        confirmLoading={assignTicket.isPending}
      >
        <Typography.Paragraph type="secondary">
          دپارتمان: {data.departmentName || "-"}
        </Typography.Paragraph>
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          value={assigneeId}
          onChange={setAssigneeId}
          loading={assignees.isLoading}
          placeholder="انتخاب کارشناس از دپارتمان"
          style={{ width: "100%" }}
          options={(assignees.data ?? []).map((user) => ({
            value: user.id,
            label: user.fullName,
          }))}
        />
        <Typography.Paragraph type="secondary" className="assign-hint">
          در صورت خالی گذاشتن، ارجاع فعلی برداشته می‌شود.
        </Typography.Paragraph>
      </Modal>
    </Space>
  );
}

function RowLayout({ children }: { children: React.ReactNode }) {
  return <div className="ticket-layout">{children}</div>;
}

function MessageBubble({ message }: { message: TicketMessage }) {
  const isCustomer = message.senderType === "Customer";
  const isInternal = Boolean(message.isInternalNote);

  return (
    <div
      className={`ticket-message-row ${
        isCustomer ? "ticket-message-customer" : "ticket-message-employee"
      }`}
    >
      <div
        className={`ticket-bubble ${
          isInternal ? "ticket-bubble-internal" : ""
        }`}
      >
        <div className="ticket-bubble-meta">
          <strong>{isCustomer ? "مشتری" : "کارمند"}</strong>
          {isInternal && (
            <Tag color="gold" bordered={false}>
              یادداشت داخلی
            </Tag>
          )}
          <span>{message.createdAtJalali}</span>
        </div>
        <div className="ticket-bubble-text">{message.message}</div>
      </div>
    </div>
  );
}
