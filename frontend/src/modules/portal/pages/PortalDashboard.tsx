import {
  ArrowLeftOutlined,
  MessageOutlined,
  ShoppingOutlined,
  TeamOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import {
  Button,
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
import {
  usePortalActiveServices,
  usePortalOrders,
  usePortalProfile,
  usePortalServiceRequests,
  usePortalSubCustomers,
  usePortalTickets,
  usePortalWallet,
} from "@/modules/portal/hooks/usePortal";
import type { CustomerDetail } from "@/modules/customers/types";
import { formatCurrency, formatNumber } from "@/shared/utils/format";

export function PortalDashboard() {
  const navigate = useNavigate();
  const profile = usePortalProfile();
  const wallet = usePortalWallet();
  const orders = usePortalOrders();
  const serviceRequests = usePortalServiceRequests();
  const activeServices = usePortalActiveServices();
  const tickets = usePortalTickets();
  const subCustomers = usePortalSubCustomers(Boolean(profile.data?.isReseller));
  const customer = profile.data;

  const activeOrders =
    orders.data?.filter(
      (order) => order.status !== "Completed" && order.status !== "Canceled",
    ).length ?? 0;
  const openTickets =
    tickets.data?.filter(
      (ticket) => ticket.status !== "Closed" && ticket.status !== "Resolved",
    ).length ?? 0;

  const subColumns: TableColumnsType<CustomerDetail> = [
    {
      title: "نام",
      dataIndex: "displayName",
      key: "displayName",
    },
    {
      title: "موبایل",
      dataIndex: "mobile",
      key: "mobile",
      width: 145,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "وضعیت",
      dataIndex: "isReseller",
      key: "isReseller",
      width: 110,
      render: (value: boolean) => (
        <Tag color={value ? "purple" : "blue"} bordered={false}>
          {value ? "ریسلر" : "عادی"}
        </Tag>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <Card className="portal-welcome-card" variant="borderless">
        <div className="portal-welcome-content">
          <div>
            <Typography.Title level={2}>
              خوش آمدید، {customer?.displayName ?? "مشتری عزیز"}
            </Typography.Title>
            <Typography.Text type="secondary">
              وضعیت سفارش‌ها، فاکتورها و پشتیبانی خود را از اینجا مدیریت کنید.
            </Typography.Text>
          </div>
          <div className="portal-welcome-icon">
            <WalletOutlined />
          </div>
        </div>
      </Card>

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={8}>
          <Card variant="borderless" className="stat-card stat-green portal-stat">
            <Statistic
              title="موجودی کیف پول"
              value={formatNumber(wallet.data?.balance?.balance ?? 0)}
              suffix="تومان"
              prefix={<WalletOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card variant="borderless" className="stat-card stat-blue portal-stat">
            <Statistic
              title="سفارش‌های فعال"
              value={formatNumber(activeOrders)}
              suffix="سفارش"
              prefix={<ShoppingOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card variant="borderless" className="stat-card stat-orange portal-stat">
            <Statistic
              title="تیکت‌های باز"
              value={formatNumber(openTickets)}
              suffix="تیکت"
              prefix={<MessageOutlined />}
            />
          </Card>
        </Col>
      </Row>

      <Card
        title="وضعیت سرویس‌های من"
        variant="borderless"
        className="settings-section-card"
        extra={
          <Button type="link" onClick={() => navigate("/portal/services")}>
            مشاهده همه
          </Button>
        }
      >
        {serviceRequests.data?.length || activeServices.data?.length ? (
          <Space direction="vertical" size="small" className="module-stack">
            {(serviceRequests.data ?? []).slice(0, 5).map((request) => (
              <div className="portal-mini-row" key={request.orderId}>
                <Typography.Text strong>{request.serviceName || "سرویس"}</Typography.Text>
                <Space size="small">
                  <Tag color="blue" bordered={false}>
                    {request.workflowStateName || "در حال بررسی"}
                  </Tag>
                  <Typography.Text type="secondary" dir="ltr">
                    {request.orderNumber}
                  </Typography.Text>
                </Space>
              </div>
            ))}
            {(activeServices.data ?? []).slice(0, 3).map((service) => (
              <div className="portal-mini-row" key={service.id}>
                <Typography.Text strong>{service.serviceName}</Typography.Text>
                <Tag color="green" bordered={false}>
                  فعال
                </Tag>
              </div>
            ))}
          </Space>
        ) : (
          <Typography.Text type="secondary">سرویس یا درخواستی ثبت نشده است.</Typography.Text>
        )}
      </Card>

      <Row gutter={[18, 18]}>
        <Col xs={24} lg={12}>
          <Card
            title="دسترسی سریع" variant="borderless"
            className="settings-section-card portal-action-card"
          >
            <Space direction="vertical" size="middle" className="module-stack">
              <Button
                block
                icon={<ShoppingOutlined />}
                onClick={() => navigate("/portal/orders")}
              >
                مشاهده سفارش‌های من
              </Button>
              <Button
                block
                icon={<WalletOutlined />}
                onClick={() => navigate("/portal/invoices")}
              >
                مشاهده و پرداخت فاکتورها
              </Button>
              <Button
                block
                icon={<MessageOutlined />}
                onClick={() => navigate("/portal/tickets")}
              >
                ثبت و پیگیری تیکت
              </Button>
            </Space>
          </Card>
        </Col>

        <Col xs={24} lg={12}>
          <Card
            title="آخرین سفارش‌ها" variant="borderless"
            className="settings-section-card"
            extra={
              <Button
                type="link"
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate("/portal/orders")}
              >
                همه
              </Button>
            }
          >
            {orders.data?.length ? (
              <Space direction="vertical" size="small" className="module-stack">
                {orders.data.slice(0, 4).map((order) => (
                  <div className="portal-mini-row" key={order.id}>
                    <Typography.Text strong dir="ltr">
                      {order.orderNumber}
                    </Typography.Text>
                    <Space size="small">
                      <Typography.Text type="secondary">
                        {order.orderDateJalali}
                      </Typography.Text>
                      <Typography.Text>{formatCurrency(order.totalAmount)}</Typography.Text>
                    </Space>
                  </div>
                ))}
              </Space>
            ) : (
              <Typography.Text type="secondary">
                سفارشی برای نمایش وجود ندارد.
              </Typography.Text>
            )}
          </Card>
        </Col>
      </Row>

      {customer?.isReseller && (
        <Card
          title={
            <Space>
              <TeamOutlined />
              زیرمجموعه‌های من
            </Space>
          }
          bordered={false}
          className="settings-section-card"
          extra={
            <Tag color="purple" bordered={false}>
              {formatNumber(subCustomers.data?.length ?? 0)} زیرمجموعه
            </Tag>
          }
        >
          <Table<CustomerDetail>
            rowKey="id"
            columns={subColumns}
            dataSource={subCustomers.data ?? []}
            loading={subCustomers.isLoading}
            pagination={{ pageSize: 5, showSizeChanger: false }}
            scroll={{ x: 480 }}
            size="small"
          />
        </Card>
      )}
    </Space>
  );
}
