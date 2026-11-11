import { useEffect, useState } from "react";
import {
  ArrowRightOutlined,
  LinkOutlined,
  PlusOutlined,
  SaveOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Descriptions,
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
  Tabs,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate, useParams } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import {
  billingPeriodLabels,
  pricingModeLabels,
  useAddServicePrice,
  useCatalogService,
  useDeactivateCatalogAddon,
  useLinkServiceForm,
  usePriceHistory,
  useServiceAddons,
  useServiceForms,
  useServiceStatusAction,
  useSetAttributeSchema,
  useUnlinkServiceForm,
  useUpdateCatalogService,
} from "@/modules/catalog/hooks/useCatalog";
import type {
  BillingPeriod,
  PricingMode,
  ServiceAddon,
  ServiceFormLink,
  ServicePrice,
} from "@/modules/catalog/types";
import { useFormTemplates } from "@/modules/forms/hooks/useForms";
import { formatCurrency } from "@/shared/utils/format";

interface ServiceForm {
  code: string;
  name: string;
  categoryId: string;
  serviceType: string;
  unit: string;
  pricingMode: PricingMode;
  billingPeriod: BillingPeriod;
  commissionPercent?: number;
  workflowDefinitionId?: string;
  isActive: boolean;
}

interface PriceForm {
  price: number;
  effectiveFrom: { toISOString: () => string };
  currency: string;
}

export function ServiceEditor() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const canManage = useAuthStore((state) => state.hasPermission("Catalog.Service.Manage"));
  const service = useCatalogService(id);
  const addons = useServiceAddons(id);
  const forms = useServiceForms(id);
  const prices = usePriceHistory(id);
  const formTemplates = useFormTemplates(true);
  const update = useUpdateCatalogService(id);
  const setSchemaMutation = useSetAttributeSchema(id);
  const addPrice = useAddServicePrice(id);
  const statusAction = useServiceStatusAction(id);
  const linkForm = useLinkServiceForm(id);
  const unlinkForm = useUnlinkServiceForm(id);
  const deactivateAddon = useDeactivateCatalogAddon();
  const [form] = Form.useForm<ServiceForm>();
  const [priceForm] = Form.useForm<PriceForm>();
  const [linkFormForm] = Form.useForm<{
    formDefinitionId: string;
    isRequired: boolean;
    sortOrder: number;
  }>();
  const [schema, setSchema] = useState("");
  const [priceOpen, setPriceOpen] = useState(false);
  const [linkOpen, setLinkOpen] = useState(false);

  useEffect(() => {
    if (!service.data) return;
    form.setFieldsValue({
      code: service.data.code,
      name: service.data.name,
      categoryId: service.data.categoryId,
      serviceType: service.data.serviceType,
      unit: service.data.unit,
      pricingMode: service.data.pricingMode,
      billingPeriod: service.data.billingPeriod,
      commissionPercent: service.data.commissionPercent ?? undefined,
      workflowDefinitionId: service.data.workflowDefinitionId ?? undefined,
      isActive: service.data.isActive,
    });
    setSchema(service.data.attributeSchemaJson ?? "");
  }, [service.data, form]);

  if (service.isLoading) {
    return <div className="centered-state"><Spin size="large" /></div>;
  }
  if (service.isError || !service.data) {
    return (
      <Card variant="borderless" className="settings-section-card">
        <Typography.Title level={3}>سرویس یافت نشد</Typography.Title>
        <Button onClick={() => navigate("/catalog/services")}>بازگشت</Button>
      </Card>
    );
  }

  const data = service.data;
  const editable = data.canEdit && canManage;

  const save = async (values: ServiceForm) => {
    try {
      await update.mutateAsync({
        ...values,
        description: data.description,
        attributeSchemaJson: schema || null,
      });
      message.success("سرویس به‌روزرسانی شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const saveSchema = async () => {
    try {
      await setSchemaMutation.mutateAsync(schema.trim() || null);
      message.success("AttributeSchema ذخیره شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش می‌شود.
    }
  };

  const submitPrice = async (values: PriceForm) => {
    try {
      await addPrice.mutateAsync({
        price: values.price,
        effectiveFromUtc: values.effectiveFrom.toISOString(),
        currency: values.currency,
      });
      message.success("قیمت جدید ثبت شد.");
      setPriceOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const submitLink = async (values: {
    formDefinitionId: string;
    isRequired: boolean;
    sortOrder: number;
  }) => {
    try {
      await linkForm.mutateAsync(values);
      message.success("فرم متصل شد.");
      setLinkOpen(false);
      linkFormForm.resetFields();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const priceColumns: TableColumnsType<ServicePrice> = [
    { title: "قیمت", dataIndex: "price", render: (value: number) => formatCurrency(value) },
    { title: "شروع", dataIndex: "effectiveFromJalali" },
    { title: "پایان", dataIndex: "effectiveToJalali", render: (value: string | null) => value || "باز" },
    { title: "ارز", dataIndex: "currency", width: 90 },
  ];

  const addonColumns: TableColumnsType<ServiceAddon> = [
    { title: "کد", dataIndex: "code", width: 130 },
    { title: "نام", dataIndex: "name" },
    { title: "قیمت", dataIndex: "price", width: 130, render: (value: number) => formatCurrency(value) },
    {
      title: "نوع",
      dataIndex: "pricingMode",
      width: 100,
      render: (value: string) => (value === "PerUnit" ? "واحدی" : "ثابت"),
    },
    {
      title: "وضعیت",
      dataIndex: "isActive",
      width: 100,
      render: (value: boolean) => (value ? "فعال" : "غیرفعال"),
    },
    {
      title: "عملیات",
      width: 100,
      render: (_value, record) =>
        canManage && record.isActive ? (
          <Popconfirm
            title="آیتم جانبی غیرفعال شود؟"
            onConfirm={async () => {
              try {
                await deactivateAddon.mutateAsync(record.id);
                message.success("آیتم جانبی غیرفعال شد.");
              } catch {
                // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
              }
            }}
          >
            <Button type="link" danger>غیرفعال</Button>
          </Popconfirm>
        ) : "-",
    },
  ];

  const formColumns: TableColumnsType<ServiceFormLink> = [
    { title: "کد فرم", dataIndex: "formCode", render: (value: string | null) => value || "-" },
    { title: "عنوان", dataIndex: "formTitle" },
    { title: "الزامی", dataIndex: "isRequired", render: (value: boolean) => (value ? "بله" : "خیر") },
    {
      title: "عملیات",
      width: 100,
      render: (_value, record) =>
        canManage ? (
          <Button
            type="link"
            danger
            onClick={async () => {
              try {
                await unlinkForm.mutateAsync(record.formDefinitionId);
                message.success("اتصال فرم حذف شد.");
              } catch {
                // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
              }
            }}
          >
            حذف
          </Button>
        ) : "-",
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <Space size="middle" align="start">
          <Button type="text" icon={<ArrowRightOutlined />} onClick={() => navigate("/catalog/services")} />
          <div>
            <Space size="small" wrap>
              <Typography.Title level={2} style={{ margin: 0 }}>{data.name}</Typography.Title>
              <Tag color={data.isActive ? "green" : "default"}>{data.isActive ? "فعال" : "غیرفعال"}</Tag>
              <Tag>{pricingModeLabels[data.pricingMode]}</Tag>
            </Space>
            <Typography.Text type="secondary" dir="ltr">{data.code}</Typography.Text>
          </div>
        </Space>
        {canManage && (
          <Space wrap>
            <Popconfirm
              title={data.isActive ? "سرویس غیرفعال شود؟" : "سرویس فعال شود؟"}
              onConfirm={async () => {
                try {
                  await statusAction.mutateAsync(data.isActive ? "deactivate" : "activate");
                  message.success("وضعیت سرویس تغییر کرد.");
                } catch {
                  // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
                }
              }}
            >
              <Button>{data.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}</Button>
            </Popconfirm>
            <Button type="primary" icon={<SaveOutlined />} disabled={!editable} onClick={() => form.submit()} loading={update.isPending}>
              ذخیره
            </Button>
          </Space>
        )}
      </div>

      {!data.canEdit && (
        <Alert
          type="warning"
          showIcon
          message="این سرویس قابل ویرایش نیست"
          description={data.editBlockedReason || "برای ویرایش، نسخهٔ جدید بسازید."}
        />
      )}

      <Form form={form} layout="vertical" onFinish={save} disabled={!editable}>
        <Card variant="borderless" className="settings-section-card">
          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item name="code" label="کد" rules={[{ required: true }]}>
                <Input dir="ltr" />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="name" label="نام" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="categoryId" label="دسته‌بندی">
                <Input dir="ltr" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={24} md={6}>
              <Form.Item name="pricingMode" label="حالت قیمت‌گذاری">
                <Select options={Object.entries(pricingModeLabels).map(([value, label]) => ({ value, label }))} />
              </Form.Item>
            </Col>
            <Col xs={24} md={6}>
              <Form.Item name="billingPeriod" label="دوره">
                <Select options={Object.entries(billingPeriodLabels).map(([value, label]) => ({ value, label }))} />
              </Form.Item>
            </Col>
            <Col xs={24} md={6}>
              <Form.Item name="commissionPercent" label="درصد پورسانت">
                <InputNumber min={0} max={100} style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} md={6}>
              <Form.Item name="workflowDefinitionId" label="WorkflowDefinitionId">
                <Input dir="ltr" />
              </Form.Item>
            </Col>
          </Row>
          <Descriptions column={1} size="small">
            <Descriptions.Item label="قیمت فعلی">{formatCurrency(data.currentPrice ?? 0)}</Descriptions.Item>
            <Descriptions.Item label="دسته">{data.categoryName}</Descriptions.Item>
          </Descriptions>
        </Card>
      </Form>

      <Tabs
        items={[
          {
            key: "schema",
            label: "فیلدهای داینامیک",
            children: (
              <Card variant="borderless" className="settings-section-card">
                <Space direction="vertical" size={12} style={{ width: "100%" }}>
                  <Typography.Text type="secondary">
                    SchemaJson با قالب فرم‌ساز؛ هنگام ثبت سفارش به‌صورت فیلدهای ورودی نمایش داده می‌شود.
                  </Typography.Text>
                  <Input.TextArea
                    rows={12}
                    dir="ltr"
                    value={schema}
                    onChange={(event) => setSchema(event.target.value)}
                    disabled={!editable}
                    placeholder='{"fields":[{"key":"bandwidth_mb","label":"پهنای باند","type":"number","required":true}]}'
                  />
                  <Button onClick={() => void saveSchema()} disabled={!editable} loading={setSchemaMutation.isPending}>
                    ذخیره AttributeSchema
                  </Button>
                </Space>
              </Card>
            ),
          },
          {
            key: "prices",
            label: "تاریخچه قیمت",
            children: (
              <Card
                variant="borderless"
                className="settings-section-card"
                extra={<Button icon={<PlusOutlined />} onClick={() => setPriceOpen(true)} disabled={!canManage}>قیمت جدید</Button>}
              >
                <Table<ServicePrice> rowKey="id" size="small" columns={priceColumns} dataSource={prices.data ?? []} pagination={false} />
              </Card>
            ),
          },
          {
            key: "addons",
            label: "آیتم‌های جانبی",
            children: (
              <Card variant="borderless" className="settings-section-card">
                <Table<ServiceAddon> rowKey="id" size="small" columns={addonColumns} dataSource={addons.data ?? []} pagination={false} />
              </Card>
            ),
          },
          {
            key: "forms",
            label: "فرم‌های متصل",
            children: (
              <Card
                variant="borderless"
                className="settings-section-card"
                extra={<Button icon={<LinkOutlined />} onClick={() => setLinkOpen(true)} disabled={!canManage}>اتصال فرم</Button>}
              >
                <Table<ServiceFormLink> rowKey="id" size="small" columns={formColumns} dataSource={forms.data ?? []} pagination={false} />
              </Card>
            ),
          },
        ]}
      />

      <Modal
        title="ثبت قیمت جدید"
        open={priceOpen}
        onCancel={() => setPriceOpen(false)}
        onOk={() => priceForm.submit()}
        okText="ثبت"
        cancelText="انصراف"
        confirmLoading={addPrice.isPending}
        forceRender
      >
        <Form form={priceForm} layout="vertical" onFinish={submitPrice} initialValues={{ currency: "IRR" }}>
          <Form.Item name="price" label="قیمت" rules={[{ required: true }]}>
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="effectiveFrom" label="شروع اعتبار" rules={[{ required: true }]}>
            <DatePicker showTime style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="currency" label="ارز">
            <Input dir="ltr" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="اتصال فرم به سرویس"
        open={linkOpen}
        onCancel={() => setLinkOpen(false)}
        onOk={() => linkFormForm.submit()}
        okText="اتصال"
        cancelText="انصراف"
        confirmLoading={linkForm.isPending}
        forceRender
      >
        <Form form={linkFormForm} layout="vertical" onFinish={submitLink} initialValues={{ isRequired: true, sortOrder: 1 }}>
          <Form.Item name="formDefinitionId" label="فرم" rules={[{ required: true }]}>
            <Select
              showSearch
              optionFilterProp="label"
              options={(formTemplates.data ?? []).map((template) => ({
                value: template.id,
                label: `${template.title} (${template.code})`,
              }))}
            />
          </Form.Item>
          <Form.Item name="isRequired" label="الزامی">
            <Select options={[{ value: true, label: "بله" }, { value: false, label: "خیر" }]} />
          </Form.Item>
          <Form.Item name="sortOrder" label="ترتیب">
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
