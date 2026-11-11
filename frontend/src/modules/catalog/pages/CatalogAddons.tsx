import { useState } from "react";
import { PlusOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useAuthStore } from "@/modules/auth/store";
import {
  useCatalogAddons,
  useCatalogServices,
  useCreateCatalogAddon,
  useDeactivateCatalogAddon,
  useUpdateCatalogAddon,
} from "@/modules/catalog/hooks/useCatalog";
import type { AddonPricingMode, ServiceAddon } from "@/modules/catalog/types";
import { formatCurrency } from "@/shared/utils/format";

interface AddonForm {
  serviceId?: string;
  code: string;
  name: string;
  price: number;
  unit: string;
  pricingMode: AddonPricingMode;
  sortOrder: number;
  description?: string;
  isActive: boolean;
}

const unitOptions = [
  { value: "Piece", label: "عدد" },
  { value: "Monthly", label: "ماه" },
  { value: "Package", label: "بسته" },
  { value: "RackUnit", label: "یونیت رک" },
];

export function CatalogAddons() {
  const { message } = App.useApp();
  const canManage = useAuthStore((state) => state.hasPermission("Catalog.AddOn.Manage"));
  const addons = useCatalogAddons();
  const services = useCatalogServices({ page: 1, pageSize: 100 });
  const create = useCreateCatalogAddon();
  const update = useUpdateCatalogAddon();
  const deactivate = useDeactivateCatalogAddon();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<ServiceAddon | null>(null);
  const [form] = Form.useForm<AddonForm>();

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({
      pricingMode: "Fixed",
      unit: "Piece",
      isActive: true,
      sortOrder: 0,
    });
    setOpen(true);
  };

  const openEdit = (addon: ServiceAddon) => {
    setEditing(addon);
    form.setFieldsValue({
      serviceId: addon.serviceId ?? undefined,
      code: addon.code ?? "",
      name: addon.name,
      price: addon.price,
      unit: addon.unit,
      pricingMode: addon.pricingMode,
      sortOrder: addon.sortOrder,
      description: addon.description ?? undefined,
      isActive: addon.isActive,
    });
    setOpen(true);
  };

  const submit = async (values: AddonForm) => {
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, ...values, serviceId: values.serviceId || null });
        message.success("آیتم جانبی به‌روزرسانی شد.");
      } else {
        await create.mutateAsync({ ...values, serviceId: values.serviceId || null });
        message.success("آیتم جانبی ایجاد شد.");
      }
      setOpen(false);
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const columns: TableColumnsType<ServiceAddon> = [
    { title: "کد", dataIndex: "code", width: 140, render: (value: string) => <span dir="ltr">{value}</span> },
    { title: "نام", dataIndex: "name" },
    {
      title: "دامنه",
      key: "scope",
      width: 150,
      render: (_value, record) =>
        record.serviceId
          ? <Tag color="blue" bordered={false}>اختصاصی</Tag>
          : <Tag bordered={false}>عمومی</Tag>,
    },
    { title: "قیمت", dataIndex: "price", width: 130, render: (value: number) => formatCurrency(value) },
    {
      title: "وضعیت",
      dataIndex: "isActive",
      width: 100,
      render: (value: boolean) => (
        <Tag color={value ? "green" : "default"} bordered={false}>{value ? "فعال" : "غیرفعال"}</Tag>
      ),
    },
    {
      title: "عملیات",
      width: 180,
      render: (_value, record) =>
        canManage ? (
          <Space size="small">
            <Button type="link" onClick={() => openEdit(record)}>ویرایش</Button>
            {record.isActive && (
              <Popconfirm
                title="آیتم جانبی غیرفعال شود؟"
                onConfirm={async () => {
                  try {
                    await deactivate.mutateAsync(record.id);
                    message.success("آیتم جانبی غیرفعال شد.");
                  } catch {
                    // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
                  }
                }}
              >
                <Button type="link" danger>غیرفعال</Button>
              </Popconfirm>
            )}
          </Space>
        ) : "-",
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>آیتم‌های جانبی</Typography.Title>
          <Typography.Text type="secondary">Addonهای عمومی و اختصاصی سرویس‌ها</Typography.Text>
        </div>
        {canManage && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Addon جدید</Button>
        )}
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<ServiceAddon>
          rowKey="id"
          columns={columns}
          dataSource={addons.data ?? []}
          loading={addons.isLoading}
          className="module-table"
          scroll={{ x: 830 }}
          locale={{ emptyText: "Addonی تعریف نشده است." }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
        />
      </Card>

      <Modal
        title={editing ? "ویرایش Addon" : "Addon جدید"}
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        okText="ذخیره"
        cancelText="انصراف"
        confirmLoading={create.isPending || update.isPending}
        width={650}
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
          <Form.Item name="serviceId" label="سرویس (خالی = عمومی)">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              options={(services.data?.items ?? []).map((service) => ({
                value: service.id,
                label: `${service.name} (${service.code})`,
              }))}
            />
          </Form.Item>
          <Space size={12} style={{ width: "100%" }} align="start">
            <Form.Item name="price" label="قیمت" rules={[{ required: true }]} style={{ flex: 1 }}>
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>
            <Form.Item name="unit" label="واحد" style={{ flex: 1 }}>
              <Select options={unitOptions} />
            </Form.Item>
            <Form.Item name="pricingMode" label="نوع" style={{ flex: 1 }}>
              <Select options={[{ value: "Fixed", label: "ثابت" }, { value: "PerUnit", label: "واحدی" }]} />
            </Form.Item>
          </Space>
          <Space size={12} style={{ width: "100%" }} align="start">
            <Form.Item name="sortOrder" label="ترتیب" style={{ flex: 1 }}>
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>
            <Form.Item name="isActive" label="فعال" style={{ flex: 1 }}>
              <Select options={[{ value: true, label: "بله" }, { value: false, label: "خیر" }]} />
            </Form.Item>
          </Space>
          <Form.Item name="description" label="توضیحات">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
