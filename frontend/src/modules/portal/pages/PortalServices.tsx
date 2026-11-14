import { Button, Card, Empty, Space, Table, Tag, Typography, type TableColumnsType } from "antd";
import { Link } from "react-router-dom";
import {
  usePortalActiveServices,
  usePortalServiceRequests,
} from "@/modules/portal/hooks/usePortal";
import type {
  CustomerService,
  ServiceRequestSummary,
} from "@/modules/orders/types";

const requestColumns: TableColumnsType<ServiceRequestSummary> = [
  {
    title: "شماره درخواست",
    dataIndex: "orderNumber",
    key: "orderNumber",
    render: (value: string) => <span dir="ltr">{value}</span>,
  },
  {
    title: "سرویس",
    dataIndex: "serviceName",
    key: "serviceName",
    render: (value: string | null | undefined) => value || "-",
  },
  {
    title: "مرحله",
    dataIndex: "workflowStateName",
    key: "workflowStateName",
    render: (value: string | null | undefined) => (
      <Tag color="blue" bordered={false}>
        {value || "در حال بررسی"}
      </Tag>
    ),
  },
  {
    title: "وضعیت",
    dataIndex: "orderStatus",
    key: "orderStatus",
    render: (value: string) => (
      <Tag bordered={false}>{value === "Pending" ? "در انتظار بررسی" : value}</Tag>
    ),
  },
  { title: "تاریخ", dataIndex: "createdAtJalali", key: "createdAtJalali" },
];

const serviceColumns: TableColumnsType<CustomerService> = [
  { title: "سرویس", dataIndex: "serviceName", key: "serviceName" },
  {
    title: "مبلغ ماهانه",
    dataIndex: "monthlyAmount",
    key: "monthlyAmount",
    render: (value: number) => value.toLocaleString("fa-IR"),
  },
  {
    title: "وضعیت",
    dataIndex: "status",
    key: "status",
    render: (value: string) => (
      <Tag color={value === "Active" ? "green" : "default"} bordered={false}>
        {value === "Active" ? "فعال" : value === "Collected" ? "جمع‌آوری‌شده" : value}
      </Tag>
    ),
  },
  { title: "تاریخ فعال‌سازی", dataIndex: "activatedAtJalali", key: "activatedAtJalali" },
  {
    title: "فاکتور بعدی",
    dataIndex: "nextInvoiceAtJalali",
    key: "nextInvoiceAtJalali",
    render: (value: string | null | undefined) => value || "-",
  },
];

export function PortalServices() {
  const requests = usePortalServiceRequests();
  const services = usePortalActiveServices();

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>سرویس‌های من</Typography.Title>
          <Link to="/portal/services/new"><Button type="primary">درخواست سرویس جدید</Button></Link>
          <Typography.Text type="secondary">
            وضعیت درخواست‌ها و سرویس‌های فعال شما
          </Typography.Text>
        </div>
      </div>

      <Card title="درخواست‌های در جریان" variant="borderless" className="detail-card">
        <Table<ServiceRequestSummary>
          rowKey="orderId"
          columns={requestColumns}
          dataSource={requests.data ?? []}
          loading={requests.isLoading}
          pagination={false}
          scroll={{ x: 700 }}
          locale={{
            emptyText: <Empty description="درخواست در جریانی ندارید." />,
          }}
        />
      </Card>

      <Card title="سرویس‌های فعال" variant="borderless" className="detail-card">
        <Table<CustomerService>
          rowKey="id"
          columns={serviceColumns}
          dataSource={services.data ?? []}
          loading={services.isLoading}
          pagination={false}
          scroll={{ x: 700 }}
          locale={{
            emptyText: <Empty description="سرویس فعالی ندارید." />,
          }}
        />
      </Card>
    </Space>
  );
}
