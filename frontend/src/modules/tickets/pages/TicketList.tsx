import { useState } from "react";
import {
  EyeOutlined,
  MessageOutlined,
  PlusOutlined,
  SearchOutlined,
} from "@ant-design/icons";
import {
  Button,
  Input,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import {
  ticketPriorityMeta,
  ticketPriorityOptions,
  ticketStatusMeta,
  ticketStatusOptions,
} from "@/modules/tickets/constants";
import { useTickets } from "@/modules/tickets/hooks/useTickets";
import { CreateTicketModal } from "@/modules/tickets/pages/CreateTicketModal";
import type {
  TicketListItem,
  TicketPriority,
  TicketStatus,
} from "@/modules/tickets/types";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

export function TicketList() {
  const navigate = useNavigate();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canCreate = hasPermission("Tickets.Ticket.Create");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<TicketStatus | undefined>();
  const [priority, setPriority] = useState<TicketPriority | undefined>();
  const [createOpen, setCreateOpen] = useState(false);
  const debouncedSearch = useDebouncedValue(search, 400);
  const tickets = useTickets({
    page,
    pageSize,
    search: debouncedSearch,
    status,
    priority,
  });

  const columns: TableColumnsType<TicketListItem> = [
    {
      title: "شماره تیکت",
      dataIndex: "ticketNumber",
      key: "ticketNumber",
      width: 155,
      render: (value: string) => (
        <Typography.Text strong copyable={{ text: value }} dir="ltr">
          {value}
        </Typography.Text>
      ),
    },
    {
      title: "موضوع",
      dataIndex: "subject",
      key: "subject",
      width: 240,
      render: (value: string, record) => (
        <div className="ticket-subject-cell">
          <Typography.Text strong>{value}</Typography.Text>
          <small>
            <MessageOutlined /> {record.messageCount} پیام
          </small>
        </div>
      ),
    },
    {
      title: "مشتری",
      dataIndex: "customerName",
      key: "customerName",
      width: 170,
      render: (value: string | null) => value || "-",
    },
    {
      title: "دپارتمان",
      dataIndex: "departmentName",
      key: "departmentName",
      width: 150,
      render: (value: string | null) => value || "-",
    },
    {
      title: "اولویت",
      dataIndex: "priority",
      key: "priority",
      width: 110,
      render: (value: TicketPriority) => {
        const meta = ticketPriorityMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 150,
      render: (value: TicketStatus) => {
        const meta = ticketStatusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "تاریخ ایجاد",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 125,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 95,
      fixed: "left",
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => navigate(`/tickets/${record.id}`)}
        >
          مشاهده
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>تیکت‌های پشتیبانی</Typography.Title>
          <Typography.Text type="secondary">
            مدیریت درخواست‌ها، پاسخ‌گویی و ارجاع تیکت‌های مشتریان
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <MessageOutlined /> {tickets.data?.totalCount ?? 0} تیکت
        </span>
      </div>

      <div className="table-toolbar">
        <Input
          allowClear
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
          prefix={<SearchOutlined />}
          placeholder="جستجو بر اساس شماره تیکت، موضوع یا نام مشتری..."
          className="toolbar-search"
        />

        <Space size="middle" wrap>
          <Select
            allowClear
            value={status}
            onChange={(value) => {
              setStatus(value as TicketStatus | undefined);
              setPage(1);
            }}
            options={ticketStatusOptions}
            placeholder="فیلتر وضعیت"
            style={{ minWidth: 165 }}
          />
          <Select
            allowClear
            value={priority}
            onChange={(value) => {
              setPriority(value as TicketPriority | undefined);
              setPage(1);
            }}
            options={ticketPriorityOptions}
            placeholder="فیلتر اولویت"
            style={{ minWidth: 145 }}
          />
          {canCreate && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setCreateOpen(true)}
            >
              ثبت تیکت جدید
            </Button>
          )}
        </Space>
      </div>

      <Table<TicketListItem>
        rowKey="id"
        columns={columns}
        dataSource={tickets.data?.items ?? []}
        loading={tickets.isLoading || tickets.isFetching}
        scroll={{ x: 1200 }}
        className="module-table"
        locale={{ emptyText: "تیکتی یافت نشد." }}
        pagination={{
          current: tickets.data?.page ?? page,
          pageSize: tickets.data?.pageSize ?? pageSize,
          total: tickets.data?.totalCount ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50],
          showTotal: (total) => `مجموع ${total} تیکت`,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPageSize !== pageSize ? 1 : nextPage);
            setPageSize(nextPageSize);
          },
        }}
      />

      <CreateTicketModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(ticket) => {
          setCreateOpen(false);
          navigate(`/tickets/${ticket.id}`);
        }}
      />
    </Space>
  );
}
