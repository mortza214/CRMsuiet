import { Alert, Card, Space, Statistic, Table, Tag, Typography } from "antd";
import { WalletOutlined } from "@ant-design/icons";
import type { TableColumnsType } from "antd";
import {
  walletTransactionColors,
  walletTransactionLabels,
} from "@/modules/wallet/constants";
import type { WalletTransaction } from "@/modules/wallet/types";
import { usePortalWallet } from "@/modules/portal/hooks/usePortal";
import { formatCurrency, formatNumber } from "@/shared/utils/format";

export function PortalWallet() {
  const wallet = usePortalWallet();

  const columns: TableColumnsType<WalletTransaction> = [
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 125,
    },
    {
      title: "نوع",
      dataIndex: "transactionType",
      key: "transactionType",
      width: 140,
      render: (value: WalletTransaction["transactionType"]) => (
        <Tag color={walletTransactionColors[value]} bordered={false}>
          {walletTransactionLabels[value] ?? value}
        </Tag>
      ),
    },
    {
      title: "مبلغ",
      dataIndex: "amount",
      key: "amount",
      width: 160,
      render: (value: number, record) => {
        const credit =
          record.transactionType === "Deposit" ||
          record.transactionType === "CommissionPayout";
        return (
          <Typography.Text type={credit ? "success" : "danger"} strong>
            {credit ? "+" : "-"}
            {formatCurrency(Math.abs(value))}
          </Typography.Text>
        );
      },
    },
    {
      title: "موجودی پس از تراکنش",
      dataIndex: "balanceAfter",
      key: "balanceAfter",
      width: 180,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "توضیحات",
      dataIndex: "description",
      key: "description",
      render: (value: string | null) => value || "-",
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <div>
          <Typography.Title level={2}>کیف پول من</Typography.Title>
          <Typography.Text type="secondary">
            موجودی و گردش مالی کیف پول شما
          </Typography.Text>
        </div>
      </div>

      <Card className="portal-wallet-card" variant="borderless">
        <Space direction="vertical" size={4}>
          <Typography.Text className="portal-wallet-label">
            موجودی فعلی
          </Typography.Text>
          <Statistic
            value={formatNumber(wallet.data?.balance?.balance ?? 0)}
            suffix="تومان"
            prefix={<WalletOutlined />}
            valueStyle={{ color: "#15803d", fontSize: 36, fontWeight: 800 }}
          />
        </Space>
      </Card>

      <Alert
        type="info"
        showIcon
        message="شارژ کیف پول"
        description="برای شارژ کیف پول، از طریق ثبت تیکت با پشتیبانی در ارتباط باشید."
      />

      <Card className="portal-table-card" variant="borderless">
        <Typography.Title level={4}>تراکنش‌های کیف پول</Typography.Title>
        <Table<WalletTransaction>
          rowKey="id"
          columns={columns}
          dataSource={wallet.data?.transactions ?? []}
          loading={wallet.isLoading}
          scroll={{ x: 760 }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          locale={{ emptyText: "تراکنشی ثبت نشده است." }}
        />
      </Card>
    </Space>
  );
}
