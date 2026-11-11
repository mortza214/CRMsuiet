import { useState } from "react";
import {
  EyeOutlined,
  FilePdfOutlined,
  FileTextOutlined,
  SearchOutlined,
} from "@ant-design/icons";
import {
  App,
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
import {
  invoiceStatusMeta,
  invoiceStatusOptions,
  invoiceTypeMeta,
} from "@/modules/billing/constants";
import { useInvoices } from "@/modules/billing/hooks/useInvoices";
import { openInvoicePdf } from "@/modules/billing/api/invoicePdf";
import type {
  InvoiceListItem,
  InvoiceStatus,
} from "@/modules/billing/types";
import { formatCurrency } from "@/shared/utils/format";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

export function InvoiceList() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<InvoiceStatus | undefined>();
  const [pdfLoadingId, setPdfLoadingId] = useState<string | null>(null);
  const debouncedSearch = useDebouncedValue(search, 400);
  const invoices = useInvoices({
    page,
    pageSize,
    search: debouncedSearch,
    status,
  });

  const handlePdf = async (id: string) => {
    setPdfLoadingId(id);
    try {
      await openInvoicePdf(id);
    } catch {
      message.error("دریافت فایل PDF فاکتور ناموفق بود.");
    } finally {
      setPdfLoadingId(null);
    }
  };

  const columns: TableColumnsType<InvoiceListItem> = [
    {
      title: "شماره فاکتور",
      dataIndex: "invoiceNumber",
      key: "invoiceNumber",
      width: 160,
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
      width: 180,
      render: (value: string | null) => value || "-",
    },
    {
      title: "شماره سفارش",
      dataIndex: "orderNumber",
      key: "orderNumber",
      width: 160,
      render: (value: string | null) =>
        value ? <span dir="ltr">{value}</span> : "-",
    },
    {
      title: "نوع",
      dataIndex: "type",
      key: "type",
      width: 100,
      render: (value: InvoiceListItem["type"]) => {
        const meta = invoiceTypeMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "مبلغ کل",
      dataIndex: "totalAmount",
      key: "totalAmount",
      width: 165,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "پرداخت‌شده",
      dataIndex: "paidAmount",
      key: "paidAmount",
      width: 165,
      render: (value: number) => formatCurrency(value),
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
      title: "تاریخ صدور",
      dataIndex: "issueDateJalali",
      key: "issueDateJalali",
      width: 120,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 150,
      fixed: "left",
      render: (_value, record) => (
        <Space size="small">
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/invoices/${record.id}`)}
          >
            مشاهده
          </Button>
          <Button
            type="text"
            icon={<FilePdfOutlined />}
            loading={pdfLoadingId === record.id}
            onClick={() => void handlePdf(record.id)}
            aria-label="دانلود PDF"
          />
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>فاکتورها</Typography.Title>
          <Typography.Text type="secondary">
            مدیریت فاکتورهای رسمی و غیررسمی، وضعیت پرداخت و خروجی PDF
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <FileTextOutlined /> {invoices.data?.totalCount ?? 0} فاکتور
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
          placeholder="جستجو بر اساس شماره فاکتور یا نام مشتری..."
          className="toolbar-search"
        />

        <Select
          allowClear
          value={status}
          onChange={(value) => {
            setStatus(value as InvoiceStatus | undefined);
            setPage(1);
          }}
          options={invoiceStatusOptions}
          placeholder="فیلتر وضعیت"
          style={{ minWidth: 170 }}
        />
      </div>

      <Table<InvoiceListItem>
        rowKey="id"
        columns={columns}
        dataSource={invoices.data?.items ?? []}
        loading={invoices.isLoading || invoices.isFetching}
        scroll={{ x: 1300 }}
        className="module-table"
        locale={{ emptyText: "فاکتوری یافت نشد." }}
        pagination={{
          current: invoices.data?.page ?? page,
          pageSize: invoices.data?.pageSize ?? pageSize,
          total: invoices.data?.totalCount ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50],
          showTotal: (total) => `مجموع ${total} فاکتور`,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPageSize !== pageSize ? 1 : nextPage);
            setPageSize(nextPageSize);
          },
        }}
      />
    </Space>
  );
}
