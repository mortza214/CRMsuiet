import {
  CustomerServiceOutlined,
  DollarOutlined,
  FileTextOutlined,
  MessageOutlined,
  RiseOutlined,
} from "@ant-design/icons";
import {
  Card,
  Col,
  Row,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { useRecentServiceRequests } from "@/modules/orders/hooks/useOrders";
import type { ServiceRequestSummary } from "@/modules/orders/types";

const stats = [
  {
    title: "کل مشتریان",
    value: 0,
    icon: <CustomerServiceOutlined />,
    color: "#1677ff",
    background: "#e6f4ff",
  },
  {
    title: "کل فروش",
    value: 0,
    icon: <DollarOutlined />,
    color: "#16a34a",
    background: "#f0fdf4",
  },
  {
    title: "فاکتورهای پرداخت‌نشده",
    value: 0,
    icon: <FileTextOutlined />,
    color: "#d97706",
    background: "#fffbeb",
  },
  {
    title: "تیکت‌های باز",
    value: 0,
    icon: <MessageOutlined />,
    color: "#7c3aed",
    background: "#f5f3ff",
  },
];

export function Dashboard() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const seesAllRequests =
    hasPermission("Orders.Sale.Manage") ||
    user?.permissions.includes("*") === true;
  const requests = useRecentServiceRequests(
    seesAllRequests ? undefined : user?.id,
  );

  const requestColumns: TableColumnsType<ServiceRequestSummary> = [
    {
      title: "شماره سفارش",
      dataIndex: "orderNumber",
      key: "orderNumber",
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "مشتری",
      dataIndex: "customerName",
      key: "customerName",
      render: (value: string | null | undefined) => value || "-",
    },
    {
      title: "سرویس",
      dataIndex: "serviceName",
      key: "serviceName",
      render: (value: string | null | undefined) => value || "-",
    },
    {
      title: "مرحله فعلی",
      dataIndex: "workflowStateName",
      key: "workflowStateName",
      render: (value: string | null | undefined) => (
        <Tag color="blue" bordered={false}>
          {value || "بدون گردش کار"}
        </Tag>
      ),
    },
    {
      title: "وضعیت سفارش",
      dataIndex: "orderStatus",
      key: "orderStatus",
      render: (value: string) => (
        <Tag bordered={false}>
          {value === "Pending" ? "در انتظار بررسی" : value}
        </Tag>
      ),
    },
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 180,
      render: (_value, record) => (
        <Space size={2}>
          <Typography.Link onClick={() => navigate(`/orders/${record.orderId}`)}>
            سفارش
          </Typography.Link>
          {record.workflowInstanceId && (
            <Typography.Link
              onClick={() =>
                navigate(`/workflow/instances/${record.workflowInstanceId}`)
              }
            >
              گردش کار
            </Typography.Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={24} className="dashboard-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>داشبورد مدیریت</Typography.Title>
          <Typography.Text type="secondary">
            به سامانه مدیریت CrmSuite خوش آمدید
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <RiseOutlined /> نمای کلی کسب‌وکار
        </span>
      </div>

      <Row gutter={[16, 16]}>
        {stats.map((item) => (
          <Col xs={24} sm={12} xl={6} key={item.title}>
            <Card className="stat-card" variant="borderless">
              <div className="stat-card-top">
                <span
                  className="stat-icon"
                  style={{ color: item.color, background: item.background }}
                >
                  {item.icon}
                </span>
                <Statistic title={item.title} value={item.value} />
              </div>
              <Typography.Text type="secondary" className="stat-hint">
                داده‌های این کارت پس از فعال‌سازی گزارش‌ها تکمیل می‌شود.
              </Typography.Text>
            </Card>
          </Col>
        ))}
      </Row>

      <Card
        title="درخواست‌های سرویس اخیر"
        variant="borderless"
        className="settings-section-card"
        extra={
          <Tag color="blue" bordered={false}>
            {requests.data?.length ?? 0} درخواست
          </Tag>
        }
      >
        <Table<ServiceRequestSummary>
          rowKey="orderId"
          size="small"
          columns={requestColumns}
          dataSource={requests.data ?? []}
          loading={requests.isLoading}
          pagination={false}
          scroll={{ x: 900 }}
          className="module-table"
          locale={{ emptyText: "درخواست سرویسی ثبت نشده است." }}
        />
      </Card>

      <Card className="welcome-card" variant="borderless">
        <Typography.Title level={4}>شروع کار با CrmSuite</Typography.Title>
        <Typography.Paragraph type="secondary" className="welcome-text">
          از منوی کنار صفحه، ماژول موردنظر را انتخاب کنید. بخش مشتریان، فروش،
          فاکتورها، پرداخت‌ها، کیف پول، پورسانت، تیکت‌ها، گزارش‌ها و تنظیمات
          به‌تدریج در همین فضا فعال می‌شوند.
        </Typography.Paragraph>
      </Card>
    </Space>
  );
}
