import { useMemo, useState } from "react";
import { EyeOutlined, ReloadOutlined } from "@ant-design/icons";
import { Button, Card, Checkbox, Input, Select, Space, Table, Tag, Typography, type TableColumnsType } from "antd";
import { useNavigate } from "react-router-dom";
import { useRecentServiceRequests } from "@/modules/orders/hooks/useOrders";
import { useAuthStore } from "@/modules/auth/store";
import type { ServiceRequestSummary } from "@/modules/orders/types";

export function RequestList() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<string | undefined>();
  const [onlyMine, setOnlyMine] = useState(false);
  const currentUserId = useAuthStore((state) => state.user?.id);
  const requests = useRecentServiceRequests(undefined, 50);
  const rows = useMemo(() => {
    const query = search.trim().toLowerCase();
    return (requests.data ?? []).filter((request) => {
      const matchesSearch =
        !query ||
        request.customerName?.toLowerCase().includes(query) ||
        request.orderNumber.toLowerCase().includes(query) ||
        request.serviceName?.toLowerCase().includes(query);
      const matchesStatus =
        !status ||
        (status === "open"
          ? request.workflowStatus === "Running"
          : request.workflowStatus === "Completed");
      const matchesOwner = !onlyMine || request.salesExpertId === currentUserId;
      return matchesSearch && matchesStatus && matchesOwner;
    });
  }, [currentUserId, onlyMine, requests.data, search, status]);

  const columns: TableColumnsType<ServiceRequestSummary> = [
    {
      title: "شماره درخواست",
      dataIndex: "orderNumber",
      width: 150,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    { title: "نام مشتری", dataIndex: "customerName", key: "customerName" },
    { title: "نوع سرویس", dataIndex: "serviceName", key: "serviceName" },
    { title: "تاریخ ثبت", dataIndex: "createdAtJalali", width: 120 },
    {
      title: "مرحله فعلی",
      dataIndex: "workflowStateName",
      width: 180,
      render: (value: string | null) => value || "-",
    },
    {
      title: "وضعیت",
      dataIndex: "workflowStatus",
      width: 120,
      render: (value: string | null) => (
        <Tag color={value === "Running" ? "blue" : value === "Completed" ? "green" : "default"}>
          {value === "Running" ? "در جریان" : value === "Completed" ? "تمام شده" : value || "-"}
        </Tag>
      ),
    },
    {
      title: "عملیات",
      width: 110,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => {
            if (record.workflowInstanceId) {
              navigate(`/workflow/instances/${record.workflowInstanceId}`);
            } else {
              navigate(`/orders/${record.orderId}`);
            }
          }}
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
          <Typography.Title level={2}>درخواست‌ها</Typography.Title>
          <Typography.Text type="secondary">
            فهرست درخواست‌های سرویس و مرحله فعلی گردش کار
          </Typography.Text>
        </div>
        <Button icon={<ReloadOutlined />} onClick={() => void requests.refetch()}>
          بازخوانی
        </Button>
      </div>
      <Card variant="borderless" className="settings-section-card">
        <Space wrap style={{ marginBottom: 16 }}>
          <Input
            allowClear
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="جستجوی مشتری، شماره درخواست یا سرویس"
            style={{ width: 300 }}
          />
          <Checkbox checked={onlyMine} onChange={(event) => setOnlyMine(event.target.checked)}>
            فقط درخواست‌های من
          </Checkbox>
          <Select
            allowClear
            value={status}
            onChange={setStatus}
            placeholder="وضعیت"
            style={{ width: 160 }}
            options={[
              { value: "open", label: "در جریان" },
              { value: "closed", label: "تمام شده" },
            ]}
          />
        </Space>
        <Table<ServiceRequestSummary>
          rowKey="orderId"
          columns={columns}
          dataSource={rows}
          loading={requests.isLoading}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 1000 }}
          className="module-table"
          locale={{ emptyText: "درخواستی یافت نشد." }}
        />
      </Card>
    </Space>
  );
}
