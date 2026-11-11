import { useState } from "react";
import {
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  RocketOutlined,
  SearchOutlined,
  StopOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import {
  useArchiveWorkflowDefinition,
  useCreateWorkflowDefinition,
  usePublishWorkflowDefinition,
  useWorkflowDefinitions,
} from "@/modules/workflow/hooks/useWorkflow";
import type {
  WorkflowDefinitionStatus,
  WorkflowDefinitionSummary,
  WorkflowEntityType,
} from "@/modules/workflow/types";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

const statusMeta: Record<WorkflowDefinitionStatus, { label: string; color: string }> = {
  Draft: { label: "پیش‌نویس", color: "default" },
  Published: { label: "منتشر شده", color: "green" },
  Archived: { label: "آرشیو شده", color: "purple" },
};

const entityLabels: Record<WorkflowEntityType, string> = {
  Order: "سفارش",
  Ticket: "تیکت",
  Customer: "مشتری",
  ManualTask: "وظیفه دستی",
};

interface CreateForm {
  code: string;
  name: string;
  entityType: WorkflowEntityType;
  description?: string;
}

export function WorkflowDefinitions() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canManage = hasPermission("Workflow.Definition.Manage");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<WorkflowDefinitionStatus | undefined>();
  const [entityType, setEntityType] = useState<WorkflowEntityType | undefined>();
  const [createOpen, setCreateOpen] = useState(false);
  const [form] = Form.useForm<CreateForm>();
  const debouncedSearch = useDebouncedValue(search, 400);
  const definitions = useWorkflowDefinitions({
    page,
    pageSize,
    search: debouncedSearch,
    status,
    entityType,
  });
  const createDefinition = useCreateWorkflowDefinition();
  const publishDefinition = usePublishWorkflowDefinition();
  const archiveDefinition = useArchiveWorkflowDefinition();

  const openCreate = () => {
    form.resetFields();
    form.setFieldsValue({ entityType: "Order" });
    setCreateOpen(true);
  };

  const submitCreate = async (values: CreateForm) => {
    try {
      const created = await createDefinition.mutateAsync({
        code: values.code.trim(),
        name: values.name.trim(),
        entityType: values.entityType,
        description: values.description?.trim() || null,
      });
      message.success("گردش کار ایجاد شد.");
      setCreateOpen(false);
      navigate(`/workflow/definitions/${created.id}`);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const publish = async (id: string) => {
    try {
      await publishDefinition.mutateAsync(id);
      message.success("گردش کار منتشر شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const archive = async (id: string) => {
    try {
      await archiveDefinition.mutateAsync(id);
      message.success("گردش کار آرشیو شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<WorkflowDefinitionSummary> = [
    {
      title: "کد",
      dataIndex: "code",
      key: "code",
      width: 170,
      render: (value: string) => (
        <Typography.Text strong dir="ltr" copyable={{ text: value }}>
          {value}
        </Typography.Text>
      ),
    },
    { title: "نام", dataIndex: "name", key: "name", width: 230 },
    {
      title: "موجودیت",
      dataIndex: "entityType",
      key: "entityType",
      width: 110,
      render: (value: WorkflowEntityType) => entityLabels[value] ?? value,
    },
    {
      title: "نسخه",
      dataIndex: "version",
      key: "version",
      width: 75,
      align: "center",
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 130,
      render: (value: WorkflowDefinitionStatus) => {
        const meta = statusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "وضعیت‌ها / گذارها",
      key: "counts",
      width: 140,
      align: "center",
      render: (_value, record) => `${record.stateCount} / ${record.transitionCount}`,
    },
    {
      title: "تاریخ انتشار",
      dataIndex: "publishedAtJalali",
      key: "publishedAtJalali",
      width: 130,
      render: (value: string | null) => value || "-",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 220,
      fixed: "left",
      render: (_value, record) => (
        <Space size="small" wrap>
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/workflow/definitions/${record.id}`)}
          >
            مشاهده
          </Button>
          {canManage && record.status === "Draft" && (
            <>
              <Button
                type="link"
                icon={<EditOutlined />}
                onClick={() => navigate(`/workflow/definitions/${record.id}`)}
              >
                ویرایش
              </Button>
              <Popconfirm
                title="گردش کار منتشر شود؟"
                okText="انتشار"
                cancelText="انصراف"
                onConfirm={() => void publish(record.id)}
              >
                <Button type="link" icon={<RocketOutlined />}>
                  انتشار
                </Button>
              </Popconfirm>
            </>
          )}
          {canManage && record.status === "Published" && (
            <Popconfirm
              title="گردش کار آرشیو شود؟"
              okText="آرشیو"
              cancelText="انصراف"
              onConfirm={() => void archive(record.id)}
            >
              <Button type="link" icon={<StopOutlined />}>
                آرشیو
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>گردش کارها</Typography.Title>
          <Typography.Text type="secondary">
            تعریف وضعیت‌ها، گذارها، قوانین Guard و انتشار نسخه‌ها
          </Typography.Text>
        </div>
        {canManage && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            تعریف گردش کار
          </Button>
        )}
      </div>

      <div className="table-toolbar">
        <Input
          allowClear
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
          prefix={<SearchOutlined />}
          placeholder="جستجوی کد یا نام گردش کار..."
          className="toolbar-search"
        />
        <Space wrap>
          <Select
            allowClear
            value={status}
            onChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
            placeholder="وضعیت"
            style={{ minWidth: 145 }}
            options={Object.entries(statusMeta).map(([value, meta]) => ({
              value,
              label: meta.label,
            }))}
          />
          <Select
            allowClear
            value={entityType}
            onChange={(value) => {
              setEntityType(value);
              setPage(1);
            }}
            placeholder="نوع موجودیت"
            style={{ minWidth: 145 }}
            options={Object.entries(entityLabels).map(([value, label]) => ({
              value,
              label,
            }))}
          />
        </Space>
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<WorkflowDefinitionSummary>
          rowKey="id"
          columns={columns}
          dataSource={definitions.data?.items ?? []}
          loading={definitions.isLoading || definitions.isFetching}
          scroll={{ x: 1180 }}
          className="module-table"
          locale={{ emptyText: "گردش کاری تعریف نشده است." }}
          pagination={{
            current: definitions.data?.page ?? page,
            pageSize: definitions.data?.pageSize ?? pageSize,
            total: definitions.data?.totalCount ?? 0,
            showSizeChanger: true,
            pageSizeOptions: [10, 20, 50],
            showTotal: (total) => `مجموع ${total} گردش کار`,
            onChange: (nextPage, nextPageSize) => {
              setPage(nextPageSize !== pageSize ? 1 : nextPage);
              setPageSize(nextPageSize);
            },
          }}
        />
      </Card>

      <Modal
        title="تعریف گردش کار جدید"
        open={createOpen}
        onCancel={() => setCreateOpen(false)}
        onOk={() => form.submit()}
        okText="ایجاد و ویرایش"
        cancelText="انصراف"
        confirmLoading={createDefinition.isPending}
      >
        <Form form={form} layout="vertical" onFinish={submitCreate}>
          <Form.Item
            name="code"
            label="کد یکتا"
            rules={[{ required: true, message: "کد گردش کار را وارد کنید." }]}
          >
            <Input placeholder="مثلاً ORDER-FULFILLMENT" dir="ltr" />
          </Form.Item>
          <Form.Item
            name="name"
            label="نام"
            rules={[{ required: true, message: "نام گردش کار را وارد کنید." }]}
          >
            <Input placeholder="نام گردش کار" />
          </Form.Item>
          <Form.Item
            name="entityType"
            label="نوع موجودیت"
            rules={[{ required: true, message: "نوع موجودیت را انتخاب کنید." }]}
          >
            <Select
              options={Object.entries(entityLabels).map(([value, label]) => ({
                value,
                label,
              }))}
            />
          </Form.Item>
          <Form.Item name="description" label="توضیحات">
            <Input.TextArea rows={3} placeholder="توضیحات اختیاری" />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
