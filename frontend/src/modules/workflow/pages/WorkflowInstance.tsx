import { useEffect, useState } from "react";
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
  Select,
  Popconfirm,
  Result,
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
import { useFormSubmissions, useFormTemplateByCode, useFormTemplates } from "@/modules/forms/hooks/useForms";
import { WorkflowSubmissionSummary } from "@/modules/workflow/components/WorkflowSubmissionSummary";
import { WorkflowFormRenderer, validateWorkflowForm } from "@/modules/workflow/components/WorkflowFormRenderer";
import {
  useCancelWorkflowInstance,
  useExecuteWorkflowAction,
  useWorkflowInstance,
  useWorkflowExperts,
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
  const [actionData, setActionData] = useState<Record<string, unknown>>({});
  const [formUploading, setFormUploading] = useState(false);
  const [actionForm] = Form.useForm<ActionForm>();
  const instance = useWorkflowInstance(id);
  const currentTask = instance.data?.tasks.find(
    (task) => task.status === "Pending" && task.stateId === instance.data?.currentStateId,
  );
  const currentFormTemplate = useFormTemplateByCode(currentTask?.formCode ?? undefined);
  const experts = useWorkflowExperts(currentTask?.actionKey === "DecideSiteVisit");
  const previousForms = useFormSubmissions(
    {
      page: 1,
      pageSize: 5,
      entityType: instance.data?.entityType === "Order" ? "Order" : undefined,
      entityId: instance.data?.entityId,
      formTemplateId: currentFormTemplate.data?.id,
    },
    actionOpen && Boolean(currentFormTemplate.data?.id),
  );
  const request = useServiceRequestDetail(
    instance.data?.entityType === "Order" ? instance.data.entityId : undefined,
  );
  const formTemplates = useFormTemplates(false);
  const formSubmissions = useFormSubmissions({
    page: 1,
    pageSize: 20,
    entityType: instance.data?.entityType === "Order" ? "Order" : undefined,
    entityId: instance.data?.entityId,
  });
  const execute = useExecuteWorkflowAction(id);
  const cancel = useCancelWorkflowInstance(id);

  useEffect(() => {
    if (!actionOpen) return;
    const json = previousForms.data?.items?.[0]?.submittedDataJson;
    if (!json) return;
    try {
      setActionData(JSON.parse(json) as Record<string, unknown>);
    } catch {
      // Ignore malformed legacy data; the form starts empty.
    }
  }, [actionOpen, previousForms.data]);

  const openAction = () => {
    setActionData({});
    actionForm.resetFields();
    setActionOpen(true);
  };

  const submitAction = async (transitionId: string) => {
    if (formUploading) { message.info("تا پایان بارگذاری فایل صبر کنید."); return; }
    const comment = actionForm.getFieldValue("comment") as string | undefined;
    const payload = {
      ...actionData,
      ...(comment?.trim() ? { comment: comment.trim() } : {}),
    };
    const error = validateWorkflowForm(currentFormTemplate.data?.schemaJson, payload);
    if (error) { message.error(error); return; }
    try {
      await execute.mutateAsync({
        transitionId,
        actionDataJson: JSON.stringify(payload),
      });
      message.success("اقدام با موفقیت اجرا شد.");
      setActionOpen(false);
    } catch {
      // NotificationProvider handles errors.
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
  const decisionTransitions = currentTask?.availableTransitions ?? data.availableTransitions;

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
        record.assigneeName ?? (record.assignedUserId
          ? "کاربر"
          : record.assignedRoleId
            ? "نقش"
            : record.assignedDepartmentId
              ? "دپارتمان"
              : "بدون مسئول"),
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

        {isRunning && canExecute && data.canExecute !== false && decisionTransitions.length > 0 && (
          <Space wrap>
            <Button
              type="primary"
              icon={<PlayCircleOutlined />}
              disabled={data.availableTransitions.length === 0}
              onClick={openAction}
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

      <Card title="داده‌های فرم" variant="borderless" className="settings-section-card">
        <WorkflowSubmissionSummary
          submissions={formSubmissions.data?.items ?? []}
          templates={formTemplates.data ?? []}
        />
      </Card>

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
        footer={
          <Space wrap>
            <Button onClick={() => setActionOpen(false)}>انصراف</Button>
            {decisionTransitions.map((transition) => (
              <Button
                key={transition.id}
                type="primary"
                loading={execute.isPending}
                onClick={() => void submitAction(transition.id)}
              >
                {transition.name}
              </Button>
            ))}
          </Space>
        }
      >
        {currentTask?.formCode && (
          <WorkflowFormRenderer
            formCode={currentTask.formCode}
            onUploadingChange={setFormUploading}
            values={actionData}
            onDataChange={setActionData}
          />
        )}
        {currentTask?.actionKey === "DecideSiteVisit" && <Form.Item label={actionData.needs_visit === "yes" ? "کارشناس مراجعه حضوری" : "ارجاع به کارشناس دیگر (اختیاری)"}>
          <Select allowClear showSearch optionFilterProp="label" value={actionData.expert_user_id as string | undefined}
            options={(experts.data ?? []).map(user => ({ value: user.id, label: user.fullName }))}
            onChange={value => setActionData(current => ({ ...current, expert_user_id: value }))} />
        </Form.Item>}
        <Form form={actionForm} layout="vertical" style={{ marginTop: currentTask?.formCode ? 16 : 0 }}>
          <Form.Item name="comment" label="توضیح / دلیل">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
