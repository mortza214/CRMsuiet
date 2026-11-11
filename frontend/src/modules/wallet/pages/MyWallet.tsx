import { useState } from "react";
import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Card,
  Form,
  InputNumber,
  Modal,
  Result,
  Space,
  Spin,
  Statistic,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useAuthStore } from "@/modules/auth/store";
import {
  walletTransactionColors,
  walletTransactionLabels,
} from "@/modules/wallet/constants";
import { useMyWallet, useRequestWithdraw } from "@/modules/wallet/hooks/useWallet";
import type { WalletTransaction } from "@/modules/wallet/types";
import { formatCurrency, formatNumber } from "@/shared/utils/format";

interface WithdrawForm {
  amount: number;
}

export function MyWallet() {
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const [withdrawOpen, setWithdrawOpen] = useState(false);
  const [form] = Form.useForm<WithdrawForm>();
  const wallet = useMyWallet();
  const withdraw = useRequestWithdraw();

  if (wallet.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (wallet.isError || !wallet.data?.balance) {
    return (
      <Result
        status="500"
        title="دریافت اطلاعات کیف پول ناموفق بود"
        subTitle="لطفاً دوباره تلاش کنید."
      />
    );
  }

  const { balance, transactions } = wallet.data;
  const isEmployee = balance.ownerType === "Employee";
  const canWithdraw =
    isEmployee &&
    balance.balance > 0 &&
    hasPermission("Wallet.Withdrawal.Request");

  const submitWithdraw = async (values: WithdrawForm) => {
    try {
      await withdraw.mutateAsync(values.amount);
      message.success("درخواست برداشت ثبت شد و در انتظار تأیید مدیر مالی است.");
      setWithdrawOpen(false);
      form.resetFields();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<WalletTransaction> = [
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 130,
    },
    {
      title: "نوع تراکنش",
      dataIndex: "transactionType",
      key: "transactionType",
      width: 150,
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
      width: 170,
      render: (value: number, record) => {
        const isCredit =
          record.transactionType === "Deposit" ||
          record.transactionType === "CommissionPayout";
        return (
          <Typography.Text
            type={isCredit ? "success" : "danger"}
            strong
            dir="ltr"
          >
            {isCredit ? "+" : "-"}
            {formatCurrency(Math.abs(value))}
          </Typography.Text>
        );
      },
    },
    {
      title: "موجودی پس از تراکنش",
      dataIndex: "balanceAfter",
      key: "balanceAfter",
      width: 190,
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
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>
            {isEmployee ? "کیف پول من" : "کیف پول"}
          </Typography.Title>
          <Typography.Text type="secondary">
            {isEmployee
              ? "موجودی پورسانت، برداشت‌ها و تراکنش‌های کیف پول"
              : "موجودی و تراکنش‌های کیف پول شما"}
          </Typography.Text>
        </div>
      </div>

      <Card className="wallet-hero" variant="borderless">
        <div className="wallet-hero-content">
          <div>
            <Typography.Text className="wallet-hero-label">
              موجودی فعلی کیف پول
            </Typography.Text>
            <Statistic
              value={formatNumber(balance.balance)}
              suffix="تومان"
              valueStyle={{ color: "#15803d", fontSize: 38, fontWeight: 800 }}
            />
            <Space size="small" className="wallet-hero-meta">
              <Tag color={isEmployee ? "blue" : "purple"} bordered={false}>
                {isEmployee ? "کیف پول پورسانت" : "کیف پول مشتری"}
              </Tag>
            </Space>
          </div>

          <div className="wallet-hero-actions">
            <WalletOutlined className="wallet-hero-icon" />
            {canWithdraw && (
              <Button
                type="primary"
                icon={<ArrowDownOutlined />}
                onClick={() => setWithdrawOpen(true)}
              >
                درخواست برداشت
              </Button>
            )}
          </div>
        </div>
      </Card>

      {!isEmployee && (
        <Alert
          type="info"
          showIcon
          message="شارژ کیف پول مشتری از طریق درگاه پرداخت در پورتال مشتری انجام می‌شود."
        />
      )}

      <Card className="detail-card" variant="borderless">
        <Typography.Title level={4}>تراکنش‌های کیف پول</Typography.Title>
        <Table<WalletTransaction>
          rowKey="id"
          columns={columns}
          dataSource={transactions}
          loading={wallet.isFetching}
          className="module-table"
          scroll={{ x: 760 }}
          pagination={{
            pageSize: 10,
            showSizeChanger: false,
            showTotal: (total) => `مجموع ${total} تراکنش`,
          }}
          locale={{ emptyText: "تراکنشی برای کیف پول ثبت نشده است." }}
        />
      </Card>

      <Modal
        title="درخواست برداشت از کیف پول"
        open={withdrawOpen}
        onCancel={() => setWithdrawOpen(false)}
        onOk={() => form.submit()}
        okText="ثبت درخواست"
        cancelText="انصراف"
        confirmLoading={withdraw.isPending}
      >
        <Form form={form} layout="vertical" onFinish={submitWithdraw}>
          <Alert
            type="info"
            showIcon
            message={`موجودی قابل برداشت: ${formatCurrency(balance.balance)}`}
            style={{ marginBottom: 16 }}
          />
          <Form.Item
            name="amount"
            label="مبلغ درخواستی (تومان)"
            rules={[
              { required: true, message: "مبلغ را وارد کنید." },
              {
                type: "number",
                min: 1,
                max: balance.balance,
                message: "مبلغ درخواستی معتبر نیست.",
              },
            ]}
          >
            <InputNumber
              min={1}
              max={balance.balance}
              style={{ width: "100%" }}
              formatter={(value) =>
                `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")
              }
            />
          </Form.Item>
          <Space size="small">
            <ArrowUpOutlined />
            <Typography.Text type="secondary">
              درخواست پس از تأیید مدیر مالی از موجودی کسر می‌شود.
            </Typography.Text>
          </Space>
        </Form>
      </Modal>
    </Space>
  );
}
