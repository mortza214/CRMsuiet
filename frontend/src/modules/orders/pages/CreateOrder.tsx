import { useEffect, useMemo, useState } from "react";
import {
  ArrowLeftOutlined,
  DeleteOutlined,
  PlusOutlined,
  ShoppingOutlined,
  UserOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Checkbox,
  Col,
  Divider,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Steps,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import {
  useCreateOrder,
  useOrderAddons,
  useOrderCustomers,
  useOrderPricePreview,
  useOrderServiceAddons,
  useOrderServiceDetail,
  useOrderServices,
} from "@/modules/orders/hooks/useOrders";
import type {
  BillingCycle,
  CatalogAddon,
  CatalogService,
  OrderDraftItem,
} from "@/modules/orders/types";
import type { CustomerListItem } from "@/modules/customers/types";
import { formatCurrency } from "@/shared/utils/format";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

interface CreateOrderForm {
  customerId: string;
  items: OrderDraftItem[];
  notes?: string;
}

interface DraftTableRow extends OrderDraftItem {
  fieldName: number;
}

const cycleLabels: Record<BillingCycle, string> = {
  OneTime: "یک‌بار",
  Monthly: "ماهیانه",
  Yearly: "سالانه",
};

export function CreateOrder() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [step, setStep] = useState(0);
  const [customerSearch, setCustomerSearch] = useState("");
  const [serviceModalOpen, setServiceModalOpen] = useState(false);
  const [addonModalOpen, setAddonModalOpen] = useState(false);
  const [form] = Form.useForm<CreateOrderForm>();
  const customerId = Form.useWatch("customerId", form);
  const items = (Form.useWatch("items", form) ?? []) as OrderDraftItem[];
  const debouncedCustomerSearch = useDebouncedValue(customerSearch, 350);
  const customers = useOrderCustomers(debouncedCustomerSearch);
  const createOrder = useCreateOrder();

  const selectedCustomer = useMemo(
    () => customers.data?.find((customer) => customer.id === customerId),
    [customerId, customers.data],
  );

  const total = items.reduce(
    (sum, item) => sum + (item.effectivePrice ?? item.quantity * item.unitPrice),
    0,
  );

  const addItem = (item: OrderDraftItem) => {
    form.setFieldValue("items", [...items, item]);
  };

  const removeItem = (fieldName: number) => {
    form.setFieldValue(
      "items",
      items.filter((_item, index) => index !== fieldName),
    );
  };

  const updateQuantity = (index: number, quantity: number | null) => {
    const next = items.map((item, itemIndex) =>
      itemIndex === index
        ? { ...item, quantity: Math.max(1, Number(quantity ?? 1)) }
        : item,
    );
    form.setFieldValue("items", next);
  };

  const goNext = () => {
    if (step === 0 && !customerId) {
      message.warning("ابتدا مشتری را انتخاب کنید.");
      return;
    }

    if (step === 1 && items.length === 0) {
      message.warning("حداقل یک سرویس یا آیتم جانبی اضافه کنید.");
      return;
    }

    setStep((value) => Math.min(2, value + 1));
  };

  const submit = async (values: CreateOrderForm) => {
    if (!values.customerId || values.items.length === 0) {
      message.warning("اطلاعات سفارش کامل نیست.");
      return;
    }

    try {
      const order = await createOrder.mutateAsync({
        customerId: values.customerId,
        orderType: "New",
        notes: values.notes?.trim() || null,
        items: values.items.map((item) => ({
          serviceId: item.serviceId ?? null,
          dataCenterItemId: null,
          addonId: item.addonId ?? null,
          quantity: item.quantity,
          billingCycle: item.billingCycle,
          attributesJson: item.attributesJson ?? null,
          addons: (item.addons ?? []).map((addon) => ({
            addonId: addon.addonId,
            quantity: addon.quantity,
          })),
        })),
      });

      message.success("سفارش با موفقیت ثبت شد.");
      navigate(`/orders/${order.id}`, { replace: true });
    } catch {
      // پیام خطای API توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const draftColumns: TableColumnsType<DraftTableRow> = [
    { title: "آیتم", dataIndex: "itemName", key: "itemName" },
    {
      title: "تعداد / مقدار",
      key: "quantity",
      width: 150,
      render: (_value, record) => (
        <InputNumber
          min={1}
          value={record.quantity}
          onChange={(value) => updateQuantity(record.fieldName, value)}
        />
      ),
    },
    {
      title: "قیمت واحد",
      dataIndex: "unitPrice",
      key: "unitPrice",
      width: 150,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "دوره",
      dataIndex: "billingCycle",
      key: "billingCycle",
      width: 100,
      render: (value: BillingCycle) => cycleLabels[value],
    },
    {
      title: "جمع",
      key: "total",
      width: 160,
      render: (_value, record) =>
        formatCurrency(record.effectivePrice ?? record.quantity * record.unitPrice),
    },
    {
      title: "حذف",
      key: "remove",
      width: 70,
      render: (_value, record) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          onClick={() => removeItem(record.fieldName)}
        />
      ),
    },
  ];

  const draftRows: DraftTableRow[] = items.map((item, index) => ({
    ...item,
    fieldName: index,
  }));

  return (
    <div className="order-create-page">
      <div className="page-heading">
        <div>
          <Button
            type="text"
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate("/orders")}
          >
            بازگشت به سفارش‌ها
          </Button>
          <Typography.Title level={2}>ثبت سفارش جدید</Typography.Title>
          <Typography.Text type="secondary">
            انتخاب مشتری، افزودن سرویس‌ها و تأیید نهایی سفارش
          </Typography.Text>
        </div>
      </div>

      <Card className="wizard-card" variant="borderless">
        <Steps
          current={step}
          items={[
            { title: "انتخاب مشتری", icon: <UserOutlined /> },
            { title: "سرویس‌ها و آیتم‌ها", icon: <ShoppingOutlined /> },
            { title: "تأیید و ثبت" },
          ]}
        />
      </Card>

      <Form
        form={form}
        layout="vertical"
        initialValues={{ items: [] }}
        onFinish={submit}
      >
        <div hidden={step !== 0}>
          <Card className="wizard-card" variant="borderless">
            <Form.Item
              name="customerId"
              label="مشتری"
              rules={[{ required: true, message: "مشتری را انتخاب کنید." }]}
            >
              <Select
                showSearch
                filterOption={false}
                onSearch={setCustomerSearch}
                placeholder="جستجو و انتخاب مشتری..."
                loading={customers.isLoading}
                options={(customers.data ?? []).map((customer) => ({
                  value: customer.id,
                  label: `${customer.displayName} — ${customer.mobile}`,
                }))}
              />
            </Form.Item>

            {selectedCustomer && (
              <CustomerSummary customer={selectedCustomer} />
            )}
          </Card>
        </div>

        <div hidden={step !== 1}>
          <Card className="wizard-card" variant="borderless">
            <Space size="middle" wrap className="wizard-actions">
              <Button
                type="primary"
                ghost
                icon={<PlusOutlined />}
                onClick={() => setServiceModalOpen(true)}
              >
                افزودن سرویس
              </Button>
              <Button
                icon={<PlusOutlined />}
                onClick={() => setAddonModalOpen(true)}
              >
                افزودن آیتم جانبی
              </Button>
            </Space>

            <Form.List name="items">
              {() => (
                <Table<DraftTableRow>
                  rowKey="key"
                  columns={draftColumns}
                  dataSource={draftRows}
                  pagination={false}
                  className="module-table draft-table"
                  scroll={{ x: 760 }}
                  locale={{ emptyText: "هنوز آیتمی اضافه نشده است." }}
                />
              )}
            </Form.List>

            <div className="order-total-preview">
              <Typography.Text type="secondary">
                مبلغ کل (پیش‌نمایش)
              </Typography.Text>
              <Typography.Title level={3}>{formatCurrency(total)}</Typography.Title>
              <Typography.Text type="secondary" className="preview-note">
                محاسبه نهایی قیمت‌ها در سرور و بر اساس Catalog انجام می‌شود.
              </Typography.Text>
            </div>
          </Card>
        </div>

        <div hidden={step !== 2}>
          <Card className="wizard-card" variant="borderless">
            <Typography.Title level={4}>خلاصه سفارش</Typography.Title>

            <Row gutter={[24, 16]} className="order-summary">
              <Col xs={24} md={8}>
                <SummaryItem label="مشتری" value={selectedCustomer?.displayName ?? "-"} />
              </Col>
              <Col xs={24} md={8}>
                <SummaryItem label="تعداد آیتم" value={String(items.length)} />
              </Col>
              <Col xs={24} md={8}>
                <SummaryItem label="مبلغ پیش‌نمایش" value={formatCurrency(total)} />
              </Col>
            </Row>

            <Divider />

            <Form.Item name="notes" label="توضیحات سفارش">
              <Input.TextArea
                rows={4}
                placeholder="توضیحات تکمیلی درباره سفارش..."
              />
            </Form.Item>
          </Card>
        </div>

        <div className="wizard-footer">
          <Button disabled={step === 0} onClick={() => setStep((value) => value - 1)}>
            مرحله قبل
          </Button>

          {step < 2 ? (
            <Button type="primary" onClick={goNext}>
              مرحله بعد
            </Button>
          ) : (
            <Button
              type="primary"
              htmlType="submit"
              loading={createOrder.isPending}
            >
              ثبت نهایی سفارش
            </Button>
          )}
        </div>
      </Form>

      <ServicePickerModal
        open={serviceModalOpen}
        onClose={() => setServiceModalOpen(false)}
        onAdd={addItem}
      />

      <AddonPickerModal
        open={addonModalOpen}
        onClose={() => setAddonModalOpen(false)}
        onAdd={addItem}
      />
    </div>
  );
}

function CustomerSummary({ customer }: { customer: CustomerListItem }) {
  return (
    <div className="customer-summary-card">
      <Space direction="vertical" size={4}>
        <Typography.Text strong>{customer.displayName}</Typography.Text>
        <Typography.Text type="secondary" dir="ltr">
          {customer.mobile}
        </Typography.Text>
        <Space size="small">
          <Tag color={customer.type === "Legal" ? "green" : "blue"} bordered={false}>
            {customer.type === "Legal" ? "حقوقی" : "حقیقی"}
          </Tag>
          {customer.isReseller && <Tag color="purple">ریسلر</Tag>}
        </Space>
      </Space>
    </div>
  );
}

function SummaryItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <Typography.Text type="secondary">{label}</Typography.Text>
      <div className="summary-value">{value}</div>
    </div>
  );
}

interface ServicePickerModalProps {
  open: boolean;
  onClose: () => void;
  onAdd: (item: OrderDraftItem) => void;
}

interface AttributeField {
  key: string;
  label: string;
  type: string;
  required?: boolean;
  options?: Array<{ value: string; label: string }>;
}

function ServicePickerModal({
  open,
  onClose,
  onAdd,
}: ServicePickerModalProps) {
  const { message } = App.useApp();
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<CatalogService | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [cycle, setCycle] = useState<BillingCycle>("Monthly");
  const [attributes, setAttributes] = useState<Record<string, unknown>>({});
  const [addonState, setAddonState] = useState<
    Record<string, { selected: boolean; quantity: number }>
  >({});
  const debouncedSearch = useDebouncedValue(search, 350);
  const services = useOrderServices(debouncedSearch);
  const detail = useOrderServiceDetail(selected?.id);
  const addons = useOrderServiceAddons(selected?.id);
  const preview = useOrderPricePreview();
  const schemaFields = useMemo(
    () => parseAttributeSchema(detail.data?.attributeSchemaJson),
    [detail.data?.attributeSchemaJson],
  );
  const selectedAddons = (addons.data ?? []).filter(
    (addon) => addonState[addon.id]?.selected,
  );

  useEffect(() => {
    setAttributes({});
    setAddonState({});
    setQuantity(1);
  }, [selected?.id]);

  useEffect(() => {
    if (!selected) return;
    const addonPayload = Object.entries(addonState)
      .filter(([, value]) => value.selected)
      .map(([addonId, value]) => ({
        addonId,
        quantity: Math.max(1, value.quantity),
      }));
    preview.mutate({
      serviceId: selected.id,
      quantity,
      attributesJson: JSON.stringify(attributes),
      addons: addonPayload,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selected?.id, quantity, JSON.stringify(attributes), JSON.stringify(addonState)]);

  const columns: TableColumnsType<CatalogService> = [
    { title: "کد", dataIndex: "code", width: 120 },
    { title: "نام سرویس", dataIndex: "name" },
    {
      title: "قیمت فعلی",
      dataIndex: "currentPrice",
      width: 140,
      render: (value: number | null) => formatCurrency(value ?? 0),
    },
    { title: "واحد", dataIndex: "unit", width: 100 },
  ];

  const add = () => {
    if (!selected) {
      message.warning("یک سرویس انتخاب کنید.");
      return;
    }
    if (preview.data?.total === undefined) {
      message.warning("قیمت سرور قابل محاسبه نیست؛ فیلدهای لازم را کامل کنید.");
      return;
    }

    onAdd({
      key: crypto.randomUUID(),
      serviceId: selected.id,
      itemName: selected.name,
      quantity,
      unitPrice: preview.data.unitPrice,
      effectivePrice: preview.data.total,
      billingCycle: cycle,
      attributesJson: JSON.stringify(attributes),
      addons: selectedAddons.map((addon) => ({
        addonId: addon.id,
        quantity: addonState[addon.id]?.quantity ?? 1,
        name: addon.name,
        unitPrice: addon.price,
      })),
    });
    setSelected(null);
    setAttributes({});
    setAddonState({});
    setQuantity(1);
    onClose();
  };

  return (
    <Modal
      title="افزودن سرویس"
      open={open}
      onCancel={onClose}
      width={800}
      footer={[
        <Button key="cancel" onClick={onClose}>انصراف</Button>,
        <Button key="add" type="primary" disabled={!selected} onClick={add} loading={preview.isPending}>
          افزودن به سفارش
        </Button>,
      ]}
    >
      <Input
        allowClear
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        placeholder="جستجوی سرویس..."
        style={{ marginBottom: 12 }}
      />

      <Table<CatalogService>
        rowKey="id"
        size="small"
        columns={columns}
        dataSource={services.data ?? []}
        loading={services.isLoading}
        pagination={false}
        scroll={{ x: 620, y: 220 }}
        rowClassName={(record) =>
          record.id === selected?.id ? "selected-catalog-row" : ""
        }
        onRow={(record) => ({ onClick: () => setSelected(record) })}
      />

      {selected && (
        <Space direction="vertical" size={12} style={{ width: "100%", marginTop: 16 }}>
          {schemaFields.length > 0 && (
            <Card size="small" title="فیلدهای سرویس" variant="borderless">
              <Row gutter={12}>
                {schemaFields.map((field) => (
                  <Col xs={24} md={12} key={field.key}>
                    <Typography.Text type="secondary">
                      {field.label}{field.required ? " *" : ""}
                    </Typography.Text>
                    {renderAttributeInput(field, attributes[field.key], (value) =>
                      setAttributes((current) => ({ ...current, [field.key]: value })),
                    )}
                  </Col>
                ))}
              </Row>
            </Card>
          )}

          <Row gutter={12}>
            <Col span={12}>
              <Typography.Text type="secondary">تعداد / مقدار</Typography.Text>
              <InputNumber
                min={1}
                value={quantity}
                onChange={(value) => setQuantity(Math.max(1, Number(value ?? 1)))}
                style={{ width: "100%" }}
              />
            </Col>
            <Col span={12}>
              <Typography.Text type="secondary">دوره پرداخت</Typography.Text>
              <Select
                value={cycle}
                onChange={(value) => setCycle(value as BillingCycle)}
                style={{ width: "100%" }}
                options={[
                  { value: "Monthly", label: "ماهانه" },
                  { value: "OneTime", label: "یک‌بار" },
                  { value: "Yearly", label: "سالانه" },
                ]}
              />
            </Col>
          </Row>

          <Card size="small" title="آیتم‌های جانبی" variant="borderless">
            {(addons.data ?? []).length === 0 ? (
              <Typography.Text type="secondary">Addon فعالی ندارد.</Typography.Text>
            ) : (
              <Space direction="vertical" style={{ width: "100%" }}>
                {(addons.data ?? []).map((addon) => (
                  <Space key={addon.id} style={{ width: "100%", justifyContent: "space-between" }}>
                    <Checkbox
                      checked={addonState[addon.id]?.selected ?? false}
                      onChange={(event) =>
                        setAddonState((current) => ({
                          ...current,
                          [addon.id]: {
                            selected: event.target.checked,
                            quantity: current[addon.id]?.quantity ?? 1,
                          },
                        }))
                      }
                    >
                      {addon.name} — {formatCurrency(addon.price)}
                    </Checkbox>
                    {addonState[addon.id]?.selected && (
                      <InputNumber
                        min={1}
                        value={addonState[addon.id]?.quantity ?? 1}
                        onChange={(value) =>
                          setAddonState((current) => ({
                            ...current,
                            [addon.id]: {
                              selected: true,
                              quantity: Math.max(1, Number(value ?? 1)),
                            },
                          }))
                        }
                        style={{ width: 90 }}
                      />
                    )}
                  </Space>
                ))}
              </Space>
            )}
          </Card>

          <div className="order-total-preview">
            <Typography.Text type="secondary">قیمت محاسبه‌شده سرور</Typography.Text>
            <Typography.Title level={3}>
              {preview.data ? formatCurrency(preview.data.total) : "—"}
            </Typography.Title>
            {preview.data && preview.data.addonsTotal > 0 && (
              <Typography.Text type="secondary">
                شامل {formatCurrency(preview.data.addonsTotal)} آیتم جانبی
              </Typography.Text>
            )}
          </div>
        </Space>
      )}
    </Modal>
  );
}

function parseAttributeSchema(schemaJson?: string | null): AttributeField[] {
  if (!schemaJson) return [];
  try {
    const schema = JSON.parse(schemaJson) as {
      fields?: Array<Record<string, unknown>>;
    };
    return (schema.fields ?? []).map((field) => ({
      key: String(field.key ?? ""),
      label: String(field.label ?? field.key ?? ""),
      type: String(field.type ?? "text"),
      required: Boolean(field.required),
      options: Array.isArray(field.options)
        ? (field.options as Array<{ value?: unknown; label?: unknown }>).map((option) => ({
            value: String(option.value ?? ""),
            label: String(option.label ?? option.value ?? ""),
          }))
        : undefined,
    }));
  } catch {
    return [];
  }
}

function renderAttributeInput(
  field: AttributeField,
  value: unknown,
  onChange: (value: unknown) => void,
) {
  if (field.options?.length) {
    return field.type === "multiselect" ? (
      <Select
        mode="multiple"
        value={(value as string[] | undefined) ?? []}
        onChange={onChange}
        options={field.options}
        style={{ width: "100%" }}
      />
    ) : (
      <Select
        value={value as string | undefined}
        onChange={onChange}
        options={field.options}
        style={{ width: "100%" }}
      />
    );
  }

  if (field.type === "number") {
    return (
      <InputNumber
        value={value as number | undefined}
        onChange={onChange}
        style={{ width: "100%" }}
      />
    );
  }

  return (
    <Input
      value={(value as string | undefined) ?? ""}
      onChange={(event) => onChange(event.target.value)}
    />
  );
}
interface AddonPickerModalProps {
  open: boolean;
  onClose: () => void;
  onAdd: (item: OrderDraftItem) => void;
}

function AddonPickerModal({
  open,
  onClose,
  onAdd,
}: AddonPickerModalProps) {
  const { message } = App.useApp();
  const [selected, setSelected] = useState<CatalogAddon | null>(null);
  const [quantity, setQuantity] = useState(1);
  const addons = useOrderAddons();

  const add = () => {
    if (!selected) {
      message.warning("یک آیتم جانبی انتخاب کنید.");
      return;
    }

    onAdd({
      key: crypto.randomUUID(),
      addonId: selected.id,
      itemName: selected.name,
      quantity,
      unitPrice: selected.price,
      billingCycle: "OneTime",
    });
    setSelected(null);
    setQuantity(1);
    onClose();
  };

  return (
    <Modal
      title="افزودن آیتم جانبی"
      open={open}
      onCancel={onClose}
      footer={[
        <Button key="cancel" onClick={onClose}>انصراف</Button>,
        <Button key="add" type="primary" disabled={!selected} onClick={add}>
          افزودن به سفارش
        </Button>,
      ]}
    >
      <Table<CatalogAddon>
        rowKey="id"
        size="small"
        loading={addons.isLoading}
        dataSource={addons.data ?? []}
        pagination={false}
        rowClassName={(record) =>
          record.id === selected?.id ? "selected-catalog-row" : ""
        }
        onRow={(record) => ({ onClick: () => setSelected(record) })}
        columns={[
          { title: "نام", dataIndex: "name" },
          {
            title: "قیمت",
            dataIndex: "price",
            width: 150,
            render: (value: number) => formatCurrency(value),
          },
          { title: "واحد", dataIndex: "unit", width: 100 },
        ]}
      />

      {selected && (
        <div className="picker-controls">
          <Typography.Text type="secondary">تعداد</Typography.Text>
          <InputNumber
            min={1}
            value={quantity}
            onChange={(value) => setQuantity(Math.max(1, Number(value ?? 1)))}
            style={{ width: 130 }}
          />
        </div>
      )}
    </Modal>
  );
}
