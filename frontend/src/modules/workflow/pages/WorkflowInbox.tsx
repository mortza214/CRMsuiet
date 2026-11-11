import { useEffect, useMemo, useState } from "react";
import {
  ApartmentOutlined,
  EyeOutlined,
  PlayCircleOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Card,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { useFormSubmissions } from "@/modules/forms/hooks/useForms";
import { useDepartments } from "@/modules/tickets/hooks/useTickets";
import { useDepartments as useSettingsDepartments } from "@/modules/settings/hooks/useSettings";
import { WorkflowFormRenderer } from "../components/WorkflowFormRenderer";
import {
  useExecuteWorkflowAction,
  useMyWorkflowTasks,
  useWorkflowExperts,
} from "@/modules/workflow/hooks/useWorkflow";
import type {
  WorkflowTask,
  WorkflowTaskStatus,
} from "@/modules/workflow/types";

const taskStatusMeta: Record<WorkflowTaskStatus, { label: string; color: string }> = {
  Pending: { label: "در انتظار", color: "orange" },
  InProgress: { label: "در حال انجام", color: "blue" },
  Done: { label: "انجام شده", color: "green" },
  Cancelled: { label: "لغو شده", color: "default" },
};

interface ActionForm {
  comment?: string;
  payloadJson?: string;
}

export function WorkflowInbox() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const canReadDefinitions = useAuthStore((state) =>
    state.hasPermission("Workflow.Definition.Read"),
  );
  const canReadDepartments = useAuthStore((state) =>
    state.hasPermission("Identity.Department.Read"),
  );
  const [status, setStatus] = useState<WorkflowTaskStatus | undefined>();
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const [selectedTask, setSelectedTask] = useState<WorkflowTask | null>(null);
  const [selectedTaskAction, setSelectedTaskAction] = useState<{
    actionKey: string;
    formCode?: string;
  } | null>(null);
  const [actionOpen, setActionOpen] = useState(false);
  const [actionData, setActionData] = useState<Record<string, unknown>>({});
  const [reviewEditable, setReviewEditable] = useState(false);
  const [actionForm] = Form.useForm<ActionForm>();
  const tasks = useMyWorkflowTasks(status, departmentId);
  const departments = useDepartments(canReadDepartments);
  const settingsDepartments = useSettingsDepartments(
    canReadDepartments && selectedTaskAction?.actionKey === "DecideSiteVisit",
  );
  const expertsQuery = useWorkflowExperts(
    selectedTaskAction?.actionKey === "DecideSiteVisit",
  );
  const technicalDepartment = settingsDepartments.data?.find(
    (department) => department.name === "فنی",
  );
  const technicalExperts = (expertsQuery.data ?? []).filter((user) =>
    technicalDepartment
      ? user.departmentIds?.includes(technicalDepartment.id)
      : false,
  );
  const expertCandidates =
    technicalExperts.length > 0 ? technicalExperts : (expertsQuery.data ?? []);
  const previousForms = useFormSubmissions(
    {
      page: 1,
      pageSize: 5,
      entityType: "Order",
      entityId: selectedTask?.entityId,
    },
    Boolean(selectedTask && selectedTaskAction?.formCode) ||
      selectedTaskAction?.actionKey === "ReviewFeasibility",
  );
  const previousFormData = useMemo(() => {
    const json = previousForms.data?.items?.[0]?.submittedDataJson;
    if (!json) return {};
    try {
      return JSON.parse(json) as Record<string, unknown>;
    } catch {
      return {};
    }
  }, [previousForms.data]);

  useEffect(() => {
    if (
      selectedTaskAction?.formCode &&
      selectedTaskAction.actionKey !== "ReviewFeasibility" &&
      Object.keys(previousFormData).length > 0
    ) {
      setActionData(previousFormData);
      setReviewEditable(true);
    }
  }, [previousFormData, selectedTaskAction]);

  const execute = useExecuteWorkflowAction(selectedTask?.instanceId);

  const openAction = (task: WorkflowTask) => {
    setSelectedTask(task);
    setSelectedTaskAction({
      actionKey: task.actionKey || "None",
      formCode: task.formCode || undefined,
    });
    setActionData({});
    setReviewEditable(false);
    actionForm.resetFields();
    setActionOpen(true);
  };

  const submitAction = async (values: ActionForm, transitionId?: string) => {
    const actionKey = selectedTaskAction?.actionKey ?? "None";
    const selectedTransition = selectedTask?.availableTransitions.find(
      (transition) => transition.id === transitionId,
    );
    const normalizedData = { ...actionData };
    if (actionKey === "CustomerAction" && !normalizedData.decision) {
      normalizedData.decision = selectedTransition?.name.includes("انصراف")
        ? "cancel"
        : "approve";
    }

    const actionPayload = actionKey === "FillForm" || selectedTaskAction?.formCode
      ? normalizedData
      : {
          ...normalizedData,
          comment: values.comment?.trim() || undefined,
        };

    try {
      await execute.mutateAsync({
        transitionId,
        actionDataJson: JSON.stringify(actionPayload),
      });
      message.success("اکشن با موفقیت اجرا شد.");
      setActionOpen(false);
    } catch {
      // NotificationProvider handles errors.
    }
  };

  const columns: TableColumnsType<WorkflowTask> = [
    {
      title: "گردش کار",
      key: "definition",
      width: 200,
      render: (_value, record) => (
        <div>
          <Typography.Text strong>{record.definitionName}</Typography.Text>
          <br />
          <Typography.Text type="secondary" dir="ltr">
            {record.definitionCode}
          </Typography.Text>
        </div>
      ),
    },
    {
      title: "موجودیت",
      key: "entity",
      width: 230,
      render: (_value, record) => (
        <div>
          <Tag color="blue" bordered={false}>
            {record.entityType === "Order"
              ? "سفارش"
              : record.entityType === "Ticket"
                ? "تیکت"
                : record.entityType}
          </Tag>
          <br />
          <Typography.Text strong>
            {record.entityDisplayName || "-"}
          </Typography.Text>
          <br />
          <Typography.Text type="secondary" dir="ltr">
            {record.entityReference || record.entityId}
          </Typography.Text>
        </div>
      ),
    },
    {
      title: "وضعیت فعلی",
      dataIndex: "currentStateName",
      key: "currentStateName",
      width: 160,
    },
    {
      title: "وضعیت تسک",
      dataIndex: "status",
      key: "status",
      width: 130,
      render: (value: WorkflowTaskStatus) => {
        const meta = taskStatusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "تاریخ ایجاد",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 125,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 190,
      fixed: "left",
      render: (_value, record) => (
        <Space size="small" wrap>
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/workflow/instances/${record.instanceId}`)}
          >
            مشاهده
          </Button>
          {record.status !== "Cancelled" && record.status !== "Done" && (
            <Button
              type="link"
              icon={<PlayCircleOutlined />}
              disabled={record.availableTransitions.length === 0}
              onClick={() => openAction(record)}
            >
              اقدام
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
          <Typography.Title level={2}>کارتابل گردش کار</Typography.Title>
          <Typography.Text type="secondary">
            تسک‌های در انتظار اقدام شما
          </Typography.Text>
        </div>
        <Space wrap>
          <span className="dashboard-badge">{tasks.data?.totalCount ?? 0} تسک</span>
          {canReadDefinitions && (
            <Button
              icon={<ApartmentOutlined />}
              onClick={() => navigate("/workflow/definitions")}
            >
              تعریف گردش کار
            </Button>
          )}
        </Space>
      </div>

      <div className="table-toolbar">
        <Space wrap>
          <Select
            allowClear
            value={status}
            onChange={setStatus}
            placeholder="وضعیت تسک"
            style={{ minWidth: 150 }}
            options={Object.entries(taskStatusMeta).map(([value, meta]) => ({
              value,
              label: meta.label,
            }))}
          />
          {canReadDepartments && (
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              value={departmentId}
              onChange={setDepartmentId}
              placeholder="دپارتمان"
              style={{ minWidth: 170 }}
              loading={departments.isLoading}
              options={(departments.data ?? []).map((department) => ({
                value: department.id,
                label: department.name,
              }))}
            />
          )}
        </Space>
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<WorkflowTask>
          rowKey="id"
          columns={columns}
          dataSource={tasks.data?.items ?? []}
          loading={tasks.isLoading || tasks.isFetching}
          scroll={{ x: 1050 }}
          className="module-table"
          locale={{ emptyText: "تسکی در کارتابل شما نیست." }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
        />
      </Card>

      <Modal
        title={`اقدام روی: ${selectedTask?.definitionName ?? ""}`}
        open={actionOpen}
        onCancel={() => setActionOpen(false)}
        confirmLoading={execute.isPending}
        footer={
          <Space>
            <Button onClick={() => setActionOpen(false)}>انصراف</Button>
            {(selectedTask?.availableTransitions ?? []).map((transition) => (
              <Button
                key={transition.id}
                type="primary"
                loading={execute.isPending}
                onClick={() =>
                  actionForm.validateFields().then((values) =>
                    submitAction(values, transition.id),
                  )
                }
              >
                {selectedTask?.availableTransitions.length === 1
                  ? "ثبت و ارجاع"
                  : transition.name}
              </Button>
            ))}
          </Space>
        }
      >
        <Form form={actionForm} layout="vertical" onFinish={submitAction}>

          {selectedTaskAction?.actionKey === "DecideSiteVisit" && (
            <>
              <WorkflowFormRenderer
                formCode={selectedTaskAction?.formCode}
                values={reviewEditable ? actionData : previousFormData}
                readOnly={!reviewEditable}
                onDataChange={setActionData}
              />
              {!reviewEditable && (
                <Button
                  style={{ marginTop: 12 }}
                  onClick={() => {
                    setActionData(previousFormData);
                    setReviewEditable(true);
                  }}
                >
                  ویرایش فرم قبلی
                </Button>
              )}
              {actionData.needs_visit === "yes" && (
                <Form.Item label="کارشناس فنی" required>
                  <Select
                    showSearch
                    optionFilterProp="label"
                    placeholder="انتخاب کارشناس فنی"
                    value={actionData.expert_user_id as string | undefined}
                    onChange={(value) =>
                      setActionData((current) => ({
                        ...current,
                        expert_user_id: value,
                      }))
                    }
                    options={expertCandidates.map((user) => ({
                      value: user.id,
                      label: user.fullName,
                    }))}
                  />
                </Form.Item>
              )}
            </>
          )}

          {selectedTaskAction?.actionKey === "ReviewFeasibility" && (
            <>
              <Alert
                type="info"
                showIcon
                message="فرم کارشناس فنی را بررسی، ویرایش و تأیید کنید."
                style={{ marginBottom: 12 }}
              />
              <WorkflowFormRenderer
                formCode={selectedTaskAction?.formCode}
                onDataChange={setActionData}
              />
            </>
          )}

          {selectedTaskAction?.formCode &&
            selectedTaskAction.actionKey !== "ReviewFeasibility" &&
            selectedTaskAction.actionKey !== "DecideSiteVisit" && (
              <WorkflowFormRenderer
                formCode={selectedTaskAction.formCode}
                values={reviewEditable ? actionData : previousFormData}
                onDataChange={setActionData}
              />
            )}

          {selectedTaskAction?.actionKey === "SelectEquipment" && (
            <Form.Item label="روش تأمین تجهیزات" required>
              <Select
                value={actionData.mode as string | undefined}
                onChange={(mode) => setActionData((current) => ({ ...current, mode }))}
                options={[
                  { value: "Purchase", label: "خرید تجهیزات" },
                  { value: "Consigned", label: "تجهیزات امانی" },
                  { value: "Existing", label: "تجهیزات موجود" },
                ]}
              />
            </Form.Item>
          )}

          {selectedTaskAction?.actionKey === "CustomerAction" && (
            <Form.Item label="پاسخ مشتری" required>
              <Select
                value={actionData.decision as string | undefined}
                onChange={(decision) => setActionData((current) => ({ ...current, decision }))}
                options={[
                  { value: "approve", label: "تأیید و ادامه" },
                  { value: "cancel", label: "انصراف" },
                ]}
              />
            </Form.Item>
          )}

          {selectedTaskAction?.actionKey === "PayInvoice" && (
            <>
              <Form.Item label="روش پرداخت">
                <Select
                  value={(actionData.paymentMode as string | undefined) ?? "Gateway"}
                  onChange={(paymentMode) =>
                    setActionData((current) => ({ ...current, paymentMode }))
                  }
                  options={[
                    { value: "Gateway", label: "درگاه پرداخت" },
                    { value: "ManualDeposit", label: "فیش واریزی" },
                  ]}
                />
              </Form.Item>
              <Form.Item>
                <Checkbox
                  checked={Boolean(actionData.paid)}
                  onChange={(event) =>
                    setActionData((current) => ({ ...current, paid: event.target.checked }))
                  }
                >
                  پرداخت انجام شد
                </Checkbox>
              </Form.Item>
            </>
          )}

          {selectedTaskAction?.actionKey === "VerifyPayment" && (
            <Form.Item label="تأیید مالی">
              <Checkbox
                checked={Boolean(actionData.approved)}
                onChange={(event) =>
                  setActionData((current) => ({ ...current, approved: event.target.checked }))
                }
              >
                فیش پرداخت تأیید شد
              </Checkbox>
            </Form.Item>
          )}

          {selectedTaskAction?.actionKey === "SignContract" && (
            <Alert
              type="info"
              showIcon
              message="قرارداد در کارتابل مشتری قرار گرفته است."
              description="پس از امضای قرارداد، گردش کار تکمیل و سرویس فعال می‌شود."
            />
          )}

          {selectedTaskAction?.actionKey === "IssueProforma" && (
            <>
              <Form.Item label="مبلغ پیش‌فاکتور (اختیاری؛ پیش‌فرض Snapshot سرویس)">
                <InputNumber
                  style={{ width: "100%" }}
                  value={typeof actionData.amount === "number" ? actionData.amount : undefined}
                  onChange={(value) => setActionData((current) => ({ ...current, amount: value }))}
                />
              </Form.Item>
              <Form.Item label="روش پرداخت">
                <Select
                  value={(actionData.paymentMode as string | undefined) ?? "Gateway"}
                  onChange={(value) => setActionData((current) => ({ ...current, paymentMode: value }))}
                  options={[
                    { value: "Gateway", label: "درگاه پرداخت" },
                    { value: "ManualDeposit", label: "واریز / فیش" },
                  ]}
                />
              </Form.Item>
              <Form.Item label="نوع فاکتور">
                <Select
                  value={(actionData.invoiceType as string | undefined) ?? "Proforma"}
                  onChange={(value) => setActionData((current) => ({ ...current, invoiceType: value }))}
                  options={[
                    { value: "Official", label: "رسمی" },
                    { value: "Unofficial", label: "غیررسمی" },
                  ]}
                />
              </Form.Item>
              <Form.Item label="الزام به پرداخت">
                <Switch
                  checked={actionData.requiresPayment !== false}
                  onChange={(value) => setActionData((current) => ({ ...current, requiresPayment: value }))}
                />
              </Form.Item>
            </>
          )}

          {selectedTaskAction?.actionKey === "RequestPayment" && (
            <>
              <Alert
                type="info"
                showIcon
                message="برای ادامه، پرداخت فاکتور باید تأیید شده باشد."
                style={{ marginBottom: 12 }}
              />
              <Form.Item label="تأیید پیش‌فاکتور / پرداخت">
                <Space direction="vertical">
                  <Checkbox
                    checked={Boolean(actionData.approved)}
                    onChange={(event) => setActionData((current) => ({ ...current, approved: event.target.checked }))}
                  >
                    پیش‌فاکتور تأیید شد
                  </Checkbox>
                  <Checkbox
                    checked={Boolean(actionData.paid)}
                    onChange={(event) => setActionData((current) => ({ ...current, paid: event.target.checked }))}
                  >
                    پرداخت تأیید شد
                  </Checkbox>
                </Space>
              </Form.Item>
              <Form.Item label="شناسه فاکتور (اختیاری)">
                <Input
                  dir="ltr"
                  value={String(actionData.invoiceId ?? "")}
                  onChange={(event) => setActionData((current) => ({ ...current, invoiceId: event.target.value }))}
                />
              </Form.Item>
            </>
          )}

          {selectedTaskAction?.actionKey === "ActivateService" && (
            <Alert
              type="success"
              showIcon
              message="فعال‌سازی سرویس"
              description="فعال‌سازی فقط با فاکتور نهایی پرداخت‌شده انجام می‌شود."
            />
          )}

          {!selectedTaskAction?.formCode && (
            <Form.Item name="comment" label="توضیح / دلیل">
              <Input.TextArea rows={3} placeholder="توضیح اقدام (در صورت نیاز)" />
            </Form.Item>
          )}
          {!selectedTaskAction?.formCode &&
            selectedTaskAction?.actionKey !== "None" ? (
            <Form.Item name="payloadJson" label="Payload (JSON، اختیاری)">
              <Input.TextArea rows={3} placeholder='{"key":"value"}' dir="ltr" />
            </Form.Item>
          ) : null}
        </Form>
      </Modal>
    </Space>
  );
}
