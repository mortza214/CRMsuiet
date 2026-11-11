import { useEffect, useMemo, useState } from "react";
import {
  ArrowRightOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  RocketOutlined,
  SaveOutlined,
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
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import {
  formStatusLabels,
  useFormTemplate,
  usePublishFormTemplate,
  useUpdateFormTemplate,
} from "@/modules/forms/hooks/useForms";
import type {
  FormEntityType,
  FormFieldInput,
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

const fieldTypeLabels: Record<string, string> = {
  text: "متن کوتاه",
  textarea: "متن بلند",
  number: "عدد",
  select: "انتخابی",
  multiselect: "چندانتخابی",
  checkbox: "چک‌باکس",
  radio: "رادیو",
  date: "تاریخ",
  email: "ایمیل",
  phone: "تلفن",
  file: "فایل",
};

interface FieldForm {
  key: string;
  label: string;
  type: string;
  required: boolean;
  order: number;
  min?: string;
  max?: string;
  regex?: string;
  defaultValue?: string;
  optionsText?: string;
  visibleField?: string;
  visibleEquals?: string;
}

const typeAliases: Record<string, string> = {
  Text: "text",
  TextArea: "textarea",
  Number: "number",
  Select: "select",
  MultiSelect: "multiselect",
  Checkbox: "checkbox",
  Radio: "radio",
  Date: "date",
  Email: "email",
  Phone: "phone",
  File: "file",
};

export function FormBuilder() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const canManage = useAuthStore((state) => state.hasPermission("Forms.Form.Manage"));
  const definition = useFormTemplate(id);
  const update = useUpdateFormTemplate(id);
  const publish = usePublishFormTemplate();
  const [fields, setFields] = useState<FormFieldInput[]>([]);
  const [fieldOpen, setFieldOpen] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [form] = Form.useForm<FieldForm>();
  const [metaForm] = Form.useForm<{
    code: string;
    title: string;
    entityType: FormEntityType;
  }>();

  useEffect(() => {
    if (!definition.data) {
      return;
    }

    const source = definition.data.fields.length > 0
      ? definition.data.fields.map((field) => ({
          key: field.key,
          label: field.label,
          type: typeAliases[field.fieldType] ?? field.fieldType.toLowerCase(),
          required: field.isRequired,
          defaultValue: field.defaultValue ?? undefined,
          min: field.minValue ?? undefined,
          max: field.maxValue ?? undefined,
          regex: field.regexPattern ?? undefined,
          order: field.order,
          options: parseOptions(field.optionsJson),
          visibleIf: parseVisibleIf(field.visibleIfJson),
        }))
      : parseSchemaFields(definition.data.schemaJson);
    setFields(source);
    metaForm.setFieldsValue({
      code: definition.data.code,
      title: definition.data.title,
      entityType: definition.data.entityType,
    });
  }, [definition.data, metaForm]);

  const schemaJson = useMemo(
    () => JSON.stringify({ fields: fields.map(toSchemaField) }, null, 2),
    [fields],
  );
  const isDraft = definition.data?.status === "Draft";

  const openAdd = () => {
    setEditingIndex(null);
    form.resetFields();
    form.setFieldsValue({
      type: "text",
      required: false,
      order: fields.length + 1,
    });
    setFieldOpen(true);
  };

  const openEdit = (index: number) => {
    const field = fields[index];
    setEditingIndex(index);
    form.setFieldsValue({
      key: field.key,
      label: field.label,
      type: field.type,
      required: field.required ?? false,
      order: field.order ?? index + 1,
      min: field.min?.toString(),
      max: field.max?.toString(),
      regex: field.regex,
      defaultValue: field.defaultValue,
      optionsText: field.options?.length
        ? JSON.stringify(field.options, null, 2)
        : "",
      visibleField: field.visibleIf?.field,
      visibleEquals: field.visibleIf?.equals,
    });
    setFieldOpen(true);
  };

  const submitField = async (values: FieldForm) => {
    let options: Array<{ value: string; label: string }> | undefined;
    if (values.optionsText?.trim()) {
      try {
        const parsed = JSON.parse(values.optionsText) as Array<{
          value: string;
          label: string;
        }>;
        if (!Array.isArray(parsed)) {
          throw new Error("options must be an array");
        }
        options = parsed;
      } catch {
        message.error("گزینه‌ها باید آرایه JSON معتبر باشند.");
        return;
      }
    }

    const field: FormFieldInput = {
      key: values.key.trim(),
      label: values.label.trim(),
      type: values.type,
      required: values.required,
      order: values.order,
      min: values.min?.trim() || undefined,
      max: values.max?.trim() || undefined,
      regex: values.regex?.trim() || undefined,
      defaultValue: values.defaultValue?.trim() || undefined,
      options,
      visibleIf: values.visibleField?.trim()
        ? {
            field: values.visibleField.trim(),
            equals: values.visibleEquals?.trim() ?? "",
          }
        : null,
    };

    if (fields.some((item, index) =>
      index !== editingIndex
      && item.key.toLowerCase() === field.key.toLowerCase())) {
      message.error("کلید فیلد تکراری است.");
      return;
    }

    const next = [...fields];
    if (editingIndex === null) {
      next.push(field);
    } else {
      next[editingIndex] = field;
    }
    setFields(next.sort((left, right) => (left.order ?? 0) - (right.order ?? 0)));
    setFieldOpen(false);
  };

  const buildPayload = () => {
    const meta = metaForm.getFieldsValue();
    return {
      code: meta.code.trim(),
      title: meta.title.trim(),
      entityType: meta.entityType,
      schemaJson,
    };
  };

  const saveDraft = async () => {
    try {
      await update.mutateAsync(buildPayload());
      message.success("پیش‌نویس فرم ذخیره شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const handlePublish = async () => {
    if (fields.length === 0) {
      message.warning("برای انتشار حداقل یک فیلد اضافه کنید.");
      return;
    }
    try {
      await update.mutateAsync(buildPayload());
      if (id) {
        await publish.mutateAsync(id);
      }
      message.success("فرم منتشر شد.");
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
        <Typography.Title level={3}>فرم یافت نشد</Typography.Title>
        <Button type="primary" onClick={() => navigate("/forms/templates")}>
          بازگشت
        </Button>
      </Card>
    );
  }

  const columns: TableColumnsType<FormFieldInput> = [
    {
      title: "ترتیب",
      dataIndex: "order",
      width: 70,
      align: "center",
    },
    {
      title: "کلید",
      dataIndex: "key",
      width: 150,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    { title: "عنوان", dataIndex: "label" },
    {
      title: "نوع",
      dataIndex: "type",
      width: 130,
      render: (value: string) => fieldTypeLabels[value] ?? value,
    },
    {
      title: "الزامی",
      dataIndex: "required",
      width: 85,
      align: "center",
      render: (value: boolean) => (value ? "بله" : "خیر"),
    },
    {
      title: "نمایش شرطی",
      key: "visibleIf",
      width: 170,
      render: (_value, record) =>
        record.visibleIf
          ? `${record.visibleIf.field} = ${record.visibleIf.equals}`
          : "-",
    },
    {
      title: "عملیات",
      width: 150,
      render: (_value, record) => {
        const index = fields.indexOf(record);
        return canManage && isDraft ? (
          <Space size="small">
            <Button type="link" icon={<EditOutlined />} onClick={() => openEdit(index)}>
              ویرایش
            </Button>
            <Popconfirm
              title="این فیلد حذف شود؟"
              okText="حذف"
              cancelText="انصراف"
              onConfirm={() => setFields(fields.filter((_, itemIndex) => itemIndex !== index))}
            >
              <Button type="link" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Space>
        ) : (
          "-"
        );
      },
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <Space size="middle" align="start">
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate("/forms/templates")}
          />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} style={{ margin: 0 }}>
                {definition.data.title}
              </Typography.Title>
              <Tag color={definition.data.status === "Published" ? "green" : "default"}>
                {formStatusLabels[definition.data.status]}
              </Tag>
              <Tag color="blue">نسخه {definition.data.version}</Tag>
            </Space>
            <Typography.Text type="secondary" dir="ltr">
              {definition.data.code}
            </Typography.Text>
          </div>
        </Space>

        {canManage && isDraft && (
          <Space wrap>
            <Button icon={<SaveOutlined />} onClick={() => void saveDraft()} loading={update.isPending}>
              ذخیره پیش‌نویس
            </Button>
            <Button
              type="primary"
              icon={<RocketOutlined />}
              onClick={() => void handlePublish()}
              loading={publish.isPending}
            >
              انتشار
            </Button>
          </Space>
        )}
      </div>

      {isDraft && fields.length === 0 && (
        <Alert
          type="info"
          showIcon
          message="هنوز فیلدی اضافه نشده است؛ برای انتشار حداقل یک فیلد لازم است."
        />
      )}

      <Card title="اطلاعات فرم" variant="borderless" className="settings-section-card">
        <Form form={metaForm} layout="vertical" disabled={!isDraft || !canManage}>
          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item
                name="code"
                label="کد فرم"
                rules={[{ required: true, message: "کد فرم الزامی است." }]}
              >
                <Input dir="ltr" />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item
                name="title"
                label="عنوان"
                rules={[{ required: true, message: "عنوان الزامی است." }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="entityType" label="موجودیت هدف">
                <Select
                  options={Object.entries(entityLabels).map(([value, label]) => ({
                    value,
                    label,
                  }))}
                />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card
        title="فیلدهای فرم"
        variant="borderless"
        className="settings-section-card"
        extra={
          canManage && isDraft ? (
            <Button type="primary" ghost icon={<PlusOutlined />} onClick={openAdd}>
              افزودن فیلد
            </Button>
          ) : null
        }
      >
        <Table<FormFieldInput>
          rowKey="key"
          size="small"
          columns={columns}
          dataSource={fields}
          pagination={false}
          scroll={{ x: 830 }}
          className="module-table"
          locale={{ emptyText: "فیلدی تعریف نشده است." }}
        />
      </Card>

      <Card title="پیش‌نمایش Schema" variant="borderless" className="settings-section-card">
        <pre dir="ltr" style={{ margin: 0, whiteSpace: "pre-wrap", fontSize: 12 }}>
          {schemaJson}
        </pre>
      </Card>

      <Modal
        title={editingIndex === null ? "افزودن فیلد" : "ویرایش فیلد"}
        open={fieldOpen}
        onCancel={() => setFieldOpen(false)}
        onOk={() => form.submit()}
        okText="ذخیره فیلد"
        cancelText="انصراف"
        width={720}
        forceRender
      >
        <Form form={form} layout="vertical" onFinish={submitField}>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="key"
                label="کلید (انگلیسی)"
                rules={[{ required: true, message: "کلید فیلد الزامی است." }]}
              >
                <Input dir="ltr" placeholder="bandwidth_mb" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="label"
                label="عنوان"
                rules={[{ required: true, message: "عنوان فیلد الزامی است." }]}
              >
                <Input placeholder="پهنای باند (مگ)" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="type" label="نوع فیلد">
                <Select
                  options={Object.entries(fieldTypeLabels).map(([value, label]) => ({
                    value,
                    label,
                  }))}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="order" label="ترتیب">
                <InputNumber min={1} style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="required" label="الزامی">
                <Select
                  options={[
                    { value: true, label: "بله" },
                    { value: false, label: "خیر" },
                  ]}
                />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="min" label="حداقل / حداقل طول">
                <Input dir="ltr" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="max" label="حداکثر / حداکثر طول">
                <Input dir="ltr" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="defaultValue" label="مقدار پیش‌فرض">
                <Input />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="regex" label="Regex (اختیاری)">
            <Input dir="ltr" placeholder="^[0-9]+$" />
          </Form.Item>
          <Form.Item name="optionsText" label="گزینه‌ها (آرایه JSON، برای Select/Radio)">
            <Input.TextArea
              rows={3}
              dir="ltr"
              placeholder='[{"value":"wireless","label":"وایرلس"},{"value":"fiber","label":"فیبر"}]'
            />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="visibleField" label="نمایش وابسته به فیلد">
                <Input dir="ltr" placeholder="medium" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="visibleEquals" label="مقدار مساوی">
                <Input dir="ltr" placeholder="wireless" />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </Space>
  );
}

function parseSchemaFields(schemaJson: string): FormFieldInput[] {
  try {
    const schema = JSON.parse(schemaJson) as {
      fields?: Array<Record<string, unknown>>;
      properties?: Record<string, Record<string, unknown>>;
      required?: string[];
    };
    if (Array.isArray(schema.fields)) {
      return schema.fields.map((field, index) => ({
        key: String(field.key ?? ""),
        label: String(field.label ?? field.key ?? ""),
        type: String(field.type ?? "text").toLowerCase(),
        required: Boolean(field.required),
        defaultValue: field.defaultValue ? String(field.defaultValue) : undefined,
        min: field.min !== undefined ? String(field.min) : undefined,
        max: field.max !== undefined ? String(field.max) : undefined,
        regex: field.regex ? String(field.regex) : undefined,
        order: Number(field.order ?? index + 1),
        options: Array.isArray(field.options)
          ? (field.options as Array<{ value?: unknown; label?: unknown }>).map((option) => ({
              value: String(option.value ?? ""),
              label: String(option.label ?? option.value ?? ""),
            }))
          : undefined,
        visibleIf: field.visibleIf
          ? {
              field: String((field.visibleIf as { field?: unknown }).field ?? ""),
              equals: String((field.visibleIf as { equals?: unknown }).equals ?? ""),
            }
          : null,
      }));
    }
    if (schema.properties) {
      const required = new Set(schema.required ?? []);
      return Object.entries(schema.properties).map(([key, property], index) => ({
        key,
        label: String(property.title ?? key),
        type: String(property.type ?? "text").toLowerCase(),
        required: required.has(key),
        min: property.minimum !== undefined ? String(property.minimum) : undefined,
        max: property.maximum !== undefined ? String(property.maximum) : undefined,
        regex: property.pattern ? String(property.pattern) : undefined,
        order: index + 1,
      }));
    }
    return [];
  } catch {
    return [];
  }
}

function parseOptions(optionsJson?: string | null): Array<{ value: string; label: string }> | undefined {
  if (!optionsJson) return undefined;
  try {
    const parsed = JSON.parse(optionsJson) as Array<unknown>;
    if (!Array.isArray(parsed)) return undefined;
    return parsed.map((item) => {
      if (typeof item === "string") return { value: item, label: item };
      const object = item as { value?: unknown; label?: unknown };
      return {
        value: String(object.value ?? ""),
        label: String(object.label ?? object.value ?? ""),
      };
    });
  } catch {
    return undefined;
  }
}

function parseVisibleIf(value?: string | null): { field: string; equals: string } | null {
  if (!value) return null;
  try {
    const parsed = JSON.parse(value) as { field?: string; equals?: unknown };
    return parsed.field
      ? { field: parsed.field, equals: String(parsed.equals ?? "") }
      : null;
  } catch {
    return null;
  }
}

function toSchemaField(field: FormFieldInput) {
  const result: Record<string, unknown> = {
    key: field.key,
    label: field.label,
    type: field.type,
    required: field.required ?? false,
    order: field.order ?? 0,
  };
  if (field.defaultValue) result.defaultValue = field.defaultValue;
  if (field.min) {
    const value = Number(field.min);
    result.min = Number.isFinite(value) ? value : field.min;
  }
  if (field.max) {
    const value = Number(field.max);
    result.max = Number.isFinite(value) ? value : field.max;
  }
  if (field.regex) result.regex = field.regex;
  if (field.options?.length) result.options = field.options;
  if (field.visibleIf) result.visibleIf = field.visibleIf;
  return result;
}
