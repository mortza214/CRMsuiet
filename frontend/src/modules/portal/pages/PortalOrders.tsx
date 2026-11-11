import { useState } from "react";
import { EyeOutlined, ShoppingOutlined } from "@ant-design/icons";
import {
  Button,
  Card,
  Descriptions,
  Empty,
  Modal,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { orderStatusMeta } from "@/modules/orders/constants";
import type { OrderItem, OrderListItem, OrderStatus } from "@/modules/orders/types";
import {
  usePortalOrder,
  usePortalOrders,
} from "@/modules/portal/hooks/usePortal";
import { formatCurrency } from "@/shared/utils/format";

export function PortalOrders() {
  const [selectedId, setSelectedId] = useState<string | undefined>();
  const orders = usePortalOrders();
  const order = usePortalOrder(selectedId);

  const columns: TableColumnsType<OrderListItem> = [
    {
      title: "شماره سفارش",
      dataIndex: "orderNumber",
      key: "orderNumber",
      width: 160,
      render: (value: string) => (
        <Typography.Text strong dir="ltr">
          {value}
        </Typography.Text>
      ),
    },
    {
      title: "تاریخ",
      dataIndex: "orderDateJalali",
      key: "orderDateJalali",
      width: 125,
    },
    {
      title: "تعداد آیتم",
      dataIndex: "itemCount",
      key: "itemCount",
      width: 105,
      align: "center",
    },
    {
      title: "مبلغ",
      dataIndex: "totalAmount",
      key: "totalAmount",
      width: 170,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 140,
      render: (value: OrderStatus) => {
        const meta = orderStatusMeta[value];
        return (
          <Tag color={meta?.color} bordered={false}>
            {meta?.label}
          </Tag>
        );
      },
    },
    {
      title: "عملیات",
      key: "actions",
      width: 100,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => setSelectedId(record.id)}
        >
          مشاهده
        </Button>
      ),
    },
  ];

  const itemColumns: TableColumnsType<OrderItem> = [
    { title: "آیتم", dataIndex: "itemName", key: "itemName" },
    {
      title: "تعداد",
      dataIndex: "quantity",
      key: "quantity",
      width: 90,
      align: "center",
    },
    {
      title: "قیمت واحد",
      dataIndex: "unitPrice",
      key: "unitPrice",
      width: 150,
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "جمع",
      dataIndex: "totalPrice",
      key: "totalPrice",
      width: 155,
      render: (value: number) => formatCurrency(value),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <div>
          <Typography.Title level={2}>سفارش‌های من</Typography.Title>
          <Typography.Text type="secondary">
            مشاهده وضعیت سفارش‌ها و سرویس‌های خریداری‌شده
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <ShoppingOutlined /> {orders.data?.length ?? 0} سفارش
        </span>
      </div>

      <Card className="portal-table-card" variant="borderless">
        <Table<OrderListItem>
          rowKey="id"
          columns={columns}
          dataSource={orders.data ?? []}
          loading={orders.isLoading}
          scroll={{ x: 820 }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          locale={{ emptyText: "سفارشی ثبت نشده است." }}
        />
      </Card>

      <Modal
        title="جزئیات سفارش"
        open={Boolean(selectedId)}
        onCancel={() => setSelectedId(undefined)}
        footer={null}
        width={720}
      >
        {order.isLoading ? (
          <div className="centered-state">
            <Spin />
          </div>
        ) : order.data ? (
          <Space direction="vertical" size="middle" className="module-stack">
            <Descriptions bordered size="small" column={{ xs: 1, sm: 2 }}>
              <Descriptions.Item label="شماره سفارش">
                {order.data.orderNumber}
              </Descriptions.Item>
              <Descriptions.Item label="تاریخ">
                {order.data.orderDateJalali}
              </Descriptions.Item>
              <Descriptions.Item label="وضعیت">
                {orderStatusMeta[order.data.status]?.label}
              </Descriptions.Item>
              <Descriptions.Item label="مبلغ کل">
                {formatCurrency(order.data.totalAmount)}
              </Descriptions.Item>
              <Descriptions.Item label="تخفیف">
                {formatCurrency(order.data.discountAmount)}
              </Descriptions.Item>
              <Descriptions.Item label="قابل پرداخت">
                {formatCurrency(order.data.payableAmount)}
              </Descriptions.Item>
            </Descriptions>

            <Table<OrderItem>
              rowKey="id"
              columns={itemColumns}
              dataSource={order.data.items}
              pagination={false}
              size="small"
              scroll={{ x: 560 }}
            />
          </Space>
        ) : (
          <Empty description="اطلاعات سفارش دریافت نشد." />
        )}
      </Modal>
    </Space>
  );
}
