import { useState } from "react";
import {
  CheckCircleFilled,
  CloseCircleFilled,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  SearchOutlined,
  TeamOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Checkbox,
  Input,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { CustomerModal } from "@/modules/customers/pages/CustomerModal";
import { useCustomers } from "@/modules/customers/hooks/useCustomers";
import type { CustomerDetail, CustomerListItem } from "@/modules/customers/types";
import { api } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

export function CustomerList() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const canCreate = hasPermission("Customers.Customer.Create");
  const canUpdate = hasPermission("Customers.Customer.Update");

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [onlyDebtors, setOnlyDebtors] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerDetail | null>(null);
  const [loadingEditId, setLoadingEditId] = useState<string | null>(null);
  const debouncedSearch = useDebouncedValue(search, 400);

  const customers = useCustomers({
    page,
    pageSize,
    search: debouncedSearch,
    onlyDebtors,
  });

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = async (id: string) => {
    setLoadingEditId(id);
    try {
      const response = await api.get<ApiEnvelope<CustomerDetail>>(
        `/customers/${id}`,
      );
      if (!response.data.data) {
        message.error("اطلاعات مشتری دریافت نشد.");
        return;
      }
      setEditing(response.data.data);
      setModalOpen(true);
    } finally {
      setLoadingEditId(null);
    }
  };

  const columns: TableColumnsType<CustomerListItem> = [
    {
      title: "ردیف",
      key: "row",
      width: 70,
      align: "center",
      render: (_value, _record, index) => (page - 1) * pageSize + index + 1,
    },
    {
      title: "نام مشتری",
      key: "name",
      width: 250,
      render: (_value, record) => {
        const contact = [record.firstName, record.lastName]
          .filter(Boolean)
          .join(" ");
        const title =
          record.type === "Legal"
            ? record.companyName || record.displayName
            : record.displayName || contact;

        return (
          <div className="customer-name-cell">
            <strong>{title}</strong>
            {record.type === "Legal" && contact && <small>{contact}</small>}
            {record.tags.length > 0 && (
              <div className="customer-tags">
                {record.tags.map((tag) => (
                  <Tag key={tag.id} color={tag.color} bordered={false}>
                    {tag.name}
                  </Tag>
                ))}
              </div>
            )}
          </div>
        );
      },
    },
    {
      title: "موبایل",
      dataIndex: "mobile",
      key: "mobile",
      width: 135,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "کد ملی / شناسه ملی",
      dataIndex: "nationalId",
      key: "nationalId",
      width: 170,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "نوع",
      dataIndex: "type",
      key: "type",
      width: 95,
      align: "center",
      render: (value: CustomerListItem["type"]) => (
        <Tag color={value === "Legal" ? "green" : "blue"} bordered={false}>
          {value === "Legal" ? "حقوقی" : "حقیقی"}
        </Tag>
      ),
    },
    {
      title: "ریسلر",
      dataIndex: "isReseller",
      key: "isReseller",
      width: 90,
      align: "center",
      render: (value: boolean) =>
        value ? (
          <Tooltip title="ریسلر">
            <CheckCircleFilled className="icon-success" />
          </Tooltip>
        ) : (
          <Tooltip title="غیرریسلر">
            <CloseCircleFilled className="icon-muted" />
          </Tooltip>
        ),
    },
    {
      title: "کارشناس فروش",
      dataIndex: "ownerName",
      key: "ownerName",
      width: 170,
      render: (value: string | null) => value || <Typography.Text type="secondary">تعیین‌نشده</Typography.Text>,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 125,
      fixed: "left",
      render: (_value, record) => (
        <Space size="small">
          <Tooltip title="مشاهده جزئیات">
            <Button
              type="text"
              icon={<EyeOutlined />}
              onClick={() => navigate(`/customers/${record.id}`)}
            />
          </Tooltip>
          {canUpdate && (
            <Tooltip title="ویرایش">
              <Button
                type="text"
                icon={<EditOutlined />}
                loading={loadingEditId === record.id}
                onClick={() => void openEdit(record.id)}
              />
            </Tooltip>
          )}
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>مدیریت مشتریان</Typography.Title>
          <Typography.Text type="secondary">
            مشاهده، جستجو و مدیریت پرونده مشتریان حقیقی و حقوقی
          </Typography.Text>
        </div>
        <span className="dashboard-badge">
          <TeamOutlined /> {customers.data?.totalCount ?? 0} مشتری
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
          placeholder="جستجو بر اساس نام، موبایل یا کد ملی..."
          className="toolbar-search"
        />

        <Space size="middle" wrap>
          <Checkbox
            checked={onlyDebtors}
            onChange={(event) => {
              setOnlyDebtors(event.target.checked);
              setPage(1);
            }}
          >
            فقط بدهکاران
          </Checkbox>

          {canCreate && (
            <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
              افزودن مشتری جدید
            </Button>
          )}
        </Space>
      </div>

      <Table<CustomerListItem>
        rowKey="id"
        columns={columns}
        dataSource={customers.data?.items ?? []}
        loading={customers.isLoading || customers.isFetching}
        scroll={{ x: 1050 }}
        className="module-table"
        locale={{ emptyText: "مشتری‌ای یافت نشد." }}
        pagination={{
          current: customers.data?.page ?? page,
          pageSize: customers.data?.pageSize ?? pageSize,
          total: customers.data?.totalCount ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50],
          showTotal: (total) => `مجموع ${total} مشتری`,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPageSize !== pageSize ? 1 : nextPage);
            setPageSize(nextPageSize);
          },
        }}
      />

      <CustomerModal
        open={modalOpen}
        customer={editing}
        onClose={() => setModalOpen(false)}
        onSaved={() => {
          setModalOpen(false);
          setEditing(null);
        }}
      />
    </Space>
  );
}
