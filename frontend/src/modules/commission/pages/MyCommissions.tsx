import { useMemo, useState } from "react";
import { BankOutlined, CheckCircleOutlined, ClockCircleOutlined } from "@ant-design/icons";
import {
  Card,
  Col,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import {
  commissionStatusMeta,
  commissionStatusOptions,
  payoutTypeLabels,
} from "@/modules/commission/constants";
import { useMyCommissions } from "@/modules/commission/hooks/useCommissions";
import type {
  CommissionEntry,
  CommissionStatus,
} from "@/modules/commission/types";
import { formatCurrency, formatNumber } from "@/shared/utils/format";

export function MyCommissions() {
  const [status, setStatus] = useState<CommissionStatus | undefined>();
  const commissions = useMyCommissions(status);
  const rows = commissions.data ?? [];

  const totals = useMemo(
    () => ({
      calculated: rows
        .filter((entry) => entry.status === "Calculated" || entry.status === "Pending")
        .reduce((sum, entry) => sum + entry.netAmount, 0),
      payable: rows
        .filter((entry) => entry.status === "Payable" || entry.status === "Approved")
        .reduce((sum, entry) => sum + entry.netAmount, 0),
      paid: rows
        .filter((entry) => entry.status === "Paid")
        .reduce((sum, entry) => sum + entry.netAmount, 0),
    }),
    [rows],
  );

  const columns: TableColumnsType<CommissionEntry> = [
    {
      title: "مشتری",
      dataIndex: "customerName",
      key: "customerName",
      render: (value: string | null) => value || "-",
    },
    {
      title: "سرویس",
      dataIndex: "serviceName",
      key: "serviceName",
      render: (value: string | null) => value || "-",
    },
    {
      title: "شماره سفارش",
      dataIndex: "orderNumber",
      key: "orderNumber",
      width: 180,
      render: (value: string | null) =>
        value ? <span dir="ltr">{value}</span> : "-",
    },
    {
      title: "مبلغ پورسانت",
      dataIndex: "netAmount",
      key: "netAmount",
      width: 180,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "نوع پرداخت",
      dataIndex: "payoutType",
      key: "payoutType",
      width: 120,
      render: (value: CommissionEntry["payoutType"]) =>
        payoutTypeLabels[value] ?? value,
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 145,
      render: (value: CommissionStatus) => {
        const meta = commissionStatusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 130,
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>پورسانت‌های من</Typography.Title>
          <Typography.Text type="secondary">
            وضعیت پورسانت‌های محاسبه‌شده، قابل پرداخت و پرداخت‌شده
          </Typography.Text>
        </div>
        <Select
          allowClear
          value={status}
          onChange={(value) => setStatus(value as CommissionStatus | undefined)}
          options={commissionStatusOptions}
          placeholder="فیلتر وضعیت"
          style={{ minWidth: 170 }}
        />
      </div>

      <Row gutter={[16, 16]}>
        <Col xs={24} md={8}>
          <Card variant="borderless" className="stat-card stat-orange">
            <Statistic
              title="در انتظار تأیید"
              value={formatNumber(totals.calculated)}
              suffix="تومان"
              prefix={<ClockCircleOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card variant="borderless" className="stat-card stat-blue">
            <Statistic
              title="قابل پرداخت"
              value={formatNumber(totals.payable)}
              suffix="تومان"
              prefix={<BankOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card variant="borderless" className="stat-card stat-green">
            <Statistic
              title="پرداخت‌شده"
              value={formatNumber(totals.paid)}
              suffix="تومان"
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
      </Row>

      <Table<CommissionEntry>
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={commissions.isLoading || commissions.isFetching}
        className="module-table"
        scroll={{ x: 760 }}
        locale={{ emptyText: "پورسانتی برای شما ثبت نشده است." }}
        pagination={{
          pageSize: 10,
          showSizeChanger: false,
          showTotal: (total) => `مجموع ${total} پورسانت`,
        }}
      />
    </Space>
  );
}
