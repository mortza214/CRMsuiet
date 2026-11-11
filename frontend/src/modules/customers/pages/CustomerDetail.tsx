import { useState } from "react";
import {
  ArrowRightOutlined,
  EditOutlined,
  EyeOutlined,
  FileTextOutlined,
  MessageOutlined,
  PlusOutlined,
  ShoppingOutlined,
  TeamOutlined,
  UserOutlined,
} from "@ant-design/icons";
import {
  Button,
  Card,
  Descriptions,
  Empty,
  Result,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { CustomerModal } from "@/modules/customers/pages/CustomerModal";
import {
  useCustomerDetails,
  useCustomerOwners,
  useCustomers,
} from "@/modules/customers/hooks/useCustomers";
import type { CustomerListItem } from "@/modules/customers/types";
import { useInvoices } from "@/modules/billing/hooks/useInvoices";
import {
  invoiceStatusMeta,
  invoiceTypeMeta,
} from "@/modules/billing/constants";
import type {
  InvoiceListItem,
  InvoiceStatus,
} from "@/modules/billing/types";
import {
  useCustomerServiceRequests,
  useCustomerServices,
} from "@/modules/orders/hooks/useOrders";
import type {
  CustomerService,
  ServiceRequestSummary,
} from "@/modules/orders/types";

export function CustomerDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canUpdate = hasPermission("Customers.Customer.Update");
  const canCreateService = hasPermission("Orders.Order.Create");
  const [modalOpen, setModalOpen] = useState(false);
  const customer = useCustomerDetails(id);
  const owners = useCustomerOwners();
  const subCustomers = useCustomers(
    { page: 1, pageSize: 100, parentCustomerId: id },
    Boolean(customer.data?.isReseller && id),
  );
  const customerServices = useCustomerServices(id);
  const serviceRequests = useCustomerServiceRequests(id);
  const invoices = useInvoices(
    { page: 1, pageSize: 50, customerId: id },
    Boolean(id),
  );

  if (customer.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (customer.isError || !customer.data) {
    return (
      <Result
        status="404"
        title="مشتری یافت نشد"
        subTitle="مشتری موردنظر وجود ندارد یا دسترسی به آن مجاز نیست."
        extra={
          <Button type="primary" onClick={() => navigate("/customers")}>
            بازگشت به لیست مشتریان
          </Button>
        }
      />
    );
  }

  const data = customer.data;
  const ownerName =
    owners.data?.find((user) => user.id === data.ownerUserId)?.fullName ??
    "تعیین‌نشده";
  const contactName = [data.firstName, data.lastName].filter(Boolean).join(" ");
  const title =
    data.type === "Legal"
      ? data.companyName || data.displayName
      : data.displayName || contactName;

  const subColumns: TableColumnsType<CustomerListItem> = [
    {
      title: "نام مشتری",
      key: "name",
      render: (_value, record) => record.displayName,
    },
    {
      title: "موبایل",
      dataIndex: "mobile",
      key: "mobile",
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "نوع",
      dataIndex: "type",
      key: "type",
      render: (value: CustomerListItem["type"]) => (
        <Tag color={value === "Legal" ? "green" : "blue"} bordered={false}>
          {value === "Legal" ? "حقوقی" : "حقیقی"}
        </Tag>
      ),
    },
    {
      title: "عملیات",
      key: "actions",
      width: 100,
      render: (_value, record) => (
        <Button
          type="text"
          icon={<EyeOutlined />}
          onClick={() => navigate(`/customers/${record.id}`)}
        >
          مشاهده
        </Button>
      ),
    },
  ];

  const requestColumns: TableColumnsType<ServiceRequestSummary> = [
    {
      title: "شماره سفارش",
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
      title: "مرحله گردش کار",
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
        <Tag bordered={false}>{value === "Pending" ? "در انتظار بررسی" : value}</Tag>
      ),
    },
    {
      title: "مبلغ",
      dataIndex: "totalAmount",
      key: "totalAmount",
      render: (value: number) => value.toLocaleString("fa-IR"),
    },
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 120,
      render: (_value, record) => (
        <Space size={2}>
          <Button type="link" onClick={() => navigate(`/orders/${record.orderId}`)}>
            سفارش
          </Button>
          {record.workflowInstanceId && (
            <Button
              type="link"
              onClick={() => navigate(`/workflow/instances/${record.workflowInstanceId}`)}
            >
              گردش کار
            </Button>
          )}
        </Space>
      ),
    },
  ];

  const invoiceColumns: TableColumnsType<InvoiceListItem> = [
    {
      title: "شماره فاکتور",
      dataIndex: "invoiceNumber",
      key: "invoiceNumber",
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "نوع",
      dataIndex: "type",
      key: "type",
      render: (value: InvoiceListItem["type"]) => {
        const meta = invoiceTypeMeta[value];
        return <Tag color={meta?.color}>{meta?.label ?? value}</Tag>;
      },
    },
    {
      title: "مبلغ",
      dataIndex: "totalAmount",
      key: "totalAmount",
      render: (value: number) => value.toLocaleString("fa-IR"),
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      render: (value: InvoiceStatus) => {
        const meta = invoiceStatusMeta[value];
        return <Tag color={meta?.color}>{meta?.label ?? value}</Tag>;
      },
    },
    {
      title: "تاریخ صدور",
      dataIndex: "issueDateJalali",
      key: "issueDateJalali",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 100,
      render: (_value, record) => (
        <Button type="link" onClick={() => navigate(`/invoices/${record.id}`)}>
          مشاهده
        </Button>
      ),
    },
  ];

  const serviceColumns: TableColumnsType<CustomerService> = [
    { title: "سرویس", dataIndex: "serviceName", key: "serviceName" },
    {
      title: "مبلغ ماهانه (ریال)",
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
          {value === "Active" ? "فعال" : value}
        </Tag>
      ),
    },
    {
      title: "تاریخ فعال‌سازی",
      dataIndex: "activatedAtJalali",
      key: "activatedAtJalali",
    },
    {
      title: "فاکتور بعدی",
      dataIndex: "nextInvoiceAtJalali",
      key: "nextInvoiceAtJalali",
      render: (value: string | null | undefined) => value || "-",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 100,
      render: (_value, record) => (
        <Button type="link" onClick={() => navigate(`/orders/${record.orderId}`)}>
          سفارش
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="detail-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/customers")}
            aria-label="بازگشت"
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} className="detail-title">
                {title}
              </Typography.Title>
              <Tag color={data.type === "Legal" ? "green" : "blue"} bordered={false}>
                {data.type === "Legal" ? "حقوقی" : "حقیقی"}
              </Tag>
              {data.isReseller && <Tag color="purple">ریسلر</Tag>}
            </Space>
            <Typography.Text type="secondary">
              {data.type === "Legal" && contactName
                ? `شخص رابط: ${contactName}`
                : data.mobile}
            </Typography.Text>
          </div>
        </Space>

        <Space>
          {canCreateService && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate(`/customers/${data.id}/new-service`)}
            >
              ثبت سرویس جدید
            </Button>
          )}
          {canUpdate && (
            <Button
              icon={<EditOutlined />}
              onClick={() => setModalOpen(true)}
            >
              ویرایش مشتری
            </Button>
          )}
        </Space>
      </div>

      <Card className="detail-card" variant="borderless">
        <Tabs
          defaultActiveKey="profile"
          items={[
            {
              key: "profile",
              label: (
                <span>
                  <UserOutlined /> اطلاعات پروفایل
                </span>
              ),
              children: (
                <Descriptions
                  bordered
                  size="small"
                  column={{ xs: 1, sm: 2 }}
                  className="customer-descriptions"
                >
                  <Descriptions.Item label="نام نمایشی">
                    {data.displayName}
                  </Descriptions.Item>
                  <Descriptions.Item label="نوع مشتری">
                    {data.type === "Legal" ? "حقوقی" : "حقیقی"}
                  </Descriptions.Item>
                  {data.type === "Legal" && (
                    <>
                      <Descriptions.Item label="نام شرکت">
                        {data.companyName || "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="شخص رابط">
                        {contactName || "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="کد اقتصادی">
                        {data.economicCode || "-"}
                      </Descriptions.Item>
                    </>
                  )}
                  <Descriptions.Item label="کد ملی / شناسه ملی">
                    <span dir="ltr">{data.nationalId}</span>
                  </Descriptions.Item>
                  <Descriptions.Item label="موبایل">
                    <span dir="ltr">{data.mobile}</span>
                  </Descriptions.Item>
                  <Descriptions.Item label="تلفن ثابت">
                    {data.phone || "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label="ایمیل">
                    {data.email || "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label="آدرس" span={2}>
                    {data.address || "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label="کارشناس فروش">
                    {ownerName}
                  </Descriptions.Item>
                  <Descriptions.Item label="گروه مشتری">
                    {data.group?.name || "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label="ریسلر">
                    {data.isReseller ? "بله" : "خیر"}
                  </Descriptions.Item>
                  <Descriptions.Item label="تاریخ ثبت">
                    {data.createdAtJalali}
                  </Descriptions.Item>
                  <Descriptions.Item label="برچسب‌ها" span={2}>
                    {data.tags.length > 0 ? (
                      <Space wrap>
                        {data.tags.map((tag) => (
                          <Tag key={tag.id} color={tag.color} bordered={false}>
                            {tag.name}
                          </Tag>
                        ))}
                      </Space>
                    ) : (
                      "-"
                    )}
                  </Descriptions.Item>
                </Descriptions>
              ),
            },
            {
              key: "services",
              label: (
                <span>
                  <ShoppingOutlined /> سرویس‌ها و سفارش‌ها
                </span>
              ),
              children: (
                <Space direction="vertical" size={16} style={{ width: "100%" }}>
                  <Card
                    title="درخواست‌های سرویس"
                    variant="borderless"
                    className="settings-section-card"
                  >
                    <Table<ServiceRequestSummary>
                      rowKey="orderId"
                      size="small"
                      columns={requestColumns}
                      dataSource={serviceRequests.data ?? []}
                      loading={serviceRequests.isLoading}
                      pagination={false}
                      scroll={{ x: 900 }}
                      className="module-table"
                      locale={{ emptyText: "درخواستی ثبت نشده است." }}
                    />
                  </Card>
                  <Card
                    title="سرویس‌های فعال"
                    variant="borderless"
                    className="settings-section-card"
                  >
                    <Table<CustomerService>
                      rowKey="id"
                      size="small"
                      columns={serviceColumns}
                      dataSource={customerServices.data ?? []}
                      loading={customerServices.isLoading}
                      pagination={false}
                      scroll={{ x: 760 }}
                      className="module-table"
                      locale={{ emptyText: "سرویس فعالی ثبت نشده است." }}
                    />
                  </Card>
                </Space>
              ),
            },
            {
              key: "finance",
              label: (
                <span>
                  <FileTextOutlined /> فاکتورها و مالی
                </span>
              ),
              children: (
                <Space direction="vertical" size={16} style={{ width: "100%" }}>
                  <Card
                    title="فاکتورها"
                    variant="borderless"
                    className="settings-section-card"
                  >
                    <Table<InvoiceListItem>
                      rowKey="id"
                      size="small"
                      columns={invoiceColumns}
                      dataSource={invoices.data?.items ?? []}
                      loading={invoices.isLoading}
                      pagination={false}
                      scroll={{ x: 760 }}
                      className="module-table"
                      locale={{ emptyText: "فاکتوری ثبت نشده است." }}
                    />
                  </Card>
                  <Card
                    title="سرویس‌های فعال"
                    variant="borderless"
                    className="settings-section-card"
                  >
                    <Table<CustomerService>
                      rowKey="id"
                      size="small"
                      columns={serviceColumns}
                      dataSource={customerServices.data ?? []}
                      loading={customerServices.isLoading}
                      pagination={false}
                      scroll={{ x: 760 }}
                      className="module-table"
                      locale={{ emptyText: "سرویس فعالی ثبت نشده است." }}
                    />
                  </Card>
                </Space>
              ),
            },
            {
              key: "tickets",
              label: (
                <span>
                  <MessageOutlined /> تیکت‌ها
                </span>
              ),
              children: (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="تیکت‌های این مشتری در حال توسعه است."
                />
              ),
            },
            ...(data.isReseller
              ? [
                  {
                    key: "subcustomers",
                    label: (
                      <span>
                        <TeamOutlined /> زیرمجموعه‌ها
                      </span>
                    ),
                    children: (
                      <Table<CustomerListItem>
                        rowKey="id"
                        size="small"
                        columns={subColumns}
                        dataSource={subCustomers.data?.items ?? []}
                        loading={subCustomers.isLoading}
                        pagination={false}
                        scroll={{ x: 520 }}
                        locale={{ emptyText: "زیرمجموعه‌ای ثبت نشده است." }}
                      />
                    ),
                  },
                ]
              : []),
          ]}
        />
      </Card>

      <CustomerModal
        open={modalOpen}
        customer={data}
        onClose={() => setModalOpen(false)}
        onSaved={() => {
          setModalOpen(false);
          void customer.refetch();
        }}
      />
    </Space>
  );
}
