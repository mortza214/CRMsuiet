import { useEffect, useState } from "react";
import {
  ApartmentOutlined,
  BankOutlined,
  DeleteOutlined,
  DollarOutlined,
  EditOutlined,
  KeyOutlined,
  MessageOutlined,
  PercentageOutlined,
  PlusOutlined,
  SafetyCertificateOutlined,
  ShopOutlined,
  UserOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Row,
  Space,
  Switch,
  Table,
  Tabs,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { DepartmentModal } from "@/modules/settings/components/DepartmentModal";
import { IppanelSettingsTab } from "@/modules/settings/components/IppanelSettingsTab";
import { NotificationSettingsTab } from "@/modules/settings/components/NotificationSettingsTab";
import { RoleModal } from "@/modules/settings/components/RoleModal";
import { RolePermissionsDrawer } from "@/modules/settings/components/RolePermissionsDrawer";
import { TaxRuleModal } from "@/modules/settings/components/TaxRuleModal";
import { UserModal } from "@/modules/settings/components/UserModal";
import {
  useDeleteDepartment,
  useDepartments,
  useGateways,
  usePermissions,
  useResetRequests,
  useResetTestData,
  useRoles,
  useSettingsGroup,
  useTaxRules,
  useUpdateGateway,
  useUpdateSettings,
  useUpdateUser,
  useUsers,
} from "@/modules/settings/hooks/useSettings";
import type {
  AppRole,
  AppUser,
  Department,
  TaxRule,
} from "@/modules/settings/types";
import { useAuthStore } from "@/modules/auth/store";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

interface BillingForm {
  vatPercentage: number;
  insurancePercentage: number;
}

interface GatewayForm {
  isActive: boolean;
  merchantId: string;
  terminalId: string;
}

interface CompanyForm {
  name: string;
  economicCode: string;
  address: string;
  phone: string;
  logoUrl: string;
}

const settingValue = (
  items: Array<{ key: string; value: string }> | undefined,
  key: string,
  fallback = "",
) => items?.find((item) => item.key === key)?.value ?? fallback;

export function SettingsPage() {
  const { message } = App.useApp();
  const canManageSms = useAuthStore((state) =>
    state.hasPermission("Settings.Sms.Manage"),
  );
  const user = useAuthStore((state) => state.user);
  const canResetTestData =
    import.meta.env.DEV &&
    user?.roles.some((role) => role.name === "SuperAdmin") === true;
  const [userPage, setUserPage] = useState(1);
  const [userPageSize, setUserPageSize] = useState(20);
  const [userSearch, setUserSearch] = useState("");
  const [userModalOpen, setUserModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<AppUser | null>(null);
  const [roleDrawerOpen, setRoleDrawerOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<AppRole | null>(null);
  const [roleModalOpen, setRoleModalOpen] = useState(false);
  const [departmentModalOpen, setDepartmentModalOpen] = useState(false);
  const [taxModalOpen, setTaxModalOpen] = useState(false);
  const [editingTaxRule, setEditingTaxRule] = useState<TaxRule | null>(null);
  const [billingForm] = Form.useForm<BillingForm>();
  const [gatewayForm] = Form.useForm<GatewayForm>();
  const [companyForm] = Form.useForm<CompanyForm>();
  const debouncedUserSearch = useDebouncedValue(userSearch, 400);

  const users = useUsers({
    page: userPage,
    pageSize: userPageSize,
    search: debouncedUserSearch,
  });
  const roles = useRoles();
  const permissions = usePermissions();
  const departments = useDepartments();
  const deleteDepartment = useDeleteDepartment();
  const resetTestData = useResetTestData();
  const resetRequests = useResetRequests();
  const billingSettings = useSettingsGroup("Billing");
  const companySettings = useSettingsGroup("Company");
  const gateways = useGateways();
  const taxRules = useTaxRules();
  const updateUser = useUpdateUser();
  const updateBilling = useUpdateSettings("Billing");
  const updateCompany = useUpdateSettings("Company");
  const updateGateway = useUpdateGateway();

  useEffect(() => {
    billingForm.setFieldsValue({
      vatPercentage: Number(settingValue(billingSettings.data, "Billing.VatPercentage", "10")),
      insurancePercentage: Number(
        settingValue(billingSettings.data, "Billing.InsurancePercentage", "0"),
      ),
    });
  }, [billingForm, billingSettings.data]);

  useEffect(() => {
    const saman = gateways.data?.find((gateway) => gateway.provider === "Saman");
    gatewayForm.setFieldsValue({
      isActive: saman?.isActive ?? false,
      merchantId: saman?.merchantId ?? "",
      terminalId: saman?.terminalId ?? "",
    });
  }, [gatewayForm, gateways.data]);

  useEffect(() => {
    companyForm.setFieldsValue({
      name: settingValue(companySettings.data, "Company.Name"),
      economicCode: settingValue(companySettings.data, "Company.EconomicCode"),
      address: settingValue(companySettings.data, "Company.Address"),
      phone: settingValue(companySettings.data, "Company.Phone"),
      logoUrl: settingValue(companySettings.data, "Company.LogoUrl"),
    });
  }, [companyForm, companySettings.data]);

  const openCreateUser = () => {
    setEditingUser(null);
    setUserModalOpen(true);
  };

  const openEditUser = (user: AppUser) => {
    setEditingUser(user);
    setUserModalOpen(true);
  };

  const toggleUserStatus = async (user: AppUser, checked: boolean) => {
    try {
      await updateUser.mutateAsync({
        id: user.id,
        payload: {
          username: user.username,
          fullName: user.fullName,
          mobile: user.mobile,
          email: user.email ?? null,
          isActive: checked,
          roleIds: user.roles.map((role) => role.id),
          departmentIds: user.departments.map((department) => department.id),
        },
      });
      message.success(checked ? "کاربر فعال شد." : "کاربر غیرفعال شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const openRolePermissions = (role: AppRole) => {
    setSelectedRole(role);
    setRoleDrawerOpen(true);
  };

  const openCreateTaxRule = () => {
    setEditingTaxRule(null);
    setTaxModalOpen(true);
  };

  const openEditTaxRule = (rule: TaxRule) => {
    setEditingTaxRule(rule);
    setTaxModalOpen(true);
  };

  const saveBilling = async (values: BillingForm) => {
    try {
      await updateBilling.mutateAsync([
        {
          key: "Billing.VatPercentage",
          value: String(values.vatPercentage),
          group: "Billing",
        },
        {
          key: "Billing.InsurancePercentage",
          value: String(values.insurancePercentage),
          group: "Billing",
        },
      ]);
      message.success("تنظیمات مالی ذخیره شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const saveGateway = async (values: GatewayForm) => {
    try {
      await updateGateway.mutateAsync({
        provider: "Saman",
        isActive: values.isActive,
        merchantId: values.merchantId.trim(),
        terminalId: values.terminalId.trim(),
      });
      message.success("تنظیمات درگاه سامان ذخیره شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const saveCompany = async (values: CompanyForm) => {
    try {
      await updateCompany.mutateAsync([
        { key: "Company.Name", value: values.name.trim(), group: "Company" },
        {
          key: "Company.EconomicCode",
          value: values.economicCode.trim(),
          group: "Company",
        },
        { key: "Company.Address", value: values.address.trim(), group: "Company" },
        { key: "Company.Phone", value: values.phone.trim(), group: "Company" },
        { key: "Company.LogoUrl", value: values.logoUrl.trim(), group: "Company" },
      ]);
      message.success("تنظیمات شرکت ذخیره شد.");
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const userColumns: TableColumnsType<AppUser> = [
    {
      title: "نام کاربری",
      dataIndex: "username",
      key: "username",
      width: 145,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "نام کامل",
      dataIndex: "fullName",
      key: "fullName",
      width: 180,
    },
    {
      title: "موبایل",
      dataIndex: "mobile",
      key: "mobile",
      width: 135,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "ایمیل",
      dataIndex: "email",
      key: "email",
      width: 175,
      render: (value: string | null) =>
        value ? <span dir="ltr">{value}</span> : "-",
    },
    {
      title: "نقش‌ها",
      dataIndex: "roles",
      key: "roles",
      width: 190,
      render: (value: AppUser["roles"]) => (
        <Space size={2} wrap>
          {value.map((role) => (
            <Tag key={role.id} color="blue" bordered={false}>
              {role.displayName || role.name}
            </Tag>
          ))}
        </Space>
      ),
    },
    {
      title: "وضعیت",
      dataIndex: "isActive",
      key: "isActive",
      width: 105,
      render: (value: boolean, record) => (
        <Switch
          size="small"
          checked={value}
          loading={updateUser.isPending}
          onChange={(checked) => void toggleUserStatus(record, checked)}
          checkedChildren="فعال"
          unCheckedChildren="غیرفعال"
        />
      ),
    },
    {
      title: "عملیات",
      key: "actions",
      width: 90,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EditOutlined />}
          onClick={() => openEditUser(record)}
        >
          ویرایش
        </Button>
      ),
    },
  ];

  const roleColumns: TableColumnsType<AppRole> = [
    {
      title: "نام نقش",
      dataIndex: "displayName",
      key: "displayName",
      width: 220,
      render: (value: string, record) => value || record.name,
    },
    {
      title: "کد نقش",
      dataIndex: "name",
      key: "name",
      width: 170,
      render: (value: string) => <span dir="ltr">{value}</span>,
    },
    {
      title: "تعداد دسترسی",
      dataIndex: "permissions",
      key: "permissions",
      width: 130,
      align: "center",
      render: (value: AppRole["permissions"]) => (
        <Tag color="purple" bordered={false}>
          {value.length}
        </Tag>
      ),
    },
    {
      title: "عملیات",
      key: "actions",
      width: 160,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<KeyOutlined />}
          onClick={() => openRolePermissions(record)}
        >
          مدیریت دسترسی‌ها
        </Button>
      ),
    },
  ];

  const departmentColumns: TableColumnsType<Department> = [
    { title: "نام دپارتمان", dataIndex: "name", key: "name" },
    {
      title: "دپارتمان والد",
      key: "parent",
      render: (_value, record) =>
        departments.data?.find((item) => item.id === record.parentId)?.name ?? "-",
    },
    {
      title: "عملیات",
      key: "actions",
      width: 100,
      render: (_value, record) => (
        <Popconfirm
          title="حذف دپارتمان؟"
          description="در صورت داشتن کاربر یا زیرمجموعه، حذف انجام نمی‌شود."
          okText="حذف"
          cancelText="انصراف"
          onConfirm={() => deleteDepartment.mutate(record.id)}
        >
          <Button type="link" danger>
            حذف
          </Button>
        </Popconfirm>
      ),
    },
  ];

  const taxRuleColumns: TableColumnsType<TaxRule> = [
    { title: "نام قانون", dataIndex: "name", key: "name" },
    {
      title: "درصد",
      dataIndex: "percentage",
      key: "percentage",
      width: 110,
      align: "center",
      render: (value: number) => `${value}%`,
    },
    {
      title: "وضعیت",
      dataIndex: "isActive",
      key: "isActive",
      width: 110,
      render: (value: boolean) => (
        <Tag color={value ? "green" : "default"} bordered={false}>
          {value ? "فعال" : "غیرفعال"}
        </Tag>
      ),
    },
    {
      title: "تاریخ شروع",
      dataIndex: "effectiveFromJalali",
      key: "effectiveFromJalali",
      width: 130,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 95,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EditOutlined />}
          onClick={() => openEditTaxRule(record)}
        >
          ویرایش
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>تنظیمات</Typography.Title>
          <Typography.Text type="secondary">
            مدیریت کاربران، نقش‌ها، تنظیمات مالی، شرکت و قوانین مالیاتی
          </Typography.Text>
        </div>
        {canResetTestData && (
          <Popconfirm
            title="تمام داده‌های تست پاک شوند؟"
            description="سفارش‌ها، فاکتورها، پرداخت‌ها، گردش‌های کار و مشتریان تست حذف می‌شوند. تنظیمات و تعاریف حفظ می‌شوند."
            okText="بله، پاک کن"
            cancelText="انصراف"
            onConfirm={async () => {
              try {
                await resetTestData.mutateAsync();
                message.success("داده‌های تست پاک شدند.");
                window.location.reload();
              } catch {
                // NotificationProvider خطا را نمایش می‌دهد.
              }
            }}
          >
            <Button danger icon={<DeleteOutlined />} loading={resetTestData.isPending}>
              پاک‌سازی داده‌های تست
            </Button>
          </Popconfirm>
        )}
        {canResetTestData && (
          <Popconfirm
            title="درخواست‌ها و داده‌های تراکنشی پاک شوند؟"
            description="Workflow، سفارش‌ها، فاکتورها، پرداخت‌ها، سرویس‌ها و FormSubmissionها حذف می‌شوند. Users، Roles، Departments، WorkflowDefinitions و FormTemplates حفظ می‌شوند."
            okText="بله، پاک کن"
            cancelText="انصراف"
            onConfirm={async () => {
              try {
                await resetRequests.mutateAsync();
                message.success("درخواست‌ها و داده‌های تراکنشی پاک شدند.");
                window.location.reload();
              } catch {
                // NotificationProvider handles errors.
              }
            }}
          >
            <Button danger loading={resetRequests.isPending}>
              پاک‌سازی درخواست‌ها (تست)
            </Button>
          </Popconfirm>
        )}
      </div>

      <Card variant="borderless" className="settings-tabs-card">
        <Tabs
          items={[
            {
              key: "users",
              forceRender: true,
              label: (
                <span>
                  <UserOutlined /> کاربران و نقش‌ها
                </span>
              ),
              children: (
                <Space direction="vertical" size={18} className="module-stack">
                  <Card
                    title="کاربران" variant="borderless"
                    className="settings-section-card"
                    extra={
                      <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={openCreateUser}
                      >
                        افزودن کاربر جدید
                      </Button>
                    }
                  >
                    <Input
                      allowClear
                      value={userSearch}
                      onChange={(event) => {
                        setUserSearch(event.target.value);
                        setUserPage(1);
                      }}
                      placeholder="جستجوی نام کاربری، نام یا موبایل..."
                      style={{ maxWidth: 360, marginBottom: 14 }}
                    />
                    <Table<AppUser>
                      rowKey="id"
                      columns={userColumns}
                      dataSource={users.data?.items ?? []}
                      loading={users.isLoading || users.isFetching}
                      scroll={{ x: 1000 }}
                      className="module-table"
                      locale={{ emptyText: "کاربری یافت نشد." }}
                      pagination={{
                        current: users.data?.page ?? userPage,
                        pageSize: users.data?.pageSize ?? userPageSize,
                        total: users.data?.totalCount ?? 0,
                        showSizeChanger: true,
                        pageSizeOptions: [10, 20, 50],
                        showTotal: (total) => `مجموع ${total} کاربر`,
                        onChange: (nextPage, nextPageSize) => {
                          setUserPage(nextPageSize !== userPageSize ? 1 : nextPage);
                          setUserPageSize(nextPageSize);
                        },
                      }}
                    />
                  </Card>

                  <Card
                    title="نقش‌ها" variant="borderless"
                    className="settings-section-card"
                    extra={
                      <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => setRoleModalOpen(true)}
                      >
                        افزودن نقش
                      </Button>
                    }
                  >
                    <Table<AppRole>
                      rowKey="id"
                      columns={roleColumns}
                      dataSource={roles.data ?? []}
                      loading={roles.isLoading}
                      scroll={{ x: 700 }}
                      className="module-table"
                      pagination={false}
                      locale={{ emptyText: "نقشی یافت نشد." }}
                    />
                  </Card>

                  <Card
                    title="دپارتمان‌ها" variant="borderless"
                    className="settings-section-card"
                    extra={
                      <Button
                        icon={<ApartmentOutlined />}
                        onClick={() => setDepartmentModalOpen(true)}
                      >
                        افزودن دپارتمان
                      </Button>
                    }
                  >
                    <Table<Department>
                      rowKey="id"
                      columns={departmentColumns}
                      dataSource={departments.data ?? []}
                      loading={departments.isLoading}
                      pagination={false}
                      className="module-table"
                      locale={{ emptyText: "دپارتمانی ثبت نشده است." }}
                    />
                  </Card>
                </Space>
              ),
            },
            {
              key: "financial",
              forceRender: true,
              label: (
                <span>
                  <DollarOutlined /> تنظیمات مالی
                </span>
              ),
              children: (
                <Row gutter={[18, 18]}>
                  <Col xs={24} lg={12}>
                    <Card
                      title="مالیات و بیمه" variant="borderless"
                      className="settings-section-card"
                    >
                      <Form
                        form={billingForm}
                        layout="vertical"
                        onFinish={saveBilling}
                      >
                        <Form.Item
                          name="vatPercentage"
                          label="درصد مالیات بر ارزش افزوده"
                          rules={[
                            { required: true, message: "درصد مالیات را وارد کنید." },
                            {
                              type: "number",
                              min: 0,
                              max: 100,
                              message: "درصد باید بین ۰ تا ۱۰۰ باشد.",
                            },
                          ]}
                        >
                          <InputNumber
                            min={0}
                            max={100}
                            suffix="%"
                            style={{ width: "100%" }}
                          />
                        </Form.Item>

                        <Form.Item
                          name="insurancePercentage"
                          label="درصد بیمه"
                          rules={[
                            { required: true, message: "درصد بیمه را وارد کنید." },
                            {
                              type: "number",
                              min: 0,
                              max: 100,
                              message: "درصد باید بین ۰ تا ۱۰۰ باشد.",
                            },
                          ]}
                        >
                          <InputNumber
                            min={0}
                            max={100}
                            suffix="%"
                            style={{ width: "100%" }}
                          />
                        </Form.Item>

                        <Button
                          type="primary"
                          htmlType="submit"
                          loading={updateBilling.isPending}
                          icon={<SafetyCertificateOutlined />}
                        >
                          ذخیره تنظیمات مالی
                        </Button>
                      </Form>
                    </Card>
                  </Col>

                  <Col xs={24} lg={12}>
                    <Card
                      title="درگاه پرداخت سامان" variant="borderless"
                      className="settings-section-card"
                    >
                      <Form
                        form={gatewayForm}
                        layout="vertical"
                        onFinish={saveGateway}
                      >
                        <Form.Item
                          name="isActive"
                          label="وضعیت درگاه"
                          valuePropName="checked"
                        >
                          <Switch
                            checkedChildren="فعال"
                            unCheckedChildren="غیرفعال"
                          />
                        </Form.Item>

                        <Form.Item
                          name="merchantId"
                          label="Merchant ID"
                          rules={[
                            { required: true, message: "Merchant ID را وارد کنید." },
                          ]}
                        >
                          <Input placeholder="Saman Merchant ID" dir="ltr" />
                        </Form.Item>

                        <Form.Item
                          name="terminalId"
                          label="Terminal ID"
                          rules={[
                            { required: true, message: "Terminal ID را وارد کنید." },
                          ]}
                        >
                          <Input placeholder="Saman Terminal ID" dir="ltr" />
                        </Form.Item>

                        <Typography.Paragraph type="secondary" className="secret-hint">
                          این مقادیر در بک‌اند به‌صورت رمزنگاری‌شده ذخیره می‌شوند.
                        </Typography.Paragraph>

                        <Button
                          type="primary"
                          htmlType="submit"
                          loading={updateGateway.isPending}
                          icon={<BankOutlined />}
                        >
                          ذخیره تنظیمات درگاه
                        </Button>
                      </Form>
                    </Card>
                  </Col>
                </Row>
              ),
            },
            {
              key: "company",
              forceRender: true,
              label: (
                <span>
                  <ShopOutlined /> تنظیمات شرکت
                </span>
              ),
              children: (
                <Card
                  title="اطلاعات شرکت" variant="borderless"
                  className="settings-section-card"
                >
                  <Form
                    form={companyForm}
                    layout="vertical"
                    onFinish={saveCompany}
                  >
                    <Row gutter={16}>
                      <Col xs={24} md={12}>
                        <Form.Item
                          name="name"
                          label="نام شرکت"
                          rules={[
                            { required: true, message: "نام شرکت را وارد کنید." },
                          ]}
                        >
                          <Input placeholder="نام شرکت" />
                        </Form.Item>
                      </Col>
                      <Col xs={24} md={12}>
                        <Form.Item name="economicCode" label="کد اقتصادی">
                          <Input placeholder="کد اقتصادی" dir="ltr" />
                        </Form.Item>
                      </Col>
                    </Row>

                    <Form.Item name="address" label="آدرس">
                      <Input.TextArea rows={3} placeholder="آدرس شرکت" />
                    </Form.Item>

                    <Row gutter={16}>
                      <Col xs={24} md={12}>
                        <Form.Item name="phone" label="تلفن">
                          <Input placeholder="021xxxxxxxx" dir="ltr" />
                        </Form.Item>
                      </Col>
                      <Col xs={24} md={12}>
                        <Form.Item name="logoUrl" label="آدرس لوگو">
                          <Input
                            placeholder="https://example.com/logo.png"
                            dir="ltr"
                          />
                        </Form.Item>
                      </Col>
                    </Row>

                    <Button
                      type="primary"
                      htmlType="submit"
                      loading={updateCompany.isPending}
                    >
                      ذخیره تنظیمات شرکت
                    </Button>
                  </Form>
                </Card>
              ),
            },
            ...(canManageSms
              ? [
                  {
                    key: "sms",
                    forceRender: true,
                    label: (
                      <span>
                        <MessageOutlined /> اتصال پیامک
                      </span>
                    ),
                    children: <IppanelSettingsTab />,
                  },
                  {
                    key: "notifications",
                    forceRender: true,
                    label: (
                      <span>
                        <MessageOutlined /> اطلاع‌رسانی
                      </span>
                    ),
                    children: <NotificationSettingsTab />,
                  },
                ]
              : []),
            {
              key: "tax-rules",
              forceRender: true,
              label: (
                <span>
                  <PercentageOutlined /> قوانین مالیاتی
                </span>
              ),
              children: (
                <Card
                  title="قوانین مالیاتی" variant="borderless"
                  className="settings-section-card"
                  extra={
                    <Button
                      type="primary"
                      icon={<PlusOutlined />}
                      onClick={openCreateTaxRule}
                    >
                      افزودن قانون جدید
                    </Button>
                  }
                >
                  <Table<TaxRule>
                    rowKey="id"
                    columns={taxRuleColumns}
                    dataSource={taxRules.data ?? []}
                    loading={taxRules.isLoading}
                    scroll={{ x: 700 }}
                    className="module-table"
                    pagination={false}
                    locale={{ emptyText: "قانون مالیاتی ثبت نشده است." }}
                  />
                </Card>
              ),
            },
          ]}
        />
      </Card>

      <UserModal
        open={userModalOpen}
        user={editingUser}
        roles={roles.data ?? []}
        departments={departments.data ?? []}
        onClose={() => setUserModalOpen(false)}
        onSaved={() => setUserModalOpen(false)}
      />

      <RolePermissionsDrawer
        open={roleDrawerOpen}
        role={selectedRole}
        groups={permissions.data ?? []}
        onClose={() => setRoleDrawerOpen(false)}
      />

      <RoleModal
        open={roleModalOpen}
        groups={permissions.data ?? []}
        onClose={() => setRoleModalOpen(false)}
        onSaved={() => setRoleModalOpen(false)}
      />

      <DepartmentModal
        open={departmentModalOpen}
        departments={departments.data ?? []}
        onClose={() => setDepartmentModalOpen(false)}
        onSaved={() => setDepartmentModalOpen(false)}
      />

      <TaxRuleModal
        open={taxModalOpen}
        rule={editingTaxRule}
        onClose={() => setTaxModalOpen(false)}
        onSaved={() => setTaxModalOpen(false)}
      />
    </Space>
  );
}
