import { useEffect, useMemo, useState } from "react";
import {
  ArrowRightOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  RocketOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Spin,
  Switch,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
  type Edge,
  type Node,
  type NodeMouseHandler,
  type OnNodeDrag,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { useAuthStore } from "@/modules/auth/store";
import { useCustomerOwners } from "@/modules/customers/hooks/useCustomers";
import { useFormTemplates } from "@/modules/forms/hooks/useForms";
import { useRoles } from "@/modules/settings/hooks/useSettings";
import { useDepartments } from "@/modules/tickets/hooks/useTickets";
import {
  useAddWorkflowState,
  useAddWorkflowTransition,
  usePublishWorkflowDefinition,
  useReopenWorkflowDefinition,
  useRemoveWorkflowState,
  useRemoveWorkflowTransition,
  useUpdateWorkflowDraft,
  useUpdateWorkflowState,
  useWorkflowDefinition,
} from "@/modules/workflow/hooks/useWorkflow";
import type {
  WorkflowAssigneeType,
  WorkflowGuardType,
  WorkflowState,
  WorkflowStateType,
  WorkflowTransition,
} from "@/modules/workflow/types";

const stateTypeLabels: Record<WorkflowStateType, string> = {
  Start: "شروع",
  Normal: "عادی",
  End: "پایان",
};

const assigneeLabels: Record<WorkflowAssigneeType, string> = {
  User: "کاربر",
  Role: "نقش",
  Department: "دپارتمان",
  CustomerOwner: "مالک مشتری",
  RequestSalesOwner: "ثبت‌کننده / تأییدکننده اولیه فروش",
  Customer: "مشتری",
  None: "بدون مسئول",
};

const actionKeyLabels: Record<string, string> = {
  None: "بدون اکشن",
  FillForm: "تکمیل فرم",
  ReviewFeasibility: "بررسی امکان‌سنجی",
  DecideSiteVisit: "تصمیم مراجعه حضوری",
  SelectEquipment: "انتخاب تجهیزات",
  PurchaseEquipment: "خرید تجهیزات",
  IssueProforma: "صدور پیش‌فاکتور",
  CustomerAction: "اقدام مشتری",
  IssueFinalInvoice: "صدور فاکتور نهایی",
  PayInvoice: "پرداخت فاکتور",
  VerifyPayment: "تأیید پرداخت",
  SignContract: "امضای قرارداد",
  ActivateService: "فعال‌سازی سرویس",
  RequestPayment: "درخواست پرداخت",
};

const guardLabels: Record<WorkflowGuardType, string> = {
  None: "بدون Guard",
  PermissionOnly: "فقط Permission",
  Plugin: "Plugin",
};

interface StateForm {
  code: string;
  name: string;
  stateType: WorkflowStateType;
  order: number;
  isFinal: boolean;
  assigneeType: WorkflowAssigneeType;
  assigneeValue?: string;
  formCode?: string;
  actionKey: string;
  color: string;
}

interface TransitionForm {
  fromStateId: string;
  toStateId: string;
  name: string;
  guardType: WorkflowGuardType;
  requiredPermission?: string;
  guardPluginKey?: string;
  requiresComment: boolean;
  sortOrder: number;
}

export function WorkflowEditor() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canManage = hasPermission("Workflow.Definition.Manage");
  const definition = useWorkflowDefinition(id);
  const users = useCustomerOwners(canManage);
  const roles = useRoles(canManage);
  const departments = useDepartments(canManage);
  const formTemplates = useFormTemplates(false);
  const [stateOpen, setStateOpen] = useState(false);
  const [editingState, setEditingState] = useState<WorkflowState | null>(null);
  const [transitionOpen, setTransitionOpen] = useState(false);
  const [nameOpen, setNameOpen] = useState(false);
  const [stateForm] = Form.useForm<StateForm>();
  const [transitionForm] = Form.useForm<TransitionForm>();
  const [nameForm] = Form.useForm<{ name: string; description?: string }>();
  const assigneeType = Form.useWatch("assigneeType", stateForm);
  const addState = useAddWorkflowState(id);
  const updateState = useUpdateWorkflowState(id, editingState?.id);
  const removeState = useRemoveWorkflowState(id);
  const addTransition = useAddWorkflowTransition(id);
  const removeTransition = useRemoveWorkflowTransition(id);
  const publish = usePublishWorkflowDefinition();
  const reopen = useReopenWorkflowDefinition();
  const updateDraft = useUpdateWorkflowDraft(id);

  const states = definition.data?.states ?? [];
  const transitions = definition.data?.transitions ?? [];
  const isDraft = definition.data?.status === "Draft";
  const hasStart = states.filter((state) => state.stateType === "Start").length === 1;
  const hasEnd = states.some((state) => state.stateType === "End");
  const stateName = (stateId: string) =>
    states.find((state) => state.id === stateId)?.name ?? stateId;

  const flowNodes: Node[] = useMemo(
    () =>
      states.map((state, index) => ({
        id: state.id,
        position: {
          x: state.x && state.x !== 0 ? state.x : (index % 4) * 230,
          y: state.y && state.y !== 0 ? state.y : Math.floor(index / 4) * 150,
        },
        data: {
          label: `${state.name}${state.formCode ? `\n${state.formCode}` : ""}`,
        },
        style: {
          border: `2px solid ${state.color || "#1677ff"}`,
          background:
            state.stateType === "Start"
              ? "#f6ffed"
              : state.stateType === "End"
                ? "#fff1f0"
                : "#e6f4ff",
          borderRadius: 10,
          padding: 10,
          width: 180,
          whiteSpace: "pre-line" as const,
          textAlign: "center" as const,
        },
      })),
    [states],
  );

  const flowEdges: Edge[] = useMemo(
    () =>
      transitions.map((transition) => ({
        id: transition.id,
        source: transition.fromStateId,
        target: transition.toStateId,
        label: transition.name,
        animated: true,
        style: { stroke: "#597ef7" },
        labelStyle: { fontSize: 11 },
      })),
    [transitions],
  );

  const handleNodeClick: NodeMouseHandler = (_event, node) => {
    const state = states.find((item) => item.id === node.id);
    if (state) openEditState(state);
  };

  const handleNodeDragStop: OnNodeDrag = (_event, node) => {
    const state = states.find((item) => item.id === node.id);
    if (!state || !canManage) return;
    updateState.mutate({
      ...state,
      stateId: state.id,
      x: node.position.x,
      y: node.position.y,
    });
  };

  const assigneeOptions = useMemo(() => {
    if (assigneeType === "User") {
      return (users.data ?? []).map((user) => ({
        value: user.id,
        label: user.fullName,
      }));
    }
    if (assigneeType === "Role") {
      return (roles.data ?? []).map((role) => ({
        value: role.id,
        label: role.displayName || role.name,
      }));
    }
    if (assigneeType === "Department") {
      return (departments.data ?? []).map((department) => ({
        value: department.id,
        label: department.name,
      }));
    }
    return [];
  }, [assigneeType, departments.data, roles.data, users.data]);

  useEffect(() => {
    if (definition.data) {
      nameForm.setFieldsValue({
        name: definition.data.name,
        description: definition.data.description ?? undefined,
      });
    }
  }, [definition.data, nameForm]);

  const openAddState = () => {
    setEditingState(null);
    stateForm.resetFields();
    stateForm.setFieldsValue({
      stateType: "Normal",
      order: states.length + 1,
      isFinal: false,
      assigneeType: "None",
      actionKey: "None",
      color: "#1677ff",
    });
    setStateOpen(true);
  };

  const openEditState = (state: WorkflowState) => {
    setEditingState(state);
    stateForm.setFieldsValue({
      code: state.code,
      name: state.name,
      stateType: state.stateType,
      order: state.order,
      isFinal: state.isFinal,
      assigneeType: state.assigneeType,
      assigneeValue: state.assigneeValue ?? undefined,
      formCode: state.formCode ?? undefined,
      actionKey: state.actionKey ?? "None",
      color: state.color,
    });
    setStateOpen(true);
  };

  const submitState = async (values: StateForm) => {
    const payload = {
      code: values.code.trim(),
      name: values.name.trim(),
      stateType: values.stateType,
      order: values.order,
      isFinal: values.isFinal,
      assigneeType: values.assigneeType,
      assigneeValue:
        values.assigneeType === "CustomerOwner" || values.assigneeType === "None"
          ? null
          : values.assigneeValue?.trim() || null,
      formCode: values.formCode?.trim() || null,
      actionKey: values.actionKey,
      color: values.color,
      x: editingState?.x ?? 0,
      y: editingState?.y ?? 0,
    };

    try {
      if (editingState) {
        await updateState.mutateAsync(payload);
        message.success("وضعیت به‌روزرسانی شد.");
      } else {
        await addState.mutateAsync(payload);
        message.success("وضعیت اضافه شد.");
      }
      setStateOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const submitTransition = async (values: TransitionForm) => {
    try {
      await addTransition.mutateAsync({
        fromStateId: values.fromStateId,
        toStateId: values.toStateId,
        name: values.name.trim(),
        guardType: values.guardType,
        requiredPermission: values.requiredPermission?.trim() || null,
        guardPluginKey: values.guardPluginKey?.trim() || null,
        requiresComment: values.requiresComment,
        sortOrder: values.sortOrder,
      });
      message.success("گذار اضافه شد.");
      setTransitionOpen(false);
      transitionForm.resetFields();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const submitName = async (values: { name: string; description?: string }) => {
    try {
      await updateDraft.mutateAsync({
        name: values.name.trim(),
        description: values.description?.trim() || null,
      });
      message.success("اطلاعات گردش کار به‌روزرسانی شد.");
      setNameOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش می‌شود.
    }
  };

  const handlePublish = async () => {
    if (!id) return;
    try {
      await publish.mutateAsync(id);
      message.success("گردش کار منتشر شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  if (definition.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (definition.isError || !definition.data) {
    return (
      <Card variant="borderless" className="settings-section-card">
        <Typography.Title level={3}>گردش کار یافت نشد</Typography.Title>
        <Button type="primary" onClick={() => navigate("/workflow/definitions")}>
          بازگشت
        </Button>
      </Card>
    );
  }

  const stateColumns: TableColumnsType<WorkflowState> = [
    {
      title: "ترتیب",
      dataIndex: "order",
      width: 70,
      align: "center",
    },
    {
      title: "کد",
      dataIndex: "code",
      width: 130,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    { title: "نام", dataIndex: "name" },
    {
      title: "نوع",
      dataIndex: "stateType",
      width: 90,
      render: (value: WorkflowStateType) => (
        <Tag bordered={false}>{stateTypeLabels[value]}</Tag>
      ),
    },
    {
      title: "مسئول",
      dataIndex: "assigneeType",
      width: 120,
      render: (value: WorkflowAssigneeType) => assigneeLabels[value] ?? value,
    },
    {
      title: "اکشن",
      dataIndex: "actionKey",
      width: 145,
      render: (value: string | null) => actionKeyLabels[value ?? "None"] ?? value,
    },
    {
      title: "فرم",
      dataIndex: "formCode",
      width: 120,
      render: (value: string | null) => value || "-",
    },
    {
      title: "عملیات",
      width: 150,
      render: (_value, record) =>
        canManage && isDraft ? (
          <Space size="small">
            <Button type="link" icon={<EditOutlined />} onClick={() => openEditState(record)}>
              ویرایش
            </Button>
            <Popconfirm
              title="این وضعیت حذف شود؟"
              onConfirm={async () => {
                try {
                  await removeState.mutateAsync(record.id);
                  message.success("وضعیت حذف شد.");
                } catch {
                  // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
                }
              }}
              okText="حذف"
              cancelText="انصراف"
            >
              <Button type="link" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Space>
        ) : (
          "-"
        ),
    },
  ];

  const transitionColumns: TableColumnsType<WorkflowTransition> = [
    {
      title: "از",
      dataIndex: "fromStateId",
      render: (value: string) => stateName(value),
    },
    {
      title: "به",
      dataIndex: "toStateId",
      render: (value: string) => stateName(value),
    },
    { title: "نام گذار", dataIndex: "name" },
    {
      title: "Guard",
      dataIndex: "guardType",
      width: 130,
      render: (value: WorkflowGuardType) => guardLabels[value] ?? value,
    },
    {
      title: "توضیح اجباری",
      dataIndex: "requiresComment",
      width: 115,
      align: "center",
      render: (value: boolean) => (value ? "بله" : "خیر"),
    },
    {
      title: "عملیات",
      width: 90,
      render: (_value, record) =>
        canManage && isDraft ? (
          <Popconfirm
            title="این گذار حذف شود؟"
            onConfirm={async () => {
              try {
                await removeTransition.mutateAsync(record.id);
                message.success("گذار حذف شد.");
              } catch {
                // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
              }
            }}
            okText="حذف"
            cancelText="انصراف"
          >
            <Button type="link" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        ) : (
          "-"
        ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/workflow/definitions")}
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} style={{ margin: 0 }}>
                {definition.data.name}
              </Typography.Title>
              <Tag color={definition.data.status === "Published" ? "green" : "default"}>
                {definition.data.status === "Published"
                  ? "منتشر شده"
                  : definition.data.status === "Archived"
                    ? "آرشیو شده"
                    : "پیش‌نویس"}
              </Tag>
              <Tag color="blue">نسخه {definition.data.version}</Tag>
            </Space>
            <Typography.Text type="secondary" dir="ltr">
              {definition.data.code}
            </Typography.Text>
          </div>
        </Space>

        {canManage && !isDraft && definition.data.status === "Published" && (
          <Popconfirm
            title="گردش کار برای ویرایش باز شود؟"
            description="تنها گردش کاری که درخواست ثبت‌شده ندارد قابل ویرایش است. پس از ویرایش، آن را دوباره منتشر کنید."
            okText="بازگشت به ویرایش"
            cancelText="انصراف"
            onConfirm={async () => {
              try {
                await reopen.mutateAsync(definition.data!.id);
                message.success("گردش کار برای ویرایش مستقیم باز شد.");
              } catch {
                // NotificationProvider خطا را نمایش می‌دهد.
              }
            }}
          >
            <Button danger icon={<EditOutlined />} loading={reopen.isPending}>
              ویرایش گردش کار بدون درخواست
            </Button>
          </Popconfirm>
        )}

        {canManage && isDraft && (
          <Space wrap>
            <Button
              icon={<EditOutlined />}
              onClick={() => {
                nameForm.setFieldsValue({
                  name: definition.data.name,
                  description: definition.data.description ?? undefined,
                });
                setNameOpen(true);
              }}
            >
              ویرایش اطلاعات
            </Button>
            <Popconfirm
              title="گردش کار منتشر شود؟"
              description="پس از انتشار، ویرایش Draft ممکن نیست."
              onConfirm={() => void handlePublish()}
              okText="انتشار"
              cancelText="انصراف"
            >
              <Button type="primary" icon={<RocketOutlined />} loading={publish.isPending}>
                انتشار
              </Button>
            </Popconfirm>
          </Space>
        )}
      </div>

      {isDraft && (!hasStart || !hasEnd) && (
        <Alert
          type="warning"
          showIcon
          message="برای انتشار، گردش کار باید دقیقاً یک وضعیت شروع و حداقل یک وضعیت پایان داشته باشد."
        />
      )}

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={8}>
          <Card variant="borderless" className="settings-section-card">
            <Typography.Text type="secondary">وضعیت‌ها</Typography.Text>
            <Typography.Title level={3}>{states.length}</Typography.Title>
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card variant="borderless" className="settings-section-card">
            <Typography.Text type="secondary">گذارها</Typography.Text>
            <Typography.Title level={3}>{transitions.length}</Typography.Title>
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card variant="borderless" className="settings-section-card">
            <Typography.Text type="secondary">وضعیت شروع / پایان</Typography.Text>
            <Typography.Title level={4}>
              {hasStart ? "شروع ✓" : "بدون شروع"} / {hasEnd ? "پایان ✓" : "بدون پایان"}
            </Typography.Title>
          </Card>
        </Col>
      </Row>

      <Card
        title="وضعیت‌ها"
        variant="borderless"
        className="settings-section-card"
        extra={
          canManage && isDraft ? (
            <Button type="primary" ghost icon={<PlusOutlined />} onClick={openAddState}>
              افزودن وضعیت
            </Button>
          ) : null
        }
      >
        <Table<WorkflowState>
          rowKey="id"
          size="small"
          columns={stateColumns}
          dataSource={states}
          pagination={false}
          scroll={{ x: 760 }}
          className="module-table"
          locale={{ emptyText: "وضعیتی تعریف نشده است." }}
        />
      </Card>

      <Card
        title="گذارها"
        variant="borderless"
        className="settings-section-card"
        extra={
          canManage && isDraft && states.length >= 2 ? (
            <Button
              type="primary"
              ghost
              icon={<PlusOutlined />}
              onClick={() => {
                transitionForm.resetFields();
                transitionForm.setFieldsValue({
                  guardType: "None",
                  requiresComment: false,
                  sortOrder: transitions.length + 1,
                });
                setTransitionOpen(true);
              }}
            >
              افزودن گذار
            </Button>
          ) : null
        }
      >
        <Table<WorkflowTransition>
          rowKey="id"
          size="small"
          columns={transitionColumns}
          dataSource={transitions}
          pagination={false}
          scroll={{ x: 760 }}
          className="module-table"
          locale={{ emptyText: "گذاری تعریف نشده است." }}
        />
      </Card>

      <Card
        title="بوم گرافیکی گردش کار"
        variant="borderless"
        className="settings-section-card"
      >
        <div style={{ height: 540, direction: "ltr" }}>
          <ReactFlow
            nodes={flowNodes}
            edges={flowEdges}
            fitView
            onNodeClick={handleNodeClick}
            onNodeDragStop={handleNodeDragStop}
            nodesDraggable={isDraft && canManage}
            nodesConnectable={false}
            elementsSelectable
          >
            <Background />
            <Controls />
            <MiniMap />
          </ReactFlow>
        </div>
        <Typography.Text type="secondary">
          برای ویرایش هر مرحله روی گره کلیک کنید. جابه‌جایی گره‌ها در نسخه پیش‌نویس ذخیره می‌شود.
        </Typography.Text>
      </Card>

      <Card
        title="پیش‌نمایش مسیر گردش کار"
        variant="borderless"
        className="settings-section-card"
      >
        {states.length === 0 ? (
          <Typography.Text type="secondary">هنوز مرحله‌ای تعریف نشده است.</Typography.Text>
        ) : (
          <Space direction="vertical" size={10} style={{ width: "100%" }}>
            {[...states]
              .sort((a, b) => a.order - b.order)
              .map((state) => {
                const outbound = transitions.filter(
                  (transition) => transition.fromStateId === state.id,
                );
                return (
                  <div
                    key={state.id}
                    style={{
                      border: `1px solid ${state.color || "#d9d9d9"}`,
                      borderInlineStartWidth: 5,
                      borderRadius: 8,
                      padding: "10px 12px",
                      background: "#fff",
                    }}
                  >
                    <Space wrap>
                      <Tag color={state.stateType === "Start" ? "blue" : state.stateType === "End" ? "green" : "default"}>
                        {stateTypeLabels[state.stateType]}
                      </Tag>
                      <Typography.Text strong>{state.name}</Typography.Text>
                      <Typography.Text type="secondary" dir="ltr">
                        {state.code}
                      </Typography.Text>
                      {state.formCode && <Tag color="purple">{state.formCode}</Tag>}
                    </Space>
                    {outbound.length > 0 && (
                      <div style={{ marginTop: 8, paddingInlineStart: 12 }}>
                        {outbound.map((transition) => (
                          <Typography.Text key={transition.id} type="secondary">
                            ↳ {transition.name} → {stateName(transition.toStateId)}
                            <br />
                          </Typography.Text>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
          </Space>
        )}
      </Card>

      <Modal
        title={editingState ? "ویرایش وضعیت" : "افزودن وضعیت"}
        open={stateOpen}
        onCancel={() => setStateOpen(false)}
        onOk={() => stateForm.submit()}
        okText={editingState ? "ذخیره" : "افزودن"}
        cancelText="انصراف"
        confirmLoading={addState.isPending || updateState.isPending}
        width={600}
        forceRender
      >
        <Form form={stateForm} layout="vertical" onFinish={submitState}>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="code"
                label="کد"
                rules={[{ required: true, message: "کد وضعیت را وارد کنید." }]}
              >
                <Input placeholder="START" dir="ltr" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="name"
                label="نام"
                rules={[{ required: true, message: "نام وضعیت را وارد کنید." }]}
              >
                <Input placeholder="نام وضعیت" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="stateType" label="نوع">
                <Select
                  options={Object.entries(stateTypeLabels).map(([value, label]) => ({ value, label }))}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="order" label="ترتیب">
                <InputNumber min={0} style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="color" label="رنگ">
                <Input type="color" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="isFinal" label="وضعیت نهایی" valuePropName="checked">
            <Switch checkedChildren="بله" unCheckedChildren="خیر" />
          </Form.Item>
          <Form.Item name="assigneeType" label="نوع مسئول">
            <Select
              options={Object.entries(assigneeLabels).map(([value, label]) => ({ value, label }))}
            />
          </Form.Item>
          {assigneeType && !["None", "CustomerOwner"].includes(assigneeType) && (
            <Form.Item
              name="assigneeValue"
              label="مسئول"
              rules={[{ required: true, message: "مسئول را انتخاب کنید." }]}
            >
              <Select
                showSearch
                optionFilterProp="label"
                options={assigneeOptions}
                placeholder="انتخاب مسئول"
              />
            </Form.Item>
          )}
          <Form.Item name="actionKey" label="اکشن مرحله">
            <Select
              options={Object.entries(actionKeyLabels).map(([value, label]) => ({
                value,
                label,
              }))}
            />
          </Form.Item>
          <Form.Item name="formCode" label="فرم متصل (اختیاری)">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="انتخاب فرم"
              options={(formTemplates.data ?? []).map((template) => ({
                value: template.code,
                label: `${template.title} (${template.code})`,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="افزودن گذار"
        open={transitionOpen}
        onCancel={() => setTransitionOpen(false)}
        onOk={() => transitionForm.submit()}
        okText="افزودن"
        cancelText="انصراف"
        confirmLoading={addTransition.isPending}
        width={600}
        forceRender
      >
        <Form form={transitionForm} layout="vertical" onFinish={submitTransition}>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="fromStateId"
                label="از وضعیت"
                rules={[{ required: true, message: "وضعیت مبدأ را انتخاب کنید." }]}
              >
                <Select options={states.map((state) => ({ value: state.id, label: state.name }))} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="toStateId"
                label="به وضعیت"
                rules={[{ required: true, message: "وضعیت مقصد را انتخاب کنید." }]}
              >
                <Select options={states.map((state) => ({ value: state.id, label: state.name }))} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item
            name="name"
            label="نام گذار"
            rules={[{ required: true, message: "نام گذار را وارد کنید." }]}
          >
            <Input placeholder="مثلاً تأیید فنی" />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="guardType" label="نوع Guard">
                <Select
                  options={Object.entries(guardLabels).map(([value, label]) => ({ value, label }))}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="sortOrder" label="ترتیب">
                <InputNumber min={0} style={{ width: "100%" }} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="requiredPermission" label="Permission لازم (اختیاری)">
            <Input placeholder="Workflow.Instance.Execute" dir="ltr" />
          </Form.Item>
          <Form.Item name="guardPluginKey" label="کلید Plugin (در صورت Plugin)">
            <Input placeholder="plugin-key" dir="ltr" />
          </Form.Item>
          <Form.Item name="requiresComment" label="توضیح اجباری" valuePropName="checked">
            <Switch checkedChildren="بله" unCheckedChildren="خیر" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="ویرایش اطلاعات گردش کار"
        open={nameOpen}
        onCancel={() => setNameOpen(false)}
        onOk={() => nameForm.submit()}
        okText="ذخیره"
        cancelText="انصراف"
        confirmLoading={updateDraft.isPending}
        forceRender
      >
        <Form form={nameForm} layout="vertical" onFinish={submitName}>
          <Form.Item name="name" label="نام" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="توضیحات">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
