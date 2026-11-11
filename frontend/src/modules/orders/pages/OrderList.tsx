import { useState } from "react";
import {
  EyeOutlined,
  PlusOutlined,
  SearchOutlined,
  ShoppingCartOutlined,
} from "@ant-design/icons";
import {
  Button,
  Input,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { useCustomerOwners } from "@/modules/customers/hooks/useCustomers";
import { orderStatusMeta, orderStatusOptions } from "@/modules/orders/constants";
import { useOrders } from "@/modules/orders/hooks/useOrders";
import type { OrderListItem, OrderStatus } from "@/modules/orders/types";
import { formatCurrency } from "@/shared/utils/format";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

export function OrderList() {
  const navigate = useNavigate();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canCreate = hasPermission("Orders.Order.Create");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<OrderStatus | undefined>();
  const debouncedSearch = useDebouncedValue(search, 400);
  const orders = useOrders({
    page,
    pageSize,
    search: debouncedSearch,
    status,
  });
  const owners = useCustomerOwners();
  const ownerName = (id: string) =>
    owners.data?.find((user) => user.id === id)?.fullName ?? "-";

  const columns: TableColumnsType<OrderListItem> = [
    {
      title: "شماره سفارش",
      dataIndex: "orderNumber",
      key: "orderNumber",
      width: 165,
      render: (value: string) => (
        <Typography.Text strong copyable={{ text: value }} dir="ltr">
          {value}
        </Typography.Text>
      ),
    },
    {
      title: "نام مشتری",
      dataIndex: "customerName",
      key: "customerName",
      width: 200,
      render: (value: string | null) => value || "مشتری",
    },
    {
      title: "کارشناس فروش",
      dataIndex: "salesExpertId",
      key: "salesExpertId",
      width: 170,
      render: (value: string) => ownerName(value),
    },
    {
      title: "تاریخ ثبت",
      dataIndex: "orderDateJalali",
      key: "orderDateJalali",
      width: 125,
    },
    {
      title: "مبلغ کل",
      dataIndex: "totalAmount",
      key: "totalAmount",
      width: 165,
      align: "left",
      render: (value: number) => formatCurrency(value),
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 150,
      render: (value: OrderStatus) => {
        const meta = orderStatusMeta[value];
        return (
          <Tag color={meta?.color ?? "default"} bordered={false}>
            {meta?.label ?? value}
          </Tag>
        );
      },
    },
    {
      title: "عملیات",
      key: "actions",
      width: 95,
      fixed: "left",
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => navigate(`/orders/${record.id}`)}
        >
          مشاهده
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>فروش و سفارش‌ها</Typography.Title>
          <Typography.Text type="secondary">
            پیگیری سفارش‌های فروش، وضعیت پرداخت و تحویل سرویس‌ها
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <ShoppingCartOutlined /> {orders.data?.totalCount ?? 0} سفارش
        </span>
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
          placeholder="جستجو بر اساس شماره سفارش یا نام مشتری..."
          className="toolbar-search"
        />

        <Space size="middle" wrap>
          <Select
            allowClear
            value={status}
            onChange={(value) => {
              setStatus(value as OrderStatus | undefined);
              setPage(1);
            }}
            options={orderStatusOptions}
            placeholder="فیلتر وضعیت"
            style={{ minWidth: 170 }}
          />

          {canCreate && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate("/orders/create")}
            >
              ثبت سفارش جدید
            </Button>
          )}
        </Space>
      </div>

      <Table<OrderListItem>
        rowKey="id"
        columns={columns}
        dataSource={orders.data?.items ?? []}
        loading={orders.isLoading || orders.isFetching}
        scroll={{ x: 1100 }}
        className="module-table"
        locale={{ emptyText: "سفارشی یافت نشد." }}
        pagination={{
          current: orders.data?.page ?? page,
          pageSize: orders.data?.pageSize ?? pageSize,
          total: orders.data?.totalCount ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50],
          showTotal: (total) => `مجموع ${total} سفارش`,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPageSize !== pageSize ? 1 : nextPage);
            setPageSize(nextPageSize);
          },
        }}
      />
    </Space>
  );
}
