import { useState } from "react";
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
  transitionId: string;
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
  const execute = useExecuteWorkflowAction(selectedTask?.instanceId);

  const openAction = (task: WorkflowTask) => {
    setSelectedTask(task);
    setSelectedTaskAction({
      actionKey: task.actionKey || "None",
      formCode: task.formCode || undefined,
    });
    setActionData({});
    actionForm.resetFields();
    setActionOpen(true);
  };

  const submitAction = async (values: ActionForm) => {
    const actionKey = selectedTaskAction?.actionKey ?? "None";
    const actionPayload = actionKey === "FillForm"
      ? actionData
      : {
          ...actionData,
          comment: values.comment?.trim() || undefined,
        };

    try {
      await execute.mutateAsync({
        transitionId: actionKey === "FillForm" || actionKey === "None"
          ? undefined
          : values.transitionId,
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
        onOk={() => actionForm.submit()}
        okText={selectedTaskAction?.actionKey === "FillForm" ? "ثبت و ارجاع" : "ثبت اقدام"}
        cancelText="انصراف"
        confirmLoading={execute.isPending}
      >
        <Form form={actionForm} layout="vertical" onFinish={submitAction}>
          {selectedTaskAction?.actionKey !== "FillForm" &&
            selectedTaskAction?.actionKey !== "None" && (
            <Form.Item
              name="transitionId"
              label="گذار"
              rules={[{ required: true, message: "گذار را انتخاب کنید." }]}
            >
              <Select
                options={(selectedTask?.availableTransitions ?? []).map((transition) => ({
                  value: transition.id,
                  label: transition.name,
                }))}
              />
            </Form.Item>
          )}
          {selectedTaskAction?.actionKey === "DecideSiteVisit" && (
            <>
              <WorkflowFormRenderer
                formCode={selectedTaskAction?.formCode}
                onDataChange={setActionData}
              />
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

          {selectedTaskAction?.actionKey === "FillForm" && (
            <WorkflowFormRenderer
              formCode={selectedTaskAction?.formCode}
              onDataChange={setActionData}
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

          {selectedTaskAction?.actionKey !== "FillForm" && (
            <Form.Item name="comment" label="توضیح / دلیل">
              <Input.TextArea rows={3} placeholder="توضیح اقدام (در صورت نیاز)" />
            </Form.Item>
          )}
          {selectedTaskAction?.actionKey !== "FillForm" &&
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
