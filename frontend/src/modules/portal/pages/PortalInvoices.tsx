import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  CreditCardOutlined,
  FilePdfOutlined,
  FileTextOutlined,
  WalletOutlined,
  UploadOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Segmented,
  Space,
  Table,
  Upload,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { invoiceStatusMeta } from "@/modules/billing/constants";
import type { InvoiceListItem, InvoiceStatus } from "@/modules/billing/types";
import {
  getPortalErrorMessage,
  openPortalInvoicePdf,
  portalApi,
} from "@/modules/portal/api";
import type { ApiEnvelope } from "@/modules/auth/types";
import {
  usePortalInitiateOnline,
  usePortalInvoices,
  usePortalPayInvoiceFromWallet,
  usePortalWallet,
} from "@/modules/portal/hooks/usePortal";
import { formatCurrency, resolveApiFileUrl } from "@/shared/utils/format";

export function PortalInvoices() {
  const { message, modal } = App.useApp();
  const [pdfLoadingId, setPdfLoadingId] = useState<string | null>(null);
  const [paymentInvoice, setPaymentInvoice] = useState<InvoiceListItem | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<"Gateway" | "Manual">("Gateway");
  const [receiptUrl, setReceiptUrl] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [manualForm] = Form.useForm<{ referenceNumber: string; amount: number }>();
  const queryClient = useQueryClient();
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

  const uploadReceipt = async (file: File) => {
    setUploading(true);
    try {
      const body = new FormData();
      body.append("file", file);
      const response = await portalApi.post<ApiEnvelope<{ fileUrl: string }>>(
        "/files/upload",
        body,
        { headers: { "Content-Type": "multipart/form-data" } },
      );
      setReceiptUrl(response.data.data?.fileUrl ?? null);
      message.success("فیش آپلود شد.");
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    } finally {
      setUploading(false);
    }
  };

  const submitManual = async (values: { referenceNumber: string; amount: number }) => {
    if (!paymentInvoice) return;
    if (!receiptUrl) {
      message.warning("تصویر فیش را آپلود کنید.");
      return;
    }
    try {
      await portalApi.post("/portal/payments/manual", {
        invoiceId: paymentInvoice.id,
        amount: values.amount,
        paymentMethod: "ManualTransfer",
        referenceNumber: values.referenceNumber,
        receiptImageUrl: receiptUrl,
      });
      message.success("فیش ثبت شد و در انتظار تأیید مالی است.");
      setPaymentInvoice(null);
      setReceiptUrl(null);
      manualForm.resetFields();
      await queryClient.invalidateQueries({ queryKey: ["portal"] });
    } catch (error) {
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
                  onClick={() => {
                    setPaymentInvoice(record);
                    setPaymentMethod("Gateway");
                    setReceiptUrl(null);
                    manualForm.setFieldsValue({ amount: record.totalAmount - record.paidAmount });
                  }}
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
      <Modal
        title="پرداخت فاکتور"
        open={Boolean(paymentInvoice)}
        onCancel={() => setPaymentInvoice(null)}
        footer={null}
      >
        {paymentInvoice && (
          <Space direction="vertical" size={12} style={{ width: "100%" }}>
            <Typography.Text>
              شماره فاکتور: <strong dir="ltr">{paymentInvoice.invoiceNumber}</strong>
            </Typography.Text>
            <Typography.Text>
              مبلغ: <strong>{formatCurrency(paymentInvoice.totalAmount - paymentInvoice.paidAmount)}</strong>
            </Typography.Text>
            <Segmented
              block
              value={paymentMethod}
              onChange={(value) => setPaymentMethod(value as "Gateway" | "Manual")}
              options={[
                { value: "Gateway", label: "درگاه پرداخت" },
                { value: "Manual", label: "ثبت فیش" },
              ]}
            />
            {paymentMethod === "Gateway" ? (
              <Button
                type="primary"
                block
                loading={initiateOnline.isPending}
                onClick={() => {
                  void handleOnlinePay(paymentInvoice);
                  setPaymentInvoice(null);
                }}
              >
                انتقال به درگاه
              </Button>
            ) : (
              <Form form={manualForm} layout="vertical" onFinish={submitManual}>
                <Form.Item name="referenceNumber" label="شماره پیگیری" rules={[{ required: true }]}>
                  <Input dir="ltr" />
                </Form.Item>
                <Form.Item name="amount" label="مبلغ" rules={[{ required: true }]}>
                  <InputNumber style={{ width: "100%" }} min={1} />
                </Form.Item>
                <Form.Item label="تصویر فیش (JPG / PNG / PDF)" required>
                  <Upload
                    accept=".jpg,.jpeg,.png,.pdf"
                    maxCount={1}
                    showUploadList={false}
                    customRequest={({ file, onSuccess, onError }) => {
                      void uploadReceipt(file as File).then(
                        () => onSuccess?.(undefined),
                        (error) => onError?.(error as Error),
                      );
                    }}
                  >
                    <Button icon={<UploadOutlined />} loading={uploading}>
                      آپلود فیش
                    </Button>
                  </Upload>
                  {receiptUrl && (
                    <Typography.Paragraph style={{ margin: "8px 0 0" }}>
                      <Typography.Link href={resolveApiFileUrl(receiptUrl)} target="_blank">
                        مشاهده فایل آپلودشده
                      </Typography.Link>
                    </Typography.Paragraph>
                  )}
                </Form.Item>
                <Button type="primary" htmlType="submit" block>
                  ثبت فیش
                </Button>
              </Form>
            )}
          </Space>
        )}
      </Modal>
    </Space>
  );
}
