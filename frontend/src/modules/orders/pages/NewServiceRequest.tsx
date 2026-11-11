import { useEffect, useMemo, useState } from "react";
import { ArrowRightOutlined, CloudServerOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Col,
  Empty,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Spin,
  Switch,
  Typography,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import {
  useCatalogService,
  useCatalogServices,
  useServiceCategories,
} from "@/modules/catalog/hooks/useCatalog";
import { useCreateServiceRequest } from "@/modules/orders/hooks/useOrders";

interface AttributeField {
  key: string;
  label: string;
  type: string;
  required?: boolean;
  options?: Array<{ value: string; label: string }>;
  visibleIf?: { field: string; equals?: unknown; notEquals?: unknown };
}

interface SchemaField extends AttributeField {
  order?: number;
}

const baseFields: SchemaField[] = [
  {
    key: "bandwidth_mb",
    label: "میزان پهنای باند (مگ)",
    type: "number",
    required: true,
  },
  {
    key: "substrate",
    label: "نوع بستر",
    type: "select",
    required: true,
    options: [
      { value: "wireless", label: "وایرلس" },
      { value: "fiber", label: "فیبر" },
      { value: "cable", label: "کابل" },
    ],
  },
];

const wirelessFields: SchemaField[] = [
  { key: "building_height", label: "ارتفاع ساختمان / طبقه", type: "number", required: true },
  { key: "geo_coordinates", label: "مختصات جغرافیایی", type: "text", required: true },
  { key: "total_height", label: "ارتفاع کل", type: "number" },
  { key: "existing_equipment_type", label: "نوع تجهیزات موجود", type: "text" },
  { key: "existing_equipment_count", label: "تعداد تجهیزات موجود", type: "number" },
  {
    key: "has_cable",
    label: "کابل دارد؟",
    type: "select",
    options: [
      { value: "yes", label: "بله" },
      { value: "no", label: "خیر" },
    ],
  },
  { key: "extra_ip_count", label: "تعداد IP اضافی", type: "number" },
];

function parseSchema(schemaJson?: string | null): SchemaField[] {
  if (!schemaJson) return [];
  try {
    const schema = JSON.parse(schemaJson) as { fields?: Array<Record<string, unknown>> };
    return (schema.fields ?? [])
      .map((field) => ({
        key: String(field.key ?? ""),
        label: String(field.label ?? field.key ?? ""),
        type: String(field.type ?? "text"),
        required: Boolean(field.required),
        options: Array.isArray(field.options)
          ? field.options.map((option) => {
              const item = option as Record<string, unknown>;
              return { value: String(item.value ?? ""), label: String(item.label ?? item.value ?? "") };
            })
          : undefined,
        visibleIf:
          field.visibleIf && typeof field.visibleIf === "object"
            ? (field.visibleIf as AttributeField["visibleIf"])
            : undefined,
        order: typeof field.order === "number" ? field.order : 0,
      }))
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  } catch {
    return [];
  }
}

function isVisible(field: SchemaField, attributes: Record<string, unknown>) {
  if (!field.visibleIf) return true;
  const actual = attributes[field.visibleIf.field];
  if ("equals" in field.visibleIf) return actual === field.visibleIf.equals;
  if ("notEquals" in field.visibleIf) return actual !== field.visibleIf.notEquals;
  return true;
}

interface AttributeFieldInputProps {
  field: SchemaField;
  value: unknown;
  onChange: (value: unknown) => void;
}

function AttributeFieldInput({ field, value, onChange }: AttributeFieldInputProps) {
  if (field.options?.length) {
    return (
      <Select
        value={value as string | undefined}
        onChange={onChange}
        options={field.options}
        placeholder={`انتخاب ${field.label}`}
        allowClear={!field.required}
      />
    );
  }

  if (field.type === "number") {
    return (
      <InputNumber
        value={typeof value === "number" ? value : undefined}
        onChange={(next) => onChange(next)}
        style={{ width: "100%" }}
        placeholder={field.label}
        min={0}
      />
    );
  }

  return (
    <Input
      value={typeof value === "string" ? value : ""}
      onChange={(event) => onChange(event.target.value)}
      placeholder={field.label}
      dir={field.key === "geo_coordinates" ? "ltr" : undefined}
    />
  );
}

export function NewServiceRequest() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [categoryId, setCategoryId] = useState<string>();
  const [serviceId, setServiceId] = useState<string>();
  const [attributes, setAttributes] = useState<Record<string, unknown>>({});
  const [requiresFeasibility, setRequiresFeasibility] = useState(false);
  const [form] = Form.useForm<{ customerName?: string }>();
  const categories = useServiceCategories();
  const services = useCatalogServices({ page: 1, pageSize: 100, isActive: true });
  const service = useCatalogService(serviceId);
  const create = useCreateServiceRequest();

  const schemaFields = useMemo(
    () => parseSchema(service.data?.attributeSchemaJson),
    [service.data?.attributeSchemaJson],
  );

  const fields = useMemo(() => {
    const keys = new Set(schemaFields.map((field) => field.key));
    const result = [...schemaFields];
    for (const field of baseFields) {
      if (!keys.has(field.key)) result.push(field);
    }
    if (requiresFeasibility || attributes.substrate === "wireless") {
      for (const field of wirelessFields) {
        if (!keys.has(field.key)) result.push(field);
      }
    }
    return result;
  }, [schemaFields, requiresFeasibility, attributes.substrate]);

  useEffect(() => {
    setServiceId(undefined);
    setAttributes({});
    setRequiresFeasibility(false);
  }, [categoryId]);

  useEffect(() => {
    setAttributes({});
    setRequiresFeasibility(false);
  }, [serviceId]);

  const setAttribute = (key: string, value: unknown) => {
    setAttributes((current) => {
      const next = { ...current, [key]: value };
      if (key === "substrate") {
        setRequiresFeasibility(value === "wireless");
      }
      return next;
    });
  };

  const submit = async () => {
    if (!id) return;
    if (!serviceId) {
      message.error("سرویس را انتخاب کنید.");
      return;
    }
    for (const field of fields) {
      if (!isVisible(field, attributes)) continue;
      if (field.required) {
        const value = attributes[field.key];
        if (value === undefined || value === null || value === "") {
          message.error(`فیلد «${field.label}» الزامی است.`);
          return;
        }
      }
    }
    if (!attributes.bandwidth_mb && attributes.bandwidth_mb !== 0) {
      message.error("میزان پهنای باند الزامی است.");
      return;
    }

    try {
      const result = await create.mutateAsync({
        customerId: id,
        serviceId,
        attributesJson: JSON.stringify(attributes),
        requiresFeasibility,
      });
      message.success(`درخواست ${result.orderNumber} ثبت و گردش کار آغاز شد.`);
      navigate(`/orders/${result.orderId}`);
    } catch {
      // Error notification is centralized.
    }
  };

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="detail-heading">
        <Space>
          <Button
            type="text"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate(`/customers/${id}`)}
            aria-label="بازگشت"
          />
          <div>
            <Typography.Title level={2} className="detail-title">
              ثبت سرویس جدید
            </Typography.Title>
            <Typography.Text type="secondary">
              انتخاب سرویس، تکمیل مشخصات فنی و آغاز گردش کار فروش
            </Typography.Text>
          </div>
        </Space>
      </div>

      <Card
        variant="borderless"
        className="detail-card"
        title={
          <Space>
            <CloudServerOutlined />
            مشخصات درخواست
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item label="گروه سرویس" required>
                <Select
                  placeholder="انتخاب گروه"
                  value={categoryId}
                  onChange={setCategoryId}
                  loading={categories.isLoading}
                  options={(categories.data ?? []).map((category) => ({
                    value: category.id,
                    label: category.name,
                  }))}
                  allowClear
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item label="سرویس" required>
                <Select
                  placeholder={categoryId ? "انتخاب سرویس" : "ابتدا گروه را انتخاب کنید"}
                  value={serviceId}
                  onChange={setServiceId}
                  disabled={!categoryId}
                  loading={services.isLoading}
                  showSearch
                  optionFilterProp="label"
                  options={(services.data?.items ?? [])
                    .filter((item) => !categoryId || item.categoryId === categoryId)
                    .map((item) => ({
                      value: item.id,
                      label: `${item.name} (${item.code})`,
                    }))}
                  allowClear
                />
              </Form.Item>
            </Col>
          </Row>

          {service.isLoading && <Spin />}
          {serviceId && fields.length === 0 && (
            <Empty description="برای این سرویس فیلد مشخصات تعریف نشده است." />
          )}

          {serviceId && (
            <>
              <Form.Item label="نوع همکاری">
                <Select
                  value={(attributes.cooperation_type as string | undefined) ?? "invoice"}
                  onChange={(value) => setAttribute("cooperation_type", value)}
                  options={[
                    { value: "invoice", label: "فاکتوری" },
                    { value: "contract", label: "قرارداد" },
                  ]}
                />
              </Form.Item>
              <Form.Item label="دوره صدور فاکتور">
                <Select
                  value={Number(attributes.billing_period_months ?? 1)}
                  onChange={(value) => setAttribute("billing_period_months", value)}
                  options={[
                    { value: 1, label: "یک ماه" },
                    { value: 3, label: "سه ماه" },
                    { value: 6, label: "شش ماه" },
                    { value: 12, label: "یک سال" },
                  ]}
                />
              </Form.Item>
              <Typography.Title level={5}>مشخصات فنی</Typography.Title>
              <Row gutter={16}>
                {fields
                  .filter((field) => isVisible(field, attributes))
                  .map((field) => (
                    <Col xs={24} md={12} key={field.key}>
                      <Form.Item
                        label={field.label}
                        required={field.required}
                        style={{ marginBottom: 16 }}
                      >
                        <AttributeFieldInput
                          field={field}
                          value={attributes[field.key]}
                          onChange={(value) => setAttribute(field.key, value)}
                        />
                      </Form.Item>
                    </Col>
                  ))}
              </Row>

              <Form.Item label="نیاز به امکان‌سنجی">
                <Switch
                  checked={requiresFeasibility}
                  onChange={(checked) => {
                    setRequiresFeasibility(checked);
                    if (checked && !attributes.substrate) {
                      setAttribute("substrate", "wireless");
                    }
                  }}
                  checkedChildren="بله"
                  unCheckedChildren="خیر"
                />
              </Form.Item>
            </>
          )}

          <Space style={{ marginTop: 12 }}>
            <Button
              type="primary"
              onClick={() => void submit()}
              loading={create.isPending}
              disabled={!serviceId}
            >
              ثبت درخواست
            </Button>
            <Button onClick={() => navigate(`/customers/${id}`)}>انصراف</Button>
          </Space>
        </Form>
      </Card>
    </Space>
  );
}
