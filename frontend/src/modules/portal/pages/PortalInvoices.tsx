import { useState } from "react";
import {
  CreditCardOutlined,
  FilePdfOutlined,
  FileTextOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { invoiceStatusMeta } from "@/modules/billing/constants";
import type { InvoiceListItem, InvoiceStatus } from "@/modules/billing/types";
import {
  getPortalErrorMessage,
  openPortalInvoicePdf,
} from "@/modules/portal/api";
import {
  usePortalInitiateOnline,
  usePortalInvoices,
  usePortalPayInvoiceFromWallet,
  usePortalWallet,
} from "@/modules/portal/hooks/usePortal";
import { formatCurrency } from "@/shared/utils/format";

export function PortalInvoices() {
  const { message, modal } = App.useApp();
  const [pdfLoadingId, setPdfLoadingId] = useState<string | null>(null);
  const invoices = usePortalInvoices();
  const wallet = usePortalWallet();
  const payFromWallet = usePortalPayInvoiceFromWallet();
  const initiateOnline = usePortalInitiateOnline();

  const handlePdf = async (invoiceId: string) => {
    setPdfLoadingId(invoiceId);
    try {
      await openPortalInvoicePdf(invoiceId);
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    } finally {
      setPdfLoadingId(null);
    }
  };

  const handleWalletPay = (invoice: InvoiceListItem) => {
    const remaining = invoice.totalAmount - invoice.paidAmount;
    const balance = wallet.data?.balance?.balance ?? 0;

    if (balance < remaining) {
      message.warning("موجودی کیف پول برای پرداخت این فاکتور کافی نیست.");
      return;
    }

    modal.confirm({
      title: "پرداخت از کیف پول",
      content: `مبلغ ${formatCurrency(remaining)} از کیف پول شما کسر می‌شود. ادامه می‌دهید؟`,
      okText: "تأیید پرداخت",
      cancelText: "انصراف",
      onOk: async () => {
        try {
          await payFromWallet.mutateAsync(invoice.id);
          message.success("فاکتور با موفقیت از کیف پول پرداخت شد.");
        } catch (error) {
          message.error(getPortalErrorMessage(error));
        }
      },
    });
  };

  const handleOnlinePay = async (invoice: InvoiceListItem) => {
    const popup = window.open("about:blank", "_blank");

    try {
      const result = await initiateOnline.mutateAsync(invoice.id);
      if (popup) {
        popup.location.href = result.paymentUrl;
      } else {
        window.open(result.paymentUrl, "_blank");
      }
      message.info("برای تکمیل پرداخت به درگاه بانک هدایت شدید.");
    } catch (error) {
      popup?.close();
      message.error(getPortalErrorMessage(error));
    }
  };

  const columns: TableColumnsType<InvoiceListItem> = [
    {
      title: "شماره فاکتور",
      dataIndex: "invoiceNumber",
      key: "invoiceNumber",
      width: 155,
      render: (value: string) => (
        <Typography.Text strong dir="ltr">
          {value}
        </Typography.Text>
      ),
    },
    {
      title: "تاریخ صدور",
      dataIndex: "issueDateJalali",
      key: "issueDateJalali",
      width: 120,
    },
    {
      title: "مبلغ کل",
      dataIndex: "totalAmount",
      key: "totalAmount",
      width: 155,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "پرداخت‌شده",
      dataIndex: "paidAmount",
      key: "paidAmount",
      width: 155,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "مانده",
      key: "remaining",
      width: 155,
      render: (_value, record) => {
        const remaining = Math.max(0, record.totalAmount - record.paidAmount);
        return (
          <Typography.Text type={remaining > 0 ? "danger" : "success"}>
            {formatCurrency(remaining)}
          </Typography.Text>
        );
      },
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 130,
      render: (value: InvoiceStatus) => {
        const meta = invoiceStatusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "عملیات",
      key: "actions",
      width: 260,
      render: (_value, record) => {
        const remaining = Math.max(0, record.totalAmount - record.paidAmount);
        return (
          <Space size="small" wrap>
            <Button
              type="text"
              icon={<FilePdfOutlined />}
              loading={pdfLoadingId === record.id}
              onClick={() => void handlePdf(record.id)}
              aria-label="دانلود PDF"
            />
            {remaining > 0 && record.status === "PendingPayment" && (
              <>
                <Button
                  size="small"
                  icon={<WalletOutlined />}
                  onClick={() => handleWalletPay(record)}
                >
                  کیف پول
                </Button>
                <Button
                  size="small"
                  type="primary"
                  ghost
                  icon={<CreditCardOutlined />}
                  loading={initiateOnline.isPending}
                  onClick={() => void handleOnlinePay(record)}
                >
                  پرداخت آنلاین
                </Button>
              </>
            )}
            {remaining > 0 && record.status === "Draft" && (
              <Typography.Text type="secondary">در انتظار تایید فروش</Typography.Text>
            )}
          </Space>
        );
      },
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <div>
          <Typography.Title level={2}>فاکتورهای من</Typography.Title>
          <Typography.Text type="secondary">
            مشاهده، دانلود و پرداخت فاکتورها
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <FileTextOutlined /> {invoices.data?.length ?? 0} فاکتور
        </span>
      </div>

      <Card className="portal-table-card" variant="borderless">
        <Table<InvoiceListItem>
          rowKey="id"
          columns={columns}
          dataSource={invoices.data ?? []}
          loading={invoices.isLoading}
          scroll={{ x: 1080 }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          locale={{ emptyText: "فاکتوری ثبت نشده است." }}
        />
      </Card>
    </Space>
  );
}
