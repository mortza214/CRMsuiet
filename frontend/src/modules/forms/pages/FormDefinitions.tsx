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
  formStatusLabels,
  useArchiveFormTemplate,
  useCreateFormTemplate,
  useFormTemplates,
  usePublishFormTemplate,
} from "@/modules/forms/hooks/useForms";
import type {
  FormDefinitionStatus,
  FormEntityType,
  FormTemplate,
} from "@/modules/forms/types";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

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

const statusColors: Record<FormDefinitionStatus, string> = {
  Draft: "default",
  Published: "green",
  Archived: "purple",
};

interface CreateForm {
  code: string;
  title: string;
  entityType: FormEntityType;
}

export function FormDefinitions() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canManage = hasPermission("Forms.Form.Manage");
  const [search, setSearch] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [form] = Form.useForm<CreateForm>();
  const debouncedSearch = useDebouncedValue(search, 350);
  const templates = useFormTemplates(true);
  const create = useCreateFormTemplate();
  const publish = usePublishFormTemplate();
  const archive = useArchiveFormTemplate();

  const rows = (templates.data ?? []).filter((template) => {
    const term = debouncedSearch.trim().toLowerCase();
    return (
      !term
      || template.title.toLowerCase().includes(term)
      || template.code.toLowerCase().includes(term)
    );
  });

  const submitCreate = async (values: CreateForm) => {
    try {
      const created = await create.mutateAsync({
        code: values.code.trim(),
        title: values.title.trim(),
        entityType: values.entityType,
        schemaJson: JSON.stringify({ fields: [] }),
        isActive: false,
      });
      message.success("فرم ایجاد شد.");
      setCreateOpen(false);
      navigate(`/forms/templates/${created.id}`);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handlePublish = async (id: string) => {
    try {
      await publish.mutateAsync(id);
      message.success("فرم منتشر شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handleArchive = async (id: string) => {
    try {
      await archive.mutateAsync(id);
      message.success("فرم آرشیو شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<FormTemplate> = [
    {
      title: "کد",
      dataIndex: "code",
      width: 170,
      render: (value: string) => (
        <Typography.Text dir="ltr" copyable={{ text: value }} strong>
          {value}
        </Typography.Text>
      ),
    },
    { title: "عنوان", dataIndex: "title", width: 240 },
    {
      title: "موجودیت",
      dataIndex: "entityType",
      width: 120,
      render: (value: FormEntityType) => entityLabels[value] ?? value,
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      width: 130,
      render: (value: FormDefinitionStatus) => (
        <Tag color={statusColors[value]} bordered={false}>
          {formStatusLabels[value]}
        </Tag>
      ),
    },
    {
      title: "نسخه",
      dataIndex: "version",
      width: 80,
      align: "center",
    },
    {
      title: "تعداد فیلد",
      key: "fieldCount",
      width: 100,
      align: "center",
      render: (_value, record) => record.fields.length,
    },
    {
      title: "تاریخ انتشار",
      dataIndex: "publishedAtJalali",
      width: 125,
      render: (value: string | null | undefined) => value || "-",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 230,
      fixed: "left",
      render: (_value, record) => (
        <Space size="small" wrap>
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/forms/templates/${record.id}`)}
          >
            مشاهده
          </Button>
          {canManage && record.status === "Draft" && (
            <>
              <Button
                type="link"
                icon={<EditOutlined />}
                onClick={() => navigate(`/forms/templates/${record.id}`)}
              >
                ویرایش
              </Button>
              <Popconfirm
                title="فرم منتشر شود؟"
                okText="انتشار"
                cancelText="انصراف"
                onConfirm={() => void handlePublish(record.id)}
              >
                <Button type="link" icon={<RocketOutlined />}>
                  انتشار
                </Button>
              </Popconfirm>
            </>
          )}
          {canManage && record.status === "Published" && (
            <Popconfirm
              title="فرم آرشیو شود؟"
              okText="آرشیو"
              cancelText="انصراف"
              onConfirm={() => void handleArchive(record.id)}
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
          <Typography.Title level={2}>فرم‌ساز</Typography.Title>
          <Typography.Text type="secondary">
            ساخت، نسخه‌بندی و انتشار فرم‌های ساختاریافته
          </Typography.Text>
        </div>
        <Space wrap>
          <Button onClick={() => navigate("/forms/submissions")}>پاسخ‌ها</Button>
          {canManage && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                form.resetFields();
                form.setFieldsValue({ entityType: "None" });
                setCreateOpen(true);
              }}
            >
              فرم جدید
            </Button>
          )}
        </Space>
      </div>

      <div className="table-toolbar">
        <Input
          allowClear
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          prefix={<SearchOutlined />}
          placeholder="جستجوی کد یا عنوان فرم..."
          className="toolbar-search"
        />
        <span className="dashboard-badge">{rows.length} فرم</span>
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<FormTemplate>
          rowKey="id"
          columns={columns}
          dataSource={rows}
          loading={templates.isLoading || templates.isFetching}
          scroll={{ x: 1180 }}
          className="module-table"
          locale={{ emptyText: "فرمی تعریف نشده است." }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
        />
      </Card>

      <Modal
        title="تعریف فرم جدید"
        open={createOpen}
        onCancel={() => setCreateOpen(false)}
        onOk={() => form.submit()}
        okText="ایجاد و طراحی"
        cancelText="انصراف"
        confirmLoading={create.isPending}
        forceRender
      >
        <Form form={form} layout="vertical" onFinish={submitCreate}>
          <Form.Item
            name="code"
            label="کد یکتای فرم"
            rules={[{ required: true, message: "کد فرم را وارد کنید." }]}
          >
            <Input dir="ltr" placeholder="FORM-BANDWIDTH" />
          </Form.Item>
          <Form.Item
            name="title"
            label="عنوان فرم"
            rules={[{ required: true, message: "عنوان فرم را وارد کنید." }]}
          >
            <Input placeholder="مثلاً فرم درخواست پهنای باند" />
          </Form.Item>
          <Form.Item name="entityType" label="موجودیت هدف">
            <Select
              options={Object.entries(entityLabels).map(([value, label]) => ({
                value,
                label,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
