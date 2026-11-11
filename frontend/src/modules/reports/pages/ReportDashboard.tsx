import { useMemo, useState } from "react";
import {
  BarChartOutlined,
  BookOutlined,
  DollarOutlined,
  FileTextOutlined,
  PieChartOutlined,
  RiseOutlined,
  TeamOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import {
  Card,
  Col,
  DatePicker,
  Empty,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import type { Dayjs } from "dayjs";
import dayjs from "dayjs";
import {
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  useBadDebtors,
  useCustomerLedger,
  useDebtors,
  useFinancialSummary,
  useFinancialTrend,
  useReportCustomers,
  useSalesReport,
} from "@/modules/reports/hooks/useReports";
import type {
  BadDebtor,
  Debtor,
  LedgerEntry,
  LedgerEntryType,
  SalesReportItem,
} from "@/modules/reports/types";
import { formatCurrency, formatNumber } from "@/shared/utils/format";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

const { RangePicker } = DatePicker;

const CHART_COLORS = ["#1677ff", "#22c55e", "#f59e0b", "#8b5cf6", "#ef4444", "#06b6d4"];

const ledgerTypeLabels: Record<LedgerEntryType, string> = {
  Invoice: "فاکتور",
  Payment: "پرداخت",
  WalletDeposit: "شارژ کیف پول",
};

const ledgerTypeColors: Record<LedgerEntryType, string> = {
  Invoice: "blue",
  Payment: "green",
  WalletDeposit: "purple",
};

export function ReportDashboard() {
  const [range, setRange] = useState<[Dayjs, Dayjs]>([
    dayjs().subtract(5, "month").startOf("month"),
    dayjs(),
  ]);
  const [activeTab, setActiveTab] = useState("financial");
  const [ledgerCustomerId, setLedgerCustomerId] = useState<string | undefined>();
  const [ledgerCustomerName, setLedgerCustomerName] = useState<string | undefined>();
  const [customerSearch, setCustomerSearch] = useState("");
  const debouncedCustomerSearch = useDebouncedValue(customerSearch, 350);

  const fromUtc = range[0].startOf("day").toISOString();
  const toUtc = range[1].endOf("day").toISOString();
  const summary = useFinancialSummary({ fromUtc, toUtc });
  const trend = useFinancialTrend({ fromUtc, toUtc });
  const debtors = useDebtors(toUtc);
  const badDebtors = useBadDebtors(toUtc);
  const sales = useSalesReport({ fromUtc, toUtc });
  const ledger = useCustomerLedger(ledgerCustomerId);
  const customerOptions = useReportCustomers(debouncedCustomerSearch);

  const ledgerSelectOptions = useMemo(() => {
    const options = (customerOptions.data ?? []).map((customer) => ({
      value: customer.id,
      label: `${customer.displayName} — ${customer.mobile}`,
    }));

    if (
      ledgerCustomerId &&
      !options.some((option) => option.value === ledgerCustomerId)
    ) {
      options.unshift({
        value: ledgerCustomerId,
        label: ledgerCustomerName ?? "مشتری انتخاب‌شده",
      });
    }

    return options;
  }, [customerOptions.data, ledgerCustomerId, ledgerCustomerName]);

  const openLedger = (customerId: string, customerName: string) => {
    setLedgerCustomerId(customerId);
    setLedgerCustomerName(customerName);
    setCustomerSearch(customerName);
    setActiveTab("ledger");
  };

  const debtorColumns: TableColumnsType<Debtor> = [
    {
      title: "مشتری",
      dataIndex: "customerName",
      key: "customerName",
      render: (value: string) => value || "-",
    },
    {
      title: "موبایل",
      dataIndex: "mobile",
      key: "mobile",
      width: 145,
      render: (value: string | null) =>
        value ? <span dir="ltr">{value}</span> : "-",
    },
    {
      title: "مبلغ بدهی",
      dataIndex: "outstandingAmount",
      key: "outstandingAmount",
      width: 170,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "تعداد فاکتور معوق",
      dataIndex: "invoiceCount",
      key: "invoiceCount",
      width: 140,
      align: "center",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 135,
      render: (_value, record) => (
        <Typography.Link
          onClick={() => openLedger(record.customerId, record.customerName)}
        >
          مشاهده کاردکس
        </Typography.Link>
      ),
    },
  ];

  const badDebtorColumns: TableColumnsType<BadDebtor> = [
    {
      title: "مشتری",
      dataIndex: "customerName",
      key: "customerName",
      render: (value: string) => value || "-",
    },
    {
      title: "موبایل",
      dataIndex: "mobile",
      key: "mobile",
      width: 145,
      render: (value: string | null) =>
        value ? <span dir="ltr">{value}</span> : "-",
    },
    {
      title: "مبلغ بدهی",
      dataIndex: "outstandingAmount",
      key: "outstandingAmount",
      width: 170,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "تعداد فاکتور",
      dataIndex: "overdueInvoiceCount",
      key: "overdueInvoiceCount",
      width: 120,
      align: "center",
    },
    {
      title: "روزهای تأخیر",
      dataIndex: "maxOverdueDays",
      key: "maxOverdueDays",
      width: 120,
      align: "center",
      render: (value: number) => (
        <Tag color={value > 30 ? "red" : "orange"} bordered={false}>
          {formatNumber(value)} روز
        </Tag>
      ),
    },
    {
      title: "عملیات",
      key: "actions",
      width: 135,
      render: (_value, record) => (
        <Typography.Link
          onClick={() => openLedger(record.customerId, record.customerName)}
        >
          مشاهده کاردکس
        </Typography.Link>
      ),
    },
  ];

  const ledgerColumns: TableColumnsType<LedgerEntry> = [
    {
      title: "تاریخ",
      dataIndex: "dateJalali",
      key: "dateJalali",
      width: 125,
    },
    {
      title: "نوع",
      dataIndex: "entryType",
      key: "entryType",
      width: 120,
      render: (value: LedgerEntryType) => (
        <Tag color={ledgerTypeColors[value]} bordered={false}>
          {ledgerTypeLabels[value] ?? value}
        </Tag>
      ),
    },
    {
      title: "شرح تراکنش",
      dataIndex: "description",
      key: "description",
    },
    {
      title: "بدهکار",
      dataIndex: "debit",
      key: "debit",
      width: 150,
      render: (value: number) =>
        value > 0 ? formatCurrency(value) : "-",
    },
    {
      title: "بستانکار",
      dataIndex: "credit",
      key: "credit",
      width: 150,
      render: (value: number) =>
        value > 0 ? formatCurrency(value) : "-",
    },
    {
      title: "مانده",
      dataIndex: "balance",
      key: "balance",
      width: 160,
      render: (value: number) => (
        <Typography.Text strong type={value > 0 ? "danger" : "success"}>
          {formatCurrency(Math.abs(value))}
        </Typography.Text>
      ),
    },
  ];

  const salesColumns: TableColumnsType<SalesReportItem> = [
    {
      title: "دسته‌بندی",
      dataIndex: "serviceType",
      key: "serviceType",
    },
    {
      title: "تعداد سفارش",
      dataIndex: "orderCount",
      key: "orderCount",
      width: 130,
      align: "center",
    },
    {
      title: "تعداد/مقدار",
      dataIndex: "quantity",
      key: "quantity",
      width: 130,
      align: "center",
      render: (value: number) => formatNumber(value),
    },
    {
      title: "مبلغ فروش",
      dataIndex: "totalAmount",
      key: "totalAmount",
      width: 190,
      render: (value: number) => formatCurrency(value),
    },
  ];

  const trendData = trend.data ?? [];
  const salesData = (sales.data ?? []).map((item) => ({
    ...item,
    value: item.totalAmount,
  }));

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>گزارش‌ها</Typography.Title>
          <Typography.Text type="secondary">
            تحلیل فروش، دریافت‌ها، بدهکاران و کاردکس مشتریان
          </Typography.Text>
        </div>
        <RangePicker
          value={range}
          onChange={(values) => {
            if (values?.[0] && values?.[1]) {
              setRange([values[0], values[1]]);
            }
          }}
          allowClear={false}
          placeholder={["از تاریخ", "تا تاریخ"]}
        />
      </div>

      <Row gutter={[16, 16]}>
        <Col xs={24} md={8}>
          <Card variant="borderless" className="stat-card stat-blue report-stat">
            <Statistic
              title="کل فروش"
              value={formatNumber(summary.data?.totalSales ?? 0)}
              suffix="تومان"
              prefix={<RiseOutlined />}
            />
            <Typography.Text type="secondary" className="stat-hint">
              {formatNumber(summary.data?.invoiceCount ?? 0)} فاکتور در بازه انتخابی
            </Typography.Text>
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card variant="borderless" className="stat-card stat-green report-stat">
            <Statistic
              title="کل دریافتی"
              value={formatNumber(summary.data?.totalReceived ?? 0)}
              suffix="تومان"
              prefix={<WalletOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card variant="borderless" className="stat-card stat-orange report-stat">
            <Statistic
              title="کل بدهی"
              value={formatNumber(summary.data?.totalDebt ?? 0)}
              suffix="تومان"
              prefix={<DollarOutlined />}
            />
          </Card>
        </Col>
      </Row>

      <Card className="detail-card report-tabs-card" variant="borderless">
        <Tabs
          activeKey={activeTab}
          onChange={setActiveTab}
          items={[
            {
              key: "financial",
              label: (
                <span>
                  <BarChartOutlined /> خلاصه مالی
                </span>
              ),
              children: (
                <Card variant="borderless" className="report-chart-card">
                  <Typography.Title level={4}>
                    روند فروش و دریافتی
                  </Typography.Title>
                  {trendData.length === 0 ? (
                    <Empty
                      image={Empty.PRESENTED_IMAGE_SIMPLE}
                      description="داده‌ای برای بازه انتخابی وجود ندارد."
                    />
                  ) : (
                    <ResponsiveContainer width="100%" height={340}>
                      <LineChart data={trendData}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#eef2f7" />
                        <XAxis dataKey="monthJalali" tick={{ fontSize: 11 }} />
                        <YAxis
                          tickFormatter={(value) => formatNumber(Number(value))}
                          tick={{ fontSize: 11 }}
                          width={85}
                        />
                        <Tooltip
                          formatter={(value, name) => [
                            formatCurrency(Number(value)),
                            name === "sales" ? "فروش" : "دریافتی",
                          ]}
                        />
                        <Legend
                          formatter={(value) =>
                            value === "sales" ? "فروش" : "دریافتی"
                          }
                        />
                        <Line
                          type="monotone"
                          dataKey="sales"
                          stroke="#1677ff"
                          strokeWidth={2.5}
                          dot={{ r: 3 }}
                          name="sales"
                        />
                        <Line
                          type="monotone"
                          dataKey="received"
                          stroke="#22c55e"
                          strokeWidth={2.5}
                          dot={{ r: 3 }}
                          name="received"
                        />
                      </LineChart>
                    </ResponsiveContainer>
                  )}
                </Card>
              ),
            },
            {
              key: "debtors",
              label: (
                <span>
                  <TeamOutlined /> بدهکاران
                </span>
              ),
              children: (
                <Table<Debtor>
                  rowKey="customerId"
                  columns={debtorColumns}
                  dataSource={debtors.data ?? []}
                  loading={debtors.isLoading}
                  pagination={{ pageSize: 10, showSizeChanger: false }}
                  scroll={{ x: 850 }}
                  className="module-table"
                  locale={{ emptyText: "بدهکاری وجود ندارد." }}
                />
              ),
            },
            {
              key: "bad-debtors",
              label: (
                <span>
                  <FileTextOutlined /> بدحساب‌ها
                </span>
              ),
              children: (
                <Table<BadDebtor>
                  rowKey="customerId"
                  columns={badDebtorColumns}
                  dataSource={badDebtors.data ?? []}
                  loading={badDebtors.isLoading}
                  pagination={{ pageSize: 10, showSizeChanger: false }}
                  scroll={{ x: 950 }}
                  className="module-table"
                  locale={{ emptyText: "بدحسابی وجود ندارد." }}
                />
              ),
            },
            {
              key: "ledger",
              label: (
                <span>
                  <BookOutlined /> کاردکس مشتری
                </span>
              ),
              children: (
                <Space direction="vertical" size="middle" className="module-stack">
                  <Select
                    showSearch
                    filterOption={false}
                    value={ledgerCustomerId}
                    onSearch={setCustomerSearch}
                    onChange={(value) => {
                      setLedgerCustomerId(value);
                      const option = ledgerSelectOptions.find(
                        (item) => item.value === value,
                      );
                      setLedgerCustomerName(
                        option?.label?.split(" — ")[0] ?? undefined,
                      );
                    }}
                    loading={customerOptions.isLoading}
                    placeholder="جستجو و انتخاب مشتری برای کاردکس"
                    options={ledgerSelectOptions}
                    className="report-customer-select"
                  />

                  {!ledgerCustomerId ? (
                    <Empty
                      image={Empty.PRESENTED_IMAGE_SIMPLE}
                      description="برای مشاهده کاردکس، یک مشتری انتخاب کنید."
                    />
                  ) : (
                    <Table<LedgerEntry>
                      rowKey={(record, index) => `${record.date}-${index}`}
                      columns={ledgerColumns}
                      dataSource={ledger.data ?? []}
                      loading={ledger.isLoading}
                      pagination={false}
                      scroll={{ x: 900 }}
                      className="module-table"
                      locale={{ emptyText: "تراکنشی برای این مشتری ثبت نشده است." }}
                    />
                  )}
                </Space>
              ),
            },
            {
              key: "sales",
              label: (
                <span>
                  <PieChartOutlined /> گزارش فروش
                </span>
              ),
              children: (
                <Row gutter={[20, 20]}>
                  <Col xs={24} lg={10}>
                    <Card variant="borderless" className="report-chart-card">
                      <Typography.Title level={4}>
                        سهم دسته‌بندی‌ها
                      </Typography.Title>
                      {salesData.length === 0 ? (
                        <Empty
                          image={Empty.PRESENTED_IMAGE_SIMPLE}
                          description="فروشی در این بازه ثبت نشده است."
                        />
                      ) : (
                        <ResponsiveContainer width="100%" height={320}>
                          <PieChart>
                            <Pie
                              data={salesData}
                              dataKey="value"
                              nameKey="serviceType"
                              outerRadius={105}
                              innerRadius={55}
                              paddingAngle={3}
                            >
                              {salesData.map((item, index) => (
                                <Cell
                                  key={item.serviceType}
                                  fill={CHART_COLORS[index % CHART_COLORS.length]}
                                />
                              ))}
                            </Pie>
                            <Tooltip
                              formatter={(value) => formatCurrency(Number(value))}
                            />
                            <Legend />
                          </PieChart>
                        </ResponsiveContainer>
                      )}
                    </Card>
                  </Col>
                  <Col xs={24} lg={14}>
                    <Table<SalesReportItem>
                      rowKey="serviceType"
                      columns={salesColumns}
                      dataSource={sales.data ?? []}
                      loading={sales.isLoading}
                      pagination={false}
                      scroll={{ x: 620 }}
                      className="module-table"
                      locale={{ emptyText: "فروشی ثبت نشده است." }}
                    />
                  </Col>
                </Row>
              ),
            },
          ]}
        />
      </Card>
    </Space>
  );
}
