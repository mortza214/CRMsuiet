import { useState } from "react";
import {
  ArrowRightOutlined,
  AuditOutlined,
  DownOutlined,
  FileTextOutlined,
  PercentageOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Descriptions,
  Dropdown,
  Form,
  Input,
  InputNumber,
  Modal,
  Result,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Timeline,
  Typography,
  type MenuProps,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { useIssueInvoiceFromOrder } from "@/modules/billing/hooks/useInvoices";
import {
  nextOrderStatuses,
  orderStatusMeta,
} from "@/modules/orders/constants";
import {
  useApplyOrderDiscount,
  useOrderDetails,
  useOverrideOrderItemPrice,
  useUpdateOrderStatus,
} from "@/modules/orders/hooks/useOrders";
import type { OrderItem, OrderStatus } from "@/modules/orders/types";
import { formatCurrency } from "@/shared/utils/format";

interface DiscountForm {
  discountAmount: number;
}

export function OrderDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canUpdate = hasPermission("Orders.Order.Update");
  const canDiscount = hasPermission("Orders.Sale.Manage");
  const canInvoice = hasPermission("Billing.Invoice.Create");
  const canOverridePrice = hasPermission("Orders.Order.PriceOverride");
  const [discountOpen, setDiscountOpen] = useState(false);
  const [priceItem, setPriceItem] = useState<OrderItem | null>(null);
  const [form] = Form.useForm<DiscountForm>();
  const [priceForm] = Form.useForm<{ overridePrice: number; reason: string }>();
  const overridePrice = useOverrideOrderItemPrice();
  const order = useOrderDetails(id);
  const updateStatus = useUpdateOrderStatus(id);
  const applyDiscount = useApplyOrderDiscount(id);
  const issueInvoice = useIssueInvoiceFromOrder();

  if (order.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (order.isError || !order.data) {
    return (
      <Result
        status="404"
        title="سفارش یافت نشد"
        subTitle="سفارش موردنظر وجود ندارد یا دسترسی به آن مجاز نیست."
        extra={
          <Button type="primary" onClick={() => navigate("/orders")}>
            بازگشت به سفارش‌ها
          </Button>
        }
      />
    );
  }

  const data = order.data;
  const status = orderStatusMeta[data.status];
  const nextStatuses = nextOrderStatuses[data.status] ?? [];
  const canApplyDiscount =
    data.status === "Draft" || data.status === "PendingPayment";

  const statusMenu: MenuProps = {
    items: nextStatuses.map((nextStatus) => ({
      key: nextStatus,
      label: orderStatusMeta[nextStatus]?.label ?? nextStatus,
    })),
    onClick: async ({ key }) => {
      try {
        await updateStatus.mutateAsync(key as OrderStatus);
        message.success("وضعیت سفارش تغییر کرد.");
      } catch {
        // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
      }
    },
  };

  const itemColumns: TableColumnsType<OrderItem> = [
    { title: "نام آیتم", dataIndex: "itemName", key: "itemName" },
    {
      title: "تعداد",
      dataIndex: "quantity",
      key: "quantity",
      width: 90,
      align: "center",
    },
    {
      title: "قیمت واحد",
      dataIndex: "unitPrice",
      key: "unitPrice",
      width: 150,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "جمع",
      dataIndex: "totalPrice",
      key: "totalPrice",
      width: 160,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "دوره",
      dataIndex: "billingCycle",
      key: "billingCycle",
      width: 100,
      render: (value: string) =>
        value === "Monthly" ? "ماهیانه" : value === "OneTime" ? "یک‌بار" : "سالانه",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 120,
      render: (_value, record) =>
        canOverridePrice && canApplyDiscount ? (
          <Button
            type="link"
            onClick={() => {
              setPriceItem(record);
              priceForm.setFieldsValue({
                overridePrice: record.overridePrice ?? record.unitPrice,
                reason: record.overrideReason ?? "",
              });
            }}
          >
            ویرایش قیمت
          </Button>
        ) : "-",
    },
  ];

  const submitPrice = async (values: { overridePrice: number; reason: string }) => {
    if (!priceItem || !id) return;
    try {
      await overridePrice.mutateAsync({
        orderId: id,
        itemId: priceItem.id,
        overridePrice: values.overridePrice,
        reason: values.reason,
      });
      message.success("قیمت آیتم با ثبت دلیل تغییر کرد.");
      setPriceItem(null);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const submitDiscount = async (values: DiscountForm) => {
    try {
      await applyDiscount.mutateAsync(values.discountAmount);
      message.success("تخفیف با موفقیت اعمال شد.");
      setDiscountOpen(false);
      form.resetFields();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="detail-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/orders")}
            aria-label="بازگشت"
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} className="detail-title">
                {data.orderNumber}
              </Typography.Title>
              <Tag color={status?.color} bordered={false}>
                {status?.label}
              </Tag>
            </Space>
            <Typography.Text type="secondary">
              {data.customerName || "مشتری"} — {data.orderDateJalali}
            </Typography.Text>
          </div>
        </Space>

        <Space size="middle" wrap>
          {canUpdate && nextStatuses.length > 0 && (
            <Dropdown menu={statusMenu} trigger={["click"]}>
              <Button loading={updateStatus.isPending} icon={<DownOutlined />}>
                تغییر وضعیت
              </Button>
            </Dropdown>
          )}

          {canDiscount && canApplyDiscount && (
            <Button
              icon={<PercentageOutlined />}
              onClick={() => setDiscountOpen(true)}
            >
              اعمال تخفیف
            </Button>
          )}

          {canInvoice && data.status === "PendingPayment" && (
            <Button
              type="primary"
              icon={<FileTextOutlined />}
              loading={issueInvoice.isPending}
              onClick={async () => {
                try {
                  const invoice = await issueInvoice.mutateAsync(data.id);
                  message.success("فاکتور با موفقیت صادر شد.");
                  navigate(`/invoices/${invoice.id}`);
                } catch {
                  // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
                }
              }}
            >
              صدور فاکتور
            </Button>
          )}
        </Space>
      </div>

      <Card className="detail-card" variant="borderless">
        <Descriptions
          bordered
          size="small"
          column={{ xs: 1, sm: 2, lg: 3 }}
          className="customer-descriptions"
        >
          <Descriptions.Item label="مشتری">
            {data.customerName || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="نوع سفارش">
            {data.orderType === "New"
              ? "خرید جدید"
              : data.orderType === "Renewal"
                ? "تمدید"
                : "ارتقا"}
          </Descriptions.Item>
          <Descriptions.Item label="تعداد آیتم">{data.items.length}</Descriptions.Item>
          <Descriptions.Item label="مبلغ کل">
            {formatCurrency(data.totalAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="تخفیف">
            {formatCurrency(data.discountAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="قابل پرداخت">
            {formatCurrency(data.payableAmount)}
          </Descriptions.Item>
          <Descriptions.Item
            label="توضیحات"
            span={{ xs: 1, sm: 2, lg: 3 }}
          >
            {data.notes || "-"}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card className="detail-card" variant="borderless">
        <Tabs
          defaultActiveKey="items"
          items={[
            {
              key: "items",
              label: (
                <span>
                  <AuditOutlined /> اقلام سفارش
                </span>
              ),
              children: (
                <Table<OrderItem>
                  rowKey="id"
                  columns={itemColumns}
                  dataSource={data.items}
                  pagination={false}
                  className="module-table"
                  scroll={{ x: 680 }}
                  locale={{ emptyText: "آیتمی برای این سفارش ثبت نشده است." }}
                />
              ),
            },
            {
              key: "history",
              label: "تاریخچه وضعیت",
              children:
                data.statusHistory.length > 0 ? (
                  <Timeline
                    className="order-timeline"
                    items={data.statusHistory.map((history) => ({
                      color:
                        history.toStatus === "Completed"
                          ? "green"
                          : history.toStatus === "Canceled"
                            ? "red"
                            : "blue",
                      children: (
                        <div>
                          <Space size="small" wrap>
                            {history.fromStatus && (
                              <Typography.Text type="secondary">
                                {orderStatusMeta[history.fromStatus as OrderStatus]?.label ??
                                  history.fromStatus}
                                {" ← "}
                              </Typography.Text>
                            )}
                            <Typography.Text strong>
                              {orderStatusMeta[history.toStatus]?.label ??
                                history.toStatus}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                              {history.changedAtJalali}
                            </Typography.Text>
                          </Space>
                          {history.note && (
                            <Typography.Paragraph
                              type="secondary"
                              className="timeline-note"
                            >
                              {history.note}
                            </Typography.Paragraph>
                          )}
                        </div>
                      ),
                    }))}
                  />
                ) : (
                  <Typography.Text type="secondary">
                    هنوز تغییری در وضعیت این سفارش ثبت نشده است.
                  </Typography.Text>
                ),
            },
          ]}
        />
      </Card>

      <Modal
        title="ویرایش قیمت آیتم"
        open={priceItem !== null}
        onCancel={() => setPriceItem(null)}
        onOk={() => priceForm.submit()}
        okText="ثبت قیمت"
        cancelText="انصراف"
        confirmLoading={overridePrice.isPending}
        forceRender
      >
        <Form form={priceForm} layout="vertical" onFinish={submitPrice}>
          <Form.Item
            name="overridePrice"
            label="قیمت جایگزین هر واحد"
            rules={[{ required: true, message: "قیمت را وارد کنید." }]}
          >
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item
            name="reason"
            label="دلیل تغییر قیمت"
            rules={[{ required: true, message: "دلیل الزامی است." }]}
          >
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="اعمال تخفیف"
        open={discountOpen}
        onCancel={() => setDiscountOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={applyDiscount.isPending}
        okText="اعمال تخفیف"
        cancelText="انصراف"
      >
        <Form form={form} layout="vertical" onFinish={submitDiscount}>
          <Form.Item
            name="discountAmount"
            label="مبلغ تخفیف (تومان)"
            rules={[
              { required: true, message: "مبلغ تخفیف را وارد کنید." },
              {
                type: "number",
                min: 0,
                max: data.totalAmount,
                message: "مبلغ تخفیف معتبر نیست.",
              },
            ]}
          >
            <InputNumber
              min={0}
              max={data.totalAmount}
              style={{ width: "100%" }}
              formatter={(value) =>
                `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")
              }
            />
          </Form.Item>
          <Typography.Text type="secondary">
            حداکثر تخفیف قابل اعمال: {formatCurrency(data.totalAmount)}
          </Typography.Text>
        </Form>
      </Modal>
    </Space>
  );
}
