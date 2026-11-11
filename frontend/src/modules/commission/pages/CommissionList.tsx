import { useState } from "react";
import {
  CheckOutlined,
  CloseOutlined,
  SettingOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Checkbox,
  DatePicker,
  Form,
  InputNumber,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import type { Dayjs } from "dayjs";
import { useAuthStore } from "@/modules/auth/store";
import {
  commissionStatusMeta,
  commissionStatusOptions,
  payoutTypeLabels,
} from "@/modules/commission/constants";
import {
  useApproveCommission,
  useCommissions,
  useCreateCommissionRule,
  useRejectCommission,
  useUpdateCommissionShares,
} from "@/modules/commission/hooks/useCommissions";
import type {
  CommissionEntry,
  CommissionPayoutType,
  CommissionStatus,
} from "@/modules/commission/types";
import { useCustomerOwners } from "@/modules/customers/hooks/useCustomers";
import { useOrderServices } from "@/modules/orders/hooks/useOrders";
import { formatCurrency } from "@/shared/utils/format";

const { RangePicker } = DatePicker;

interface RuleForm {
  serviceId: string;
  percentage: number;
  fixedAmount?: number;
  payoutType: CommissionPayoutType;
}

export function CommissionList() {
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canManageRules = hasPermission("Commission.Rule.Manage");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [status, setStatus] = useState<CommissionStatus | undefined>();
  const [employeeId, setEmployeeId] = useState<string | undefined>();
  const [range, setRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [ruleOpen, setRuleOpen] = useState(false);
  const [ruleForm] = Form.useForm<RuleForm>();
  const commissions = useCommissions({
    page,
    pageSize,
    status,
    employeeId,
    fromUtc: range?.[0]?.startOf("day").toISOString(),
    toUtc: range?.[1]?.endOf("day").toISOString(),
  });
  const employees = useCustomerOwners();
  const services = useOrderServices("");
  const approve = useApproveCommission();
  const reject = useRejectCommission();
  const createRule = useCreateCommissionRule();
  const updateShares = useUpdateCommissionShares();
  const [sharesEntry, setSharesEntry] = useState<CommissionEntry | null>(null);
  const [sharesForm] = Form.useForm<{
    shares: Array<{ userId?: string; percentage: number; isCompanyShare?: boolean }>;
  }>();

  const openShares = (entry: CommissionEntry) => {
    setSharesEntry(entry);
    sharesForm.setFieldsValue({
      shares: (entry.shares ?? []).map((share) => ({
        userId: share.userId ?? undefined,
        percentage: share.percentage,
        isCompanyShare: share.isCompanyShare,
      })),
    });
  };

  const submitShares = async (values: {
    shares: Array<{ userId?: string; percentage: number; isCompanyShare?: boolean }>;
  }) => {
    if (!sharesEntry) return;
    const total = values.shares.reduce((sum, share) => sum + Number(share.percentage || 0), 0);
    if (total !== 100) {
      message.warning("مجموع درصد سهم‌ها باید ۱۰۰ باشد.");
      return;
    }
    try {
      await updateShares.mutateAsync({
        id: sharesEntry.id,
        shares: values.shares.map((share) => ({
          userId: share.isCompanyShare ? null : share.userId,
          percentage: Number(share.percentage),
          isCompanyShare: Boolean(share.isCompanyShare),
        })),
      });
      message.success("سهم‌ها ذخیره شد.");
      setSharesEntry(null);
    } catch {
      // handled globally
    }
  };

  const submitRule = async (values: RuleForm) => {
    if (!values.percentage && !values.fixedAmount) {
      message.warning("درصد یا مبلغ ثابت پورسانت را وارد کنید.");
      return;
    }

    try {
      await createRule.mutateAsync({
        serviceId: values.serviceId,
        percentage: values.percentage ?? 0,
        fixedAmount: values.fixedAmount || null,
        payoutType: values.payoutType,
      });
      message.success("قانون پورسانت ثبت شد.");
      setRuleOpen(false);
      ruleForm.resetFields();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handleApprove = async (id: string) => {
    try {
      await approve.mutateAsync(id);
      message.success("پورسانت تأیید و قابل پرداخت شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handleReject = async (id: string) => {
    try {
      await reject.mutateAsync(id);
      message.success("پورسانت رد شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<CommissionEntry> = [
    {
      title: "کارمند",
      dataIndex: "employeeName",
      key: "employeeName",
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
      title: "مبلغ پورسانت",
      dataIndex: "netAmount",
      key: "netAmount",
      width: 165,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "نوع پرداخت",
      dataIndex: "payoutType",
      key: "payoutType",
      width: 110,
      render: (value: CommissionPayoutType) => payoutTypeLabels[value] ?? value,
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 135,
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
      title: "تاریخ محاسبه",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 125,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 185,
      fixed: "left",
      render: (_value, record) => {
        if (record.status === "Calculated" || record.status === "Pending") {
          return (
            <Space size="small">
              <Button
                type="link"
                icon={<CheckOutlined />}
                loading={approve.isPending}
                onClick={() => void handleApprove(record.id)}
              >
                تأیید
              </Button>
              {record.status === "Pending" && (
                <Button type="link" onClick={() => openShares(record)}>
                  سهم‌ها
                </Button>
              )}
              <Popconfirm
                title="این پورسانت رد شود؟"
                okText="رد کردن"
                cancelText="انصراف"
                onConfirm={() => void handleReject(record.id)}
              >
                <Button
                  type="link"
                  danger
                  icon={<CloseOutlined />}
                  loading={reject.isPending}
                >
                  رد
                </Button>
              </Popconfirm>
            </Space>
          );
        }

        if (record.status === "Payable") {
          return (
            <Popconfirm
              title="این پورسانت رد شود؟"
              okText="رد کردن"
              cancelText="انصراف"
              onConfirm={() => void handleReject(record.id)}
            >
              <Button
                type="link"
                danger
                icon={<CloseOutlined />}
                loading={reject.isPending}
              >
                رد
              </Button>
            </Popconfirm>
          );
        }

        return <Typography.Text type="secondary">-</Typography.Text>;
      },
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>مدیریت پورسانت‌ها</Typography.Title>
          <Typography.Text type="secondary">
            تأیید، رد و مدیریت پورسانت کارشناسان فروش
          </Typography.Text>
        </div>
        <Space>
          {canManageRules && (
            <Button
              icon={<SettingOutlined />}
              onClick={() => setRuleOpen(true)}
            >
              تعریف قانون پورسانت
            </Button>
          )}
        </Space>
      </div>

      <Card className="detail-card" variant="borderless">
        <Space size="middle" wrap className="commission-filters">
          <Select
            allowClear
            value={status}
            onChange={(value) => {
              setStatus(value as CommissionStatus | undefined);
              setPage(1);
            }}
            options={commissionStatusOptions}
            placeholder="وضعیت"
            style={{ minWidth: 160 }}
          />
          <Select
            allowClear
            showSearch
            value={employeeId}
            onChange={(value) => {
              setEmployeeId(value);
              setPage(1);
            }}
            options={(employees.data ?? []).map((user) => ({
              value: user.id,
              label: user.fullName,
            }))}
            optionFilterProp="label"
            placeholder="نام کارمند"
            style={{ minWidth: 190 }}
          />
          <RangePicker
            value={range}
            onChange={(values) =>
              setRange(
                values?.[0] && values?.[1]
                  ? [values[0], values[1]]
                  : null,
              )
            }
            placeholder={["از تاریخ", "تا تاریخ"]}
          />
          {(status || employeeId || range) && (
            <Button
              type="link"
              onClick={() => {
                setStatus(undefined);
                setEmployeeId(undefined);
                setRange(null);
                setPage(1);
              }}
            >
              پاک کردن فیلترها
            </Button>
          )}
        </Space>
      </Card>

      <Table<CommissionEntry>
        rowKey="id"
        columns={columns}
        dataSource={commissions.data?.items ?? []}
        loading={commissions.isLoading || commissions.isFetching}
        scroll={{ x: 1050 }}
        className="module-table"
        locale={{ emptyText: "پورسانتی یافت نشد." }}
        pagination={{
          current: commissions.data?.page ?? page,
          pageSize: commissions.data?.pageSize ?? pageSize,
          total: commissions.data?.totalCount ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50],
          showTotal: (total) => `مجموع ${total} پورسانت`,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPageSize !== pageSize ? 1 : nextPage);
            setPageSize(nextPageSize);
          },
        }}
      />

      <Modal
        title="ویرایش سهم‌های پورسانت"
        open={sharesEntry !== null}
        onCancel={() => setSharesEntry(null)}
        onOk={() => sharesForm.submit()}
        okText="ذخیره سهم‌ها"
        cancelText="انصراف"
        confirmLoading={updateShares.isPending}
      >
        <Form form={sharesForm} layout="vertical" onFinish={submitShares}>
          <Form.List name="shares">
            {(fields) => (
              <Space direction="vertical" size={10} style={{ width: "100%" }}>
                {fields.map((field) => (
                  <Space key={field.key} align="start" wrap>
                    <Form.Item name={[field.name, "userId"]} rules={[{ required: true, message: "کاربر را انتخاب کنید." }]}>
                      <Select
                        showSearch
                        optionFilterProp="label"
                        placeholder="کارمند"
                        style={{ width: 210 }}
                        options={(employees.data ?? []).map((user) => ({ value: user.id, label: user.fullName }))}
                      />
                    </Form.Item>
                    <Form.Item name={[field.name, "percentage"]} rules={[{ required: true, message: "درصد را وارد کنید." }]}>
                      <InputNumber min={0} max={100} placeholder="درصد" />
                    </Form.Item>
                    <Form.Item name={[field.name, "isCompanyShare"]} valuePropName="checked">
                      <Checkbox>سهم شرکت</Checkbox>
                    </Form.Item>
                  </Space>
                ))}
              </Space>
            )}
          </Form.List>
        </Form>
      </Modal>

      <Modal
        title="تعریف قانون پورسانت"
        open={ruleOpen}
        onCancel={() => setRuleOpen(false)}
        onOk={() => ruleForm.submit()}
        okText="ثبت قانون"
        cancelText="انصراف"
        confirmLoading={createRule.isPending}
      >
        <Form
          form={ruleForm}
          layout="vertical"
          initialValues={{ percentage: 10, payoutType: "OneTime" }}
          onFinish={submitRule}
        >
          <Form.Item
            name="serviceId"
            label="سرویس"
            rules={[{ required: true, message: "سرویس را انتخاب کنید." }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              loading={services.isLoading}
              placeholder="انتخاب سرویس"
              options={(services.data ?? []).map((service) => ({
                value: service.id,
                label: `${service.name} (${service.code})`,
              }))}
            />
          </Form.Item>

          <Space size="middle" align="start" style={{ width: "100%" }}>
            <Form.Item
              name="percentage"
              label="درصد پورسانت"
              rules={[
                { type: "number", min: 0, max: 100, message: "درصد معتبر نیست." },
              ]}
            >
              <InputNumber min={0} max={100} style={{ width: 180 }} />
            </Form.Item>
            <Form.Item name="fixedAmount" label="یا مبلغ ثابت (تومان)">
              <InputNumber
                min={0}
                style={{ width: 200 }}
                formatter={(value) =>
                  `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")
                }
              />
            </Form.Item>
          </Space>

          <Form.Item
            name="payoutType"
            label="نوع پرداخت"
            rules={[{ required: true, message: "نوع پرداخت را انتخاب کنید." }]}
          >
            <Select
              options={[
                { value: "OneTime", label: "یک‌بار" },
                { value: "Monthly", label: "ماهیانه" },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
