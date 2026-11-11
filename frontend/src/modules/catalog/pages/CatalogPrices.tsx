import { useState } from "react";
import { CalendarOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  DatePicker,
  Form,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useAuthStore } from "@/modules/auth/store";
import {
  pricingModeLabels,
  useAddServicePrice,
  useCatalogServices,
  usePriceHistory,
} from "@/modules/catalog/hooks/useCatalog";
import type { ServiceListItem, ServicePrice } from "@/modules/catalog/types";
import { formatCurrency } from "@/shared/utils/format";

interface PriceForm {
  price: number;
  effectiveFrom: { toISOString: () => string };
  currency: string;
}

export function CatalogPrices() {
  const { message } = App.useApp();
  const canManage = useAuthStore((state) => state.hasPermission("Catalog.Price.Manage"));
  const services = useCatalogServices({ page: 1, pageSize: 100 });
  const [selectedService, setSelectedService] = useState<string | undefined>();
  const [priceOpen, setPriceOpen] = useState(false);
  const [priceForm] = Form.useForm<PriceForm>();
  const history = usePriceHistory(selectedService);
  const addPrice = useAddServicePrice(selectedService);

  const submit = async (values: PriceForm) => {
    if (!selectedService) return;
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

  const serviceColumns: TableColumnsType<ServiceListItem> = [
    { title: "کد", dataIndex: "code", width: 140 },
    { title: "سرویس", dataIndex: "name" },
    {
      title: "قیمت‌گذاری",
      dataIndex: "pricingMode",
      width: 120,
      render: (value: keyof typeof pricingModeLabels) => (
        <Tag bordered={false}>{pricingModeLabels[value]}</Tag>
      ),
    },
    {
      title: "قیمت فعلی",
      dataIndex: "currentPrice",
      width: 150,
      render: (value: number | null) => formatCurrency(value ?? 0),
    },
    {
      title: "عملیات",
      width: 150,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<CalendarOutlined />}
          onClick={() => setSelectedService(record.id)}
        >
          تاریخچه قیمت
        </Button>
      ),
    },
  ];

  const historyColumns: TableColumnsType<ServicePrice> = [
    { title: "قیمت", dataIndex: "price", render: (value: number) => formatCurrency(value) },
    { title: "شروع", dataIndex: "effectiveFromJalali" },
    { title: "پایان", dataIndex: "effectiveToJalali", render: (value: string | null) => value || "باز" },
    { title: "ارز", dataIndex: "currency" },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>قیمت‌های تاریخ‌دار</Typography.Title>
          <Typography.Text type="secondary">
            قیمت جدید پس از تاریخ شروع، قیمت قبلی را به‌صورت اتمیک می‌بندد.
          </Typography.Text>
        </div>
      </div>

      <Card variant="borderless" className="settings-section-card">
        <Table<ServiceListItem>
          rowKey="id"
          columns={serviceColumns}
          dataSource={services.data?.items ?? []}
          loading={services.isLoading}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          className="module-table"
        />
      </Card>

      {selectedService && (
        <Card
          title="تاریخچه قیمت"
          variant="borderless"
          className="settings-section-card"
          extra={
            <Button
              type="primary"
              onClick={() => {
                priceForm.resetFields();
                priceForm.setFieldsValue({ currency: "IRR" });
                setPriceOpen(true);
              }}
              disabled={!canManage}
            >
              قیمت جدید
            </Button>
          }
        >
          <Table<ServicePrice>
            rowKey="id"
            size="small"
            columns={historyColumns}
            dataSource={history.data ?? []}
            loading={history.isLoading}
            pagination={false}
          />
        </Card>
      )}

      <Modal
        title="زمان‌بندی قیمت"
        open={priceOpen}
        onCancel={() => setPriceOpen(false)}
        onOk={() => priceForm.submit()}
        okText="ثبت قیمت"
        cancelText="انصراف"
        confirmLoading={addPrice.isPending}
        forceRender
      >
        <Form form={priceForm} layout="vertical" onFinish={submit}>
          <Form.Item name="price" label="قیمت" rules={[{ required: true }]}>
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item
            name="effectiveFrom"
            label="شروع اعتبار (باید اکنون یا آینده باشد)"
            rules={[{ required: true }]}
          >
            <DatePicker showTime style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="currency" label="ارز">
            <Select options={[{ value: "IRR", label: "ریال" }]} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
