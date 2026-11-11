import { useState } from "react";
import { EyeOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useAuthStore } from "@/modules/auth/store";
import {
  submissionStatusLabels,
  useFormSubmissionHistory,
  useFormSubmissions,
  useReviewFormSubmission,
} from "@/modules/forms/hooks/useForms";
import type {
  FormEntityType,
  FormSubmission,
  FormSubmissionStatus,
} from "@/modules/forms/types";

const entityLabels: Record<FormEntityType, string> = {
  None: "بدون موجودیت",
  Customer: "مشتری",
  Order: "سفارش",
  Service: "سرویس",
  DataCenter: "دیتاسنتر",
  Requirement: "نیازمندی",
  Feasibility: "امکان‌سنجی",
  Ticket: "تیکت",
};

const statusColors: Record<FormSubmissionStatus, string> = {
  Draft: "default",
  Submitted: "orange",
  Approved: "green",
  Rejected: "red",
};

interface ReviewForm {
  note?: string;
}

export function FormSubmissions() {
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canReview = hasPermission("Forms.Submission.Review");
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<FormSubmissionStatus | undefined>();
  const [selected, setSelected] = useState<FormSubmission | null>(null);
  const [reviewApprove, setReviewApprove] = useState<boolean | null>(null);
  const [reviewForm] = Form.useForm<ReviewForm>();
  const submissions = useFormSubmissions({
    page,
    pageSize: 20,
    status,
  });
  const history = useFormSubmissionHistory(selected?.id);
  const review = useReviewFormSubmission();

  const submitReview = async (values: ReviewForm) => {
    if (!selected || reviewApprove === null) return;
    try {
      await review.mutateAsync({
        submissionId: selected.id,
        approve: reviewApprove,
        note: values.note?.trim() || undefined,
      });
      message.success(reviewApprove ? "پاسخ تأیید شد." : "پاسخ رد شد.");
      setReviewApprove(null);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<FormSubmission> = [
    {
      title: "فرم",
      key: "form",
      width: 220,
      render: (_value, record) => (
        <div>
          <Typography.Text strong>{record.formTitle || "-"}</Typography.Text>
          <br />
          <Typography.Text type="secondary">نسخه {record.formVersion}</Typography.Text>
        </div>
      ),
    },
    {
      title: "موجودیت",
      key: "entity",
      width: 200,
      render: (_value, record) => (
        <div>
          <Tag color="blue" bordered={false}>
            {entityLabels[record.entityType] ?? record.entityType}
          </Tag>
          <Typography.Text type="secondary" dir="ltr" style={{ display: "block" }}>
            {record.entityId || "-"}
          </Typography.Text>
        </div>
      ),
    },
    {
      title: "مشتری",
      dataIndex: "customerName",
      width: 170,
      render: (value: string | null) => value || "-",
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      width: 130,
      render: (value: FormSubmissionStatus) => (
        <Tag color={statusColors[value]} bordered={false}>
          {submissionStatusLabels[value]}
        </Tag>
      ),
    },
    {
      title: "تاریخ ارسال",
      dataIndex: "submittedAtJalali",
      width: 125,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 180,
      fixed: "left",
      render: (_value, record) => (
        <Space size="small">
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => {
              setSelected(record);
              reviewForm.resetFields();
              setReviewApprove(null);
            }}
          >
            مشاهده
          </Button>
          {canReview && record.status === "Submitted" && (
            <Button
              type="link"
              onClick={() => {
                setSelected(record);
                setReviewApprove(true);
              }}
            >
              بررسی
            </Button>
          )}
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>پاسخ‌های فرم</Typography.Title>
          <Typography.Text type="secondary">
            مشاهده و بررسی پاسخ‌های ثبت‌شده
          </Typography.Text>
        </div>
      </div>

      <div className="table-toolbar">
        <Select
          allowClear
          value={status}
          onChange={(value) => {
            setStatus(value);
            setPage(1);
          }}
          placeholder="وضعیت پاسخ"
          style={{ minWidth: 160 }}
          options={Object.entries(submissionStatusLabels).map(([value, label]) => ({
            value,
            label,
          }))}
        />
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<FormSubmission>
          rowKey="id"
          columns={columns}
          dataSource={submissions.data?.items ?? []}
          loading={submissions.isLoading || submissions.isFetching}
          scroll={{ x: 1030 }}
          className="module-table"
          locale={{ emptyText: "پاسخی ثبت نشده است." }}
          pagination={{
            current: submissions.data?.page ?? page,
            pageSize: submissions.data?.pageSize ?? 20,
            total: submissions.data?.totalCount ?? 0,
            showTotal: (total) => `مجموع ${total} پاسخ`,
            onChange: setPage,
          }}
        />
      </Card>

      <Modal
        title="جزئیات پاسخ فرم"
        open={selected !== null}
        onCancel={() => {
          setSelected(null);
          setReviewApprove(null);
        }}
        footer={null}
        width={720}
        forceRender
      >
        {selected && (
          <Space direction="vertical" size={12} style={{ width: "100%" }}>
            <Space wrap>
              <Tag color={statusColors[selected.status]}>
                {submissionStatusLabels[selected.status]}
              </Tag>
              <Tag>نسخه {selected.formVersion}</Tag>
              <Typography.Text type="secondary">{selected.submittedAtJalali}</Typography.Text>
            </Space>
            <pre
              dir="ltr"
              style={{
                margin: 0,
                padding: 12,
                background: "#fafafa",
                borderRadius: 8,
                whiteSpace: "pre-wrap",
                direction: "ltr",
              }}
            >
              {prettyJson(selected.submittedDataJson)}
            </pre>
            <Typography.Title level={5} style={{ margin: 0 }}>
              تاریخچه ویرایش
            </Typography.Title>
            {history.data?.length ? (
              <Space direction="vertical" size={4}>
                {history.data.map((entry) => (
                  <Typography.Text key={entry.id} type="secondary">
                    {entry.action === "Created" ? "ایجاد" : "ویرایش"} — {entry.modifiedAtJalali}
                  </Typography.Text>
                ))}
              </Space>
            ) : (
              <Typography.Text type="secondary">تاریخچه‌ای ثبت نشده است.</Typography.Text>
            )}
            {canReview && selected.status === "Submitted" && reviewApprove === null && (
              <Space>
                <Button type="primary" onClick={() => setReviewApprove(true)}>
                  تأیید
                </Button>
                <Button danger onClick={() => setReviewApprove(false)}>
                  رد
                </Button>
              </Space>
            )}
            {selected.reviewNote && (
              <Typography.Paragraph type="secondary">
                یادداشت: {selected.reviewNote}
              </Typography.Paragraph>
            )}
          </Space>
        )}
        <Form
          form={reviewForm}
          layout="vertical"
          onFinish={submitReview}
          style={{ display: reviewApprove === null ? "none" : "block" }}
        >
          <Form.Item name="note" label="یادداشت بررسی">
            <Input.TextArea rows={3} />
          </Form.Item>
          <Space>
            <Button
              type="primary"
              danger={!reviewApprove}
              htmlType="submit"
              loading={review.isPending}
            >
              {reviewApprove ? "ثبت تأیید" : "ثبت رد"}
            </Button>
            <Button onClick={() => setReviewApprove(null)}>انصراف</Button>
          </Space>
        </Form>
      </Modal>
    </Space>
  );
}

function prettyJson(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
