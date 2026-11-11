import { useState } from "react";
import { CheckOutlined, PlusOutlined, WalletOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useAuthStore } from "@/modules/auth/store";
import { useInvoices } from "@/modules/billing/hooks/useInvoices";
import { useOrderCustomers } from "@/modules/orders/hooks/useOrders";
import {
  manualPaymentMethods,
  paymentMethodLabels,
  transactionStatusMeta,
} from "@/modules/payments/constants";
import {
  useApproveManualPayment,
  useCreateManualPayment,
  useTransactions,
} from "@/modules/payments/hooks/usePayments";
import type {
  PaymentMethod,
  TransactionListItem,
} from "@/modules/payments/types";
import { formatCurrency } from "@/shared/utils/format";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

interface ManualPaymentForm {
  customerId: string;
  invoiceId?: string;
  amount: number;
  paymentMethod: PaymentMethod;
  referenceNumber?: string;
}

export function TransactionList() {
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canCreateManual = hasPermission("Payments.ManualPayment.Create");
  const canApprove = hasPermission("Payments.Payment.Reconcile");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [manualOpen, setManualOpen] = useState(false);
  const [customerSearch, setCustomerSearch] = useState("");
  const [form] = Form.useForm<ManualPaymentForm>();
  const customerId = Form.useWatch("customerId", form);
  const debouncedCustomerSearch = useDebouncedValue(customerSearch, 350);
  const customers = useOrderCustomers(debouncedCustomerSearch);
  const invoices = useInvoices(
    { page: 1, pageSize: 100, customerId },
    Boolean(customerId),
  );
  const transactions = useTransactions({ page, pageSize });
  const createManual = useCreateManualPayment();
  const approveManual = useApproveManualPayment();

  const openManual = () => {
    form.resetFields();
    form.setFieldsValue({ paymentMethod: "ManualTransfer", amount: 0 });
    setCustomerSearch("");
    setManualOpen(true);
  };

  const handleInvoiceChange = (invoiceId: string | undefined) => {
    const invoice = invoices.data?.items.find((item) => item.id === invoiceId);
    if (!invoice) return;
    form.setFieldValue(
      "amount",
      Math.max(0, invoice.totalAmount - invoice.paidAmount),
    );
  };

  const submitManual = async (values: ManualPaymentForm) => {
    try {
      await createManual.mutateAsync({
        customerId: values.customerId,
        invoiceId: values.invoiceId ?? null,
        amount: values.amount,
        paymentMethod: values.paymentMethod,
        referenceNumber: values.referenceNumber?.trim() || null,
      });
      message.success("پرداخت دستی ثبت شد و در انتظار تأیید مدیر مالی است.");
      setManualOpen(false);
      form.resetFields();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const approve = async (id: string) => {
    try {
      await approveManual.mutateAsync(id);
      message.success("پرداخت دستی تأیید شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<TransactionListItem> = [
    {
      title: "کد رهگیری",
      dataIndex: "trackingCode",
      key: "trackingCode",
      width: 175,
      render: (value: string) => (
        <Typography.Text strong copyable={{ text: value }} dir="ltr">
          {value}
        </Typography.Text>
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
      title: "شماره فاکتور",
      dataIndex: "invoiceNumber",
      key: "invoiceNumber",
      width: 155,
      render: (value: string | null) => value || "-",
    },
    {
      title: "مبلغ",
      dataIndex: "amount",
      key: "amount",
      width: 155,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "روش پرداخت",
      dataIndex: "paymentMethod",
      key: "paymentMethod",
      width: 125,
      render: (value: PaymentMethod) => paymentMethodLabels[value] ?? value,
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 115,
      render: (value: TransactionListItem["status"]) => {
        const meta = transactionStatusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "تاریخ پرداخت",
      key: "paidAt",
      width: 125,
      render: (_value, record) =>
        record.paidAtJalali || record.createdAtJalali,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 105,
      render: (_value, record) =>
        canApprove &&
        record.status === "Pending" &&
        (record.paymentMethod === "ManualTransfer" ||
          record.paymentMethod === "Cash") ? (
          <Button
            type="link"
            icon={<CheckOutlined />}
            loading={approveManual.isPending}
            onClick={() => void approve(record.id)}
          >
            تأیید
          </Button>
        ) : null,
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>پرداخت‌ها</Typography.Title>
          <Typography.Text type="secondary">
            مشاهده تراکنش‌های آنلاین، دستی و کیف پول
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <WalletOutlined /> {transactions.data?.totalCount ?? 0} تراکنش
        </span>
      </div>

      <div className="table-toolbar">
        <Typography.Text type="secondary">
          تراکنش‌های در انتظار نیاز به تأیید مدیر مالی دارند.
        </Typography.Text>
        {canCreateManual && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openManual}>
            ثبت پرداخت دستی
          </Button>
        )}
      </div>

      <Table<TransactionListItem>
        rowKey="id"
        columns={columns}
        dataSource={transactions.data?.items ?? []}
        loading={transactions.isLoading || transactions.isFetching}
        scroll={{ x: 1150 }}
        className="module-table"
        locale={{ emptyText: "تراکنشی یافت نشد." }}
        pagination={{
          current: transactions.data?.page ?? page,
          pageSize: transactions.data?.pageSize ?? pageSize,
          total: transactions.data?.totalCount ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50],
          showTotal: (total) => `مجموع ${total} تراکنش`,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPageSize !== pageSize ? 1 : nextPage);
            setPageSize(nextPageSize);
          },
        }}
      />

      <Modal
        title="ثبت پرداخت دستی"
        open={manualOpen}
        onCancel={() => setManualOpen(false)}
        onOk={() => form.submit()}
        okText="ثبت پرداخت"
        cancelText="انصراف"
        confirmLoading={createManual.isPending}
        width={560}
      >
        <Form form={form} layout="vertical" onFinish={submitManual}>
          <Form.Item
            name="customerId"
            label="مشتری"
            rules={[{ required: true, message: "مشتری را انتخاب کنید." }]}
          >
            <Select
              showSearch
              filterOption={false}
              onSearch={setCustomerSearch}
              loading={customers.isLoading}
              placeholder="جستجو و انتخاب مشتری..."
              options={(customers.data ?? []).map((customer) => ({
                value: customer.id,
                label: `${customer.displayName} — ${customer.mobile}`,
              }))}
            />
          </Form.Item>

          <Form.Item name="invoiceId" label="فاکتور (اختیاری)">
            <Select
              allowClear
              disabled={!customerId}
              loading={invoices.isLoading}
              onChange={handleInvoiceChange}
              placeholder="در صورت پرداخت بابت فاکتور"
              options={(invoices.data?.items ?? []).map((invoice) => ({
                value: invoice.id,
                label: `${invoice.invoiceNumber} — مانده ${formatCurrency(
                  Math.max(0, invoice.totalAmount - invoice.paidAmount),
                )}`,
              }))}
            />
          </Form.Item>

          <Form.Item
            name="amount"
            label="مبلغ (تومان)"
            rules={[
              { required: true, message: "مبلغ را وارد کنید." },
              { type: "number", min: 1, message: "مبلغ معتبر نیست." },
            ]}
          >
            <InputNumber
              min={1}
              style={{ width: "100%" }}
              formatter={(value) =>
                `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")
              }
            />
          </Form.Item>

          <Form.Item
            name="paymentMethod"
            label="روش پرداخت"
            rules={[{ required: true, message: "روش پرداخت را انتخاب کنید." }]}
          >
            <Select options={manualPaymentMethods} />
          </Form.Item>

          <Form.Item name="referenceNumber" label="شماره پیگیری / رسید">
            <Input placeholder="مثلاً شماره پیگیری کارت به کارت" dir="ltr" />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
