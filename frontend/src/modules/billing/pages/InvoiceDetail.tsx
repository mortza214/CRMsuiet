import { useState } from "react";
import {
  ArrowRightOutlined,
  CreditCardOutlined,
  DownOutlined,
  FilePdfOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Card,
  Descriptions,
  Dropdown,
  Modal,
  Result,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
  type MenuProps,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { invoiceStatusMeta } from "@/modules/billing/constants";
import { useInvoiceDetails, useUpdateInvoiceStatus } from "@/modules/billing/hooks/useInvoices";
import { openInvoicePdf } from "@/modules/billing/api/invoicePdf";
import type {
  InvoiceItem,
  InvoiceStatus,
} from "@/modules/billing/types";
import {
  useCustomerWalletBalance,
  useInitiateSamanPayment,
  usePayInvoiceFromWallet,
} from "@/modules/payments/hooks/usePayments";
import { formatCurrency } from "@/shared/utils/format";

const transitions: Record<InvoiceStatus, InvoiceStatus[]> = {
  Draft: ["Canceled"],
  Issued: ["PartiallyPaid", "Paid", "Canceled"],
  PartiallyPaid: ["Paid", "Canceled"],
  Paid: [],
  Canceled: [],
};

export function InvoiceDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canChangeStatus = hasPermission("Billing.Invoice.Issue");
  const canPayWallet = hasPermission("Wallet.CustomerWallet.Read");
  const canPayOnline = hasPermission("Payments.Payment.Create");
  const [walletOpen, setWalletOpen] = useState(false);
  const [onlineOpen, setOnlineOpen] = useState(false);
  const [paymentUrl, setPaymentUrl] = useState<string | null>(null);
  const [pdfLoading, setPdfLoading] = useState(false);
  const invoice = useInvoiceDetails(id);
  const updateStatus = useUpdateInvoiceStatus(id);
  const payFromWallet = usePayInvoiceFromWallet();
  const initiateOnline = useInitiateSamanPayment();
  const wallet = useCustomerWalletBalance(invoice.data?.customerId, walletOpen);

  if (invoice.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (invoice.isError || !invoice.data) {
    return (
      <Result
        status="404"
        title="فاکتور یافت نشد"
        subTitle="فاکتور موردنظر وجود ندارد یا دسترسی به آن مجاز نیست."
        extra={
          <Button type="primary" onClick={() => navigate("/invoices")}>
            بازگشت به فاکتورها
          </Button>
        }
      />
    );
  }

  const data = invoice.data;
  const status = invoiceStatusMeta[data.status];
  const remaining = Math.max(0, data.totalAmount - data.paidAmount);
  const options = transitions[data.status] ?? [];

  const statusMenu: MenuProps = {
    items: options.map((next) => ({
      key: next,
      label: invoiceStatusMeta[next]?.label ?? next,
    })),
    onClick: async ({ key }) => {
      try {
        await updateStatus.mutateAsync(key as InvoiceStatus);
        message.success("وضعیت فاکتور تغییر کرد.");
      } catch {
        // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
      }
    },
  };

  const handlePdf = async () => {
    setPdfLoading(true);
    try {
      await openInvoicePdf(data.id);
    } catch {
      message.error("دریافت فایل PDF فاکتور ناموفق بود.");
    } finally {
      setPdfLoading(false);
    }
  };

  const handleWalletPay = async () => {
    try {
      await payFromWallet.mutateAsync({
        invoiceId: data.id,
        customerId: data.customerId,
      });
      message.success("فاکتور از کیف پول پرداخت شد.");
      setWalletOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handleOnlinePay = async () => {
    const popup = window.open("about:blank", "_blank");
    try {
      const result = await initiateOnline.mutateAsync(data.id);
      setPaymentUrl(result.paymentUrl);
      if (popup) {
        popup.location.href = result.paymentUrl;
      } else {
        window.open(result.paymentUrl, "_blank");
      }
      message.info("برای تکمیل پرداخت به درگاه بانک هدایت شدید.");
    } catch {
      popup?.close();
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const itemColumns: TableColumnsType<InvoiceItem> = [
    { title: "شرح", dataIndex: "itemName", key: "itemName" },
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
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="detail-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/invoices")}
            aria-label="بازگشت"
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} className="detail-title" dir="ltr">
                {data.invoiceNumber}
              </Typography.Title>
              <Tag color={status.color} bordered={false}>
                {status.label}
              </Tag>
            </Space>
            <Typography.Text type="secondary">
              {data.customerName || "مشتری"} — {data.issueDateJalali}
            </Typography.Text>
          </div>
        </Space>

        <Space size="middle" wrap>
          <Button
            icon={<FilePdfOutlined />}
            loading={pdfLoading}
            onClick={() => void handlePdf()}
          >
            دانلود PDF
          </Button>

          {canChangeStatus && options.length > 0 && (
            <Dropdown menu={statusMenu} trigger={["click"]}>
              <Button loading={updateStatus.isPending} icon={<DownOutlined />}>
                تغییر وضعیت
              </Button>
            </Dropdown>
          )}

          {canPayWallet && remaining > 0 && (
            <Button
              icon={<WalletOutlined />}
              onClick={() => setWalletOpen(true)}
            >
              پرداخت از کیف پول
            </Button>
          )}

          {canPayOnline && remaining > 0 && (
            <Button
              type="primary"
              icon={<CreditCardOutlined />}
              onClick={() => setOnlineOpen(true)}
            >
              پرداخت آنلاین
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
          <Descriptions.Item label="شماره سفارش">
            {data.orderNumber || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="نوع فاکتور">
            {data.type === "Official" ? "رسمی" : "غیررسمی"}
          </Descriptions.Item>
          <Descriptions.Item label="تاریخ سررسید">
            {data.dueDateJalali}
          </Descriptions.Item>
          <Descriptions.Item label="جمع کل">
            {formatCurrency(data.subTotal)}
          </Descriptions.Item>
          <Descriptions.Item label="تخفیف">
            {formatCurrency(data.discountAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="مالیات">
            {formatCurrency(data.taxAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="بیمه">
            {formatCurrency(data.insuranceAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="مبلغ نهایی">
            {formatCurrency(data.totalAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="پرداخت‌شده">
            {formatCurrency(data.paidAmount)}
          </Descriptions.Item>
          <Descriptions.Item label="مانده">
            {formatCurrency(remaining)}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card className="detail-card" variant="borderless">
        <Typography.Title level={4}>اقلام فاکتور</Typography.Title>
        <Table<InvoiceItem>
          rowKey="id"
          columns={itemColumns}
          dataSource={data.items}
          pagination={false}
          className="module-table"
          scroll={{ x: 680 }}
          locale={{ emptyText: "آیتمی برای این فاکتور ثبت نشده است." }}
        />
      </Card>

      <Modal
        title="پرداخت از کیف پول"
        open={walletOpen}
        onCancel={() => setWalletOpen(false)}
        onOk={() => void handleWalletPay()}
        okText="تأیید پرداخت"
        cancelText="انصراف"
        confirmLoading={payFromWallet.isPending}
        okButtonProps={{
          disabled: (wallet.data?.balance ?? 0) < remaining,
        }}
      >
        <Space direction="vertical" size="middle" className="payment-modal-body">
          <Typography.Text>
            مبلغ قابل پرداخت: <strong>{formatCurrency(remaining)}</strong>
          </Typography.Text>
          <Typography.Text>
            موجودی کیف پول مشتری:{" "}
            <strong>
              {wallet.isLoading ? "در حال دریافت..." : formatCurrency(wallet.data?.balance ?? 0)}
            </strong>
          </Typography.Text>
          {(wallet.data?.balance ?? 0) < remaining && (
            <Alert
              type="warning"
              showIcon
              message="موجودی کیف پول برای پرداخت کامل این فاکتور کافی نیست."
            />
          )}
        </Space>
      </Modal>

      <Modal
        title="پرداخت آنلاین"
        open={onlineOpen}
        onCancel={() => {
          setOnlineOpen(false);
          setPaymentUrl(null);
        }}
        onOk={() => void handleOnlinePay()}
        okText="انتقال به درگاه بانک"
        cancelText="انصراف"
        confirmLoading={initiateOnline.isPending}
      >
        <Space direction="vertical" size="middle" className="payment-modal-body">
          <Alert
            type="info"
            showIcon
            message="با تأیید، به درگاه پرداخت سامان منتقل می‌شوید."
            description="پس از تکمیل پرداخت در درگاه، وضعیت فاکتور به‌روزرسانی می‌شود."
          />
          <Typography.Text>
            مبلغ قابل پرداخت: <strong>{formatCurrency(remaining)}</strong>
          </Typography.Text>
          {paymentUrl && (
            <Button type="link" href={paymentUrl} target="_blank">
              باز کردن مجدد لینک درگاه
            </Button>
          )}
        </Space>
      </Modal>
    </Space>
  );
}
