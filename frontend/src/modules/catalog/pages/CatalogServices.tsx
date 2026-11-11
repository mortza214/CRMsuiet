import { useState } from "react";
import { EditOutlined, PlusOutlined, SearchOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
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
  pricingModeLabels,
  useCatalogServices,
  useCreateCatalogService,
  useServiceCategories,
} from "@/modules/catalog/hooks/useCatalog";
import type { PricingMode, ServiceListItem } from "@/modules/catalog/types";
import { formatCurrency } from "@/shared/utils/format";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

interface CreateServiceForm {
  code: string;
  name: string;
  categoryId: string;
  serviceType: string;
  unit: string;
  pricingMode: PricingMode;
  billingPeriod: string;
  commissionPercent?: number;
  initialPrice?: number;
  isActive: boolean;
}

const serviceTypeLabels: Record<string, string> = {
  BandwidthDedicated: "پهنای باند اختصاصی",
  BandwidthShared: "پهنای باند اشتراکی",
  DedicatedServer: "سرور اختصاصی",
  SharedServer: "سرور اشتراکی",
  RackSpace: "فضای رک",
  PhysicalProduct: "محصول فیزیکی",
  Other: "سایر",
};

const unitLabels: Record<string, string> = {
  BandwidthMbps: "مگابیت",
  Package: "بسته",
  RackUnit: "یونیت رک",
  Piece: "عدد",
  Monthly: "ماه",
};

export function CatalogServices() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const canManage = useAuthStore((state) => state.hasPermission("Catalog.Service.Manage"));
  const [search, setSearch] = useState("");
  const [pricingMode, setPricingMode] = useState<PricingMode | undefined>();
  const [createOpen, setCreateOpen] = useState(false);
  const [form] = Form.useForm<CreateServiceForm>();
  const debouncedSearch = useDebouncedValue(search, 350);
  const services = useCatalogServices({
    page: 1,
    pageSize: 100,
    search: debouncedSearch,
    pricingMode,
  });
  const categories = useServiceCategories();
  const create = useCreateCatalogService();

  const submit = async (values: CreateServiceForm) => {
    try {
      const service = await create.mutateAsync({
        ...values,
        attributeSchemaJson: null,
        description: null,
      });
      message.success("سرویس ایجاد شد.");
      setCreateOpen(false);
      navigate(`/catalog/services/${service.id}`);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<ServiceListItem> = [
    { title: "کد", dataIndex: "code", width: 140, render: (value: string) => <span dir="ltr">{value}</span> },
    { title: "نام", dataIndex: "name" },
    {
      title: "قیمت‌گذاری",
      dataIndex: "pricingMode",
      width: 120,
      render: (value: PricingMode) => <Tag bordered={false}>{pricingModeLabels[value]}</Tag>,
    },
    { title: "قیمت فعلی", dataIndex: "currentPrice", width: 150, render: (value: number | null) => formatCurrency(value ?? 0) },
    {
      title: "وضعیت",
      dataIndex: "isActive",
      width: 100,
      render: (value: boolean) => (
        <Tag color={value ? "green" : "default"} bordered={false}>
          {value ? "فعال" : "غیرفعال"}
        </Tag>
      ),
    },
    {
      title: "عملیات",
      width: 130,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EditOutlined />}
          onClick={() => navigate(`/catalog/services/${record.id}`)}
        >
          {canManage ? "ویرایش" : "مشاهده"}
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>کاتالوگ سرویس‌ها</Typography.Title>
          <Typography.Text type="secondary">
            قیمت‌گذاری، فیلدهای داینامیک، Addon و فرم‌های متصل
          </Typography.Text>
        </div>
        <Space wrap>
          <Button onClick={() => navigate("/catalog/prices")}>قیمت‌ها</Button>
          <Button onClick={() => navigate("/catalog/addons")}>Addonها</Button>
          {canManage && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                form.resetFields();
                form.setFieldsValue({
                  pricingMode: "UnitBased",
                  billingPeriod: "Monthly",
                  isActive: false,
                  serviceType: "BandwidthDedicated",
                  unit: "BandwidthMbps",
                });
                setCreateOpen(true);
              }}
            >
              سرویس جدید
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
          placeholder="جستجوی سرویس..."
          className="toolbar-search"
        />
        <Select
          allowClear
          value={pricingMode}
          onChange={setPricingMode}
          placeholder="حالت قیمت‌گذاری"
          style={{ minWidth: 170 }}
          options={Object.entries(pricingModeLabels).map(([value, label]) => ({ value, label }))}
        />
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<ServiceListItem>
          rowKey="id"
          columns={columns}
          dataSource={services.data?.items ?? []}
          loading={services.isLoading || services.isFetching}
          scroll={{ x: 850 }}
          className="module-table"
          locale={{ emptyText: "سرویسی تعریف نشده است." }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
        />
      </Card>

      <Modal
        title="تعریف سرویس جدید"
        open={createOpen}
        onCancel={() => setCreateOpen(false)}
        onOk={() => form.submit()}
        okText="ایجاد"
        cancelText="انصراف"
        confirmLoading={create.isPending}
        width={680}
        forceRender
      >
        <Form form={form} layout="vertical" onFinish={submit}>
          <Space size={12} style={{ width: "100%" }} align="start">
            <Form.Item name="code" label="کد" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Input dir="ltr" />
            </Form.Item>
            <Form.Item name="name" label="نام" rules={[{ required: true }]} style={{ flex: 2 }}>
              <Input />
            </Form.Item>
          </Space>
          <Form.Item name="categoryId" label="دسته‌بندی" rules={[{ required: true }]}>
            <Select
              options={(categories.data ?? []).map((category) => ({
                value: category.id,
                label: category.name,
              }))}
            />
          </Form.Item>
          <Space size={12} style={{ width: "100%" }} align="start">
            <Form.Item name="serviceType" label="نوع سرویس" style={{ flex: 1 }}>
              <Select
                options={Object.entries(serviceTypeLabels).map(([value, label]) => ({ value, label }))}
              />
            </Form.Item>
            <Form.Item name="unit" label="واحد" style={{ flex: 1 }}>
              <Select
                options={Object.entries(unitLabels).map(([value, label]) => ({ value, label }))}
              />
            </Form.Item>
          </Space>
          <Space size={12} style={{ width: "100%" }} align="start">
            <Form.Item name="pricingMode" label="حالت قیمت‌گذاری" style={{ flex: 1 }}>
              <Select
                options={Object.entries(pricingModeLabels).map(([value, label]) => ({ value, label }))}
              />
            </Form.Item>
            <Form.Item name="billingPeriod" label="دوره" style={{ flex: 1 }}>
              <Select
                options={[
                  { value: "Monthly", label: "ماهانه" },
                  { value: "OneTime", label: "یک‌بار" },
                  { value: "Yearly", label: "سالانه" },
                ]}
              />
            </Form.Item>
          </Space>
          <Space size={12} style={{ width: "100%" }} align="start">
            <Form.Item name="commissionPercent" label="درصد پورسانت" style={{ flex: 1 }}>
              <InputNumber min={0} max={100} style={{ width: "100%" }} />
            </Form.Item>
            <Form.Item name="initialPrice" label="قیمت اولیه" style={{ flex: 1 }}>
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>
            <Form.Item name="isActive" label="فعال" style={{ flex: 1 }}>
              <Select
                options={[
                  { value: true, label: "بله" },
                  { value: false, label: "خیر" },
                ]}
              />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </Space>
  );
}
