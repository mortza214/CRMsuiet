import { useState } from "react";
import {
  ArrowRightOutlined,
  CloseCircleOutlined,
  PlayCircleOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Descriptions,
  Form,
  Input,
  Modal,
  Popconfirm,
  Result,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Timeline,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { useServiceRequestDetail } from "@/modules/orders/hooks/useOrders";
import {
  useCancelWorkflowInstance,
  useExecuteWorkflowTransition,
  useWorkflowInstance,
} from "@/modules/workflow/hooks/useWorkflow";
import type {
  WorkflowTask,
  WorkflowTaskStatus,
} from "@/modules/workflow/types";

const taskStatusLabels: Record<WorkflowTaskStatus, string> = {
  Pending: "در انتظار",
  InProgress: "در حال انجام",
  Done: "انجام شده",
  Cancelled: "لغو شده",
};

interface ActionForm {
  transitionId: string;
  comment?: string;
  payloadJson?: string;
}

const attributeLabels: Record<string, string> = {
  bandwidth_mb: "پهنای باند (مگ)",
  substrate: "نوع بستر",
  building_height: "ارتفاع ساختمان / طبقه",
  geo_coordinates: "مختصات جغرافیایی",
  total_height: "ارتفاع کل",
  existing_equipment_type: "نوع تجهیزات موجود",
  existing_equipment_count: "تعداد تجهیزات موجود",
  has_cable: "کابل دارد؟",
  extra_ip_count: "تعداد IP اضافی",
};

function parseAttributes(attributesJson?: string | null) {
  if (!attributesJson) return [];
  try {
    const values = JSON.parse(attributesJson) as Record<string, unknown>;
    return Object.entries(values)
      .filter(([, value]) => value !== null && value !== undefined && value !== "")
      .map(([key, value]) => ({
        key,
        label: attributeLabels[key] ?? key,
        value:
          key === "substrate"
            ? ({ wireless: "وایرلس", fiber: "فیبر", cable: "کابل" } as Record<string, string>)[
                String(value)
              ] ?? String(value)
            : key === "has_cable"
              ? ({ yes: "بله", no: "خیر" } as Record<string, string>)[String(value)] ??
                String(value)
              : String(value),
      }));
  } catch {
    return [];
  }
}

export function WorkflowInstance() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canExecute = hasPermission("Workflow.Instance.Execute");
  const canCancel = hasPermission("Workflow.Instance.Cancel");
  const [actionOpen, setActionOpen] = useState(false);
  const [actionForm] = Form.useForm<ActionForm>();
  const instance = useWorkflowInstance(id);
  const request = useServiceRequestDetail(
    instance.data?.entityType === "Order" ? instance.data.entityId : undefined,
  );
  const execute = useExecuteWorkflowTransition(id);
  const cancel = useCancelWorkflowInstance(id);

  const submitAction = async (values: ActionForm) => {
    try {
      await execute.mutateAsync({
        transitionId: values.transitionId,
        comment: values.comment?.trim() || null,
        payloadJson: values.payloadJson?.trim() || null,
        idempotencyKey: crypto.randomUUID(),
      });
      message.success("گذار با موفقیت اجرا شد.");
      setActionOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handleCancel = async () => {
    try {
      await cancel.mutateAsync("لغو دستی توسط کاربر");
      message.success("گردش کار لغو شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  if (instance.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (instance.isError || !instance.data) {
    return (
      <Result
        status="404"
        title="نمونه گردش کار یافت نشد"
        extra={
          <Button type="primary" onClick={() => navigate("/workflow/inbox")}>
            بازگشت به کارتابل
          </Button>
        }
      />
    );
  }

  const data = instance.data;
  const isRunning = data.status === "Running";

  const taskColumns: TableColumnsType<WorkflowTask> = [
    {
      title: "وضعیت",
      dataIndex: "stateName",
      key: "stateName",
    },
    {
      title: "وضعیت تسک",
      dataIndex: "status",
      key: "status",
      width: 130,
      render: (value: WorkflowTaskStatus) => (
        <Tag bordered={false}>{taskStatusLabels[value] ?? value}</Tag>
      ),
    },
    {
      title: "مسئول",
      key: "assignee",
      width: 180,
      render: (_value, record) =>
        record.assignedUserId
          ? "کاربر"
          : record.assignedRoleId
            ? "نقش"
            : record.assignedDepartmentId
              ? "دپارتمان"
              : "بدون مسئول",
    },
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 125,
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/workflow/inbox")}
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} style={{ margin: 0 }}>
                {data.definitionName}
              </Typography.Title>
              <Tag color={isRunning ? "blue" : data.status === "Completed" ? "green" : "red"}>
                {data.status === "Running"
                  ? "در جریان"
                  : data.status === "Completed"
                    ? "تکمیل شده"
                    : "لغو شده"}
              </Tag>
              <Tag color="purple">نسخه {data.definitionVersion}</Tag>
            </Space>
            <Typography.Text type="secondary">
              {data.definitionCode} — {data.entityType}
            </Typography.Text>
          </div>
        </Space>

        {isRunning && canExecute && (
          <Space wrap>
            <Button
              type="primary"
              icon={<PlayCircleOutlined />}
              disabled={data.availableTransitions.length === 0}
              onClick={() => {
                actionForm.resetFields();
                setActionOpen(true);
              }}
            >
              اقدام
            </Button>
            {canCancel && (
              <Popconfirm
                title="گردش کار لغو شود؟"
                okText="لغو گردش کار"
                cancelText="انصراف"
                onConfirm={() => void handleCancel()}
              >
                <Button danger icon={<CloseCircleOutlined />} loading={cancel.isPending}>
                  لغو
                </Button>
              </Popconfirm>
            )}
          </Space>
        )}
      </div>

      {request.data && (
        <Card
          title="مشخصات درخواست سرویس"
          variant="borderless"
          className="settings-section-card"
          extra={
            <Button type="link" onClick={() => navigate(`/orders/${request.data!.orderId}`)}>
              مشاهده سفارش
            </Button>
          }
        >
          <Descriptions bordered size="small" column={{ xs: 1, md: 2 }}>
            <Descriptions.Item label="مشتری">
              {request.data.customerName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="موبایل مشتری">
              <span dir="ltr">{request.data.customerMobile || "-"}</span>
            </Descriptions.Item>
            <Descriptions.Item label="شماره سفارش">
              <span dir="ltr">{request.data.orderNumber}</span>
            </Descriptions.Item>
            <Descriptions.Item label="سرویس">
              {request.data.serviceName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="مرحله فعلی">
              <Tag color="blue">{request.data.workflowStateName || "-"}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="مبلغ">
              {request.data.effectivePrice.toLocaleString("fa-IR")} ریال
            </Descriptions.Item>
            {parseAttributes(request.data.attributesJson).map((attribute) => (
              <Descriptions.Item key={attribute.key} label={attribute.label}>
                {attribute.value}
              </Descriptions.Item>
            ))}
          </Descriptions>
        </Card>
      )}

      <Card variant="borderless" className="settings-section-card">
        <Descriptions bordered size="small" column={1}>
          <Descriptions.Item label="وضعیت فعلی">
            {data.currentStateName}
          </Descriptions.Item>
          <Descriptions.Item label="شروع">
            {data.startedAtJalali}
          </Descriptions.Item>
          <Descriptions.Item label="پایان">
            {data.completedAtJalali || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="شناسه موجودیت">
            <span dir="ltr">{data.entityId}</span>
          </Descriptions.Item>
          <Descriptions.Item label="Correlation">
            <span dir="ltr">{data.correlationId || "-"}</span>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="تسک‌ها" variant="borderless" className="settings-section-card">
        <Table<WorkflowTask>
          rowKey="id"
          size="small"
          columns={taskColumns}
          dataSource={data.tasks}
          pagination={false}
          locale={{ emptyText: "تسکی ثبت نشده است." }}
        />
      </Card>

      <Card title="تاریخچه وضعیت" variant="borderless" className="settings-section-card">
        <Timeline
          items={data.history.map((history) => ({
            color: history.toStateName === data.currentStateName ? "blue" : "green",
            children: (
              <div>
                <Space size="small" wrap>
                  {history.fromStateName && (
                    <Typography.Text type="secondary">
                      {history.fromStateName} ←
                    </Typography.Text>
                  )}
                  <Typography.Text strong>{history.toStateName}</Typography.Text>
                  <Typography.Text type="secondary">
                    {history.actionAtJalali}
                  </Typography.Text>
                </Space>
                {history.comment && (
                  <Typography.Paragraph type="secondary" style={{ margin: "4px 0 0" }}>
                    {history.comment}
                  </Typography.Paragraph>
                )}
              </div>
            ),
          }))}
        />
      </Card>

      <Modal
        title="اقدام در گردش کار"
        open={actionOpen}
        onCancel={() => setActionOpen(false)}
        onOk={() => actionForm.submit()}
        okText="ثبت اقدام"
        cancelText="انصراف"
        confirmLoading={execute.isPending}
      >
        <Form form={actionForm} layout="vertical" onFinish={submitAction}>
          <Form.Item
            name="transitionId"
            label="گذار"
            rules={[{ required: true, message: "گذار را انتخاب کنید." }]}
          >
            <Select
              options={data.availableTransitions.map((transition) => ({
                value: transition.id,
                label: transition.name,
              }))}
            />
          </Form.Item>
          <Form.Item name="comment" label="توضیح / دلیل">
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item name="payloadJson" label="Payload (JSON، اختیاری)">
            <Input.TextArea rows={3} dir="ltr" placeholder='{"key":"value"}' />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
