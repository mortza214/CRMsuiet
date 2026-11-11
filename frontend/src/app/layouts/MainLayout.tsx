import { useMemo, useState, type ReactNode } from "react";
import {
  ApartmentOutlined,
  AppstoreOutlined,
  BankOutlined,
  BarChartOutlined,
  BellOutlined,
  CreditCardOutlined,
  DashboardOutlined,
  FileTextOutlined,
  FormOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  MessageOutlined,
  SafetyCertificateOutlined,
  SettingOutlined,
  ShopOutlined,
  TeamOutlined,
  UserOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import {
  App,
  Avatar,
  Button,
  Dropdown,
  Layout,
  Menu,
  Space,
  Tag,
  Typography,
  type MenuProps,
} from "antd";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import { api } from "@/shared/api/client";

const { Header, Sider, Content } = Layout;

interface NavigationItem {
  key: string;
  label: string;
  icon: ReactNode;
  permissions?: string[];
}

const navigation: NavigationItem[] = [
  { key: "/dashboard", label: "داشبورد", icon: <DashboardOutlined /> },
  {
    key: "/customers",
    label: "مشتریان",
    icon: <TeamOutlined />,
    permissions: ["Customers.Customer.Read"],
  },
  {
    key: "/orders",
    label: "فروش و سفارش‌ها",
    icon: <ShopOutlined />,
    permissions: ["Orders.Order.Read", "Orders.Order.Manage"],
  },
  {
    key: "/invoices",
    label: "فاکتورها",
    icon: <FileTextOutlined />,
    permissions: ["Billing.Invoice.Read", "Billing.Invoice.Manage"],
  },
  {
    key: "/payments",
    label: "پرداخت‌ها",
    icon: <CreditCardOutlined />,
    permissions: ["Payments.Payment.Read", "Payments.Payment.Manage"],
  },
  {
    key: "/wallet",
    label: "کیف پول من",
    icon: <WalletOutlined />,
    permissions: ["Wallet.CustomerWallet.Read", "Wallet.CommissionWallet.Read"],
  },
  {
    key: "/my-commissions",
    label: "پورسانت‌های من",
    icon: <BankOutlined />,
    permissions: ["Commission.Entry.Read"],
  },
  {
    key: "/commissions",
    label: "مدیریت پورسانت‌ها",
    icon: <BankOutlined />,
    permissions: ["Commission.Entry.Approve"],
  },
  {
    key: "/requests",
    label: "درخواست‌ها",
    icon: <ApartmentOutlined />,
    permissions: ["Orders.Order.Read"],
  },
  {
    key: "/workflow/inbox",
    label: "کارتابل من",
    icon: <ApartmentOutlined />,
    permissions: ["Workflow.Task.Read"],
  },
  {
    key: "/workflow/definitions",
    label: "تعریف گردش کار",
    icon: <ApartmentOutlined />,
    permissions: ["Workflow.Definition.Read"],
  },
  {
    key: "/catalog/services",
    label: "کاتالوگ",
    icon: <AppstoreOutlined />,
    permissions: ["Catalog.Service.Read"],
  },
  {
    key: "/forms/templates",
    label: "فرم‌ها",
    icon: <FormOutlined />,
    permissions: ["Forms.Form.Read", "Forms.Submission.Read"],
  },
  {
    key: "/tickets",
    label: "تیکت‌ها",
    icon: <MessageOutlined />,
    permissions: ["Tickets.Ticket.Read"],
  },
  {
    key: "/reports",
    label: "گزارش‌ها",
    icon: <BarChartOutlined />,
    permissions: [
      "Reports.Financial.Read",
      "Reports.Sales.Read",
      "Reports.Debtors.Read",
      "Reports.Commissions.Read",
    ],
  },
  {
    key: "/settings",
    label: "تنظیمات",
    icon: <SettingOutlined />,
    permissions: ["Settings.Company.Manage"],
  },
];

const fallbackMenuItems: MenuProps["items"] = [
  {
    key: "/dashboard",
    label: "داشبورد",
    icon: <DashboardOutlined />,
  },
];

export function MainLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { message } = App.useApp();
  const user = useAuthStore((state) => state.user);
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const logout = useAuthStore((state) => state.logout);
  const refreshToken = useAuthStore((state) => state.refreshToken);

  const items = useMemo<MenuProps["items"]>(
    () =>
      navigation
        .filter(
          (item) =>
            !item.permissions?.length ||
            item.permissions.some((permission) => hasPermission(permission)),
        )
        .map((item) => ({
          key: item.key,
          label: item.label,
          icon: item.icon,
        })),
    [hasPermission],
  );

  const selectedKey =
    navigation.find(
      (item) =>
        location.pathname === item.key ||
        location.pathname.startsWith(`${item.key}/`),
    )?.key ?? "/dashboard";

  const handleLogout = async () => {
    try {
      if (refreshToken) {
        await api.post("/auth/logout", { refreshToken });
      }
    } catch {
      // خروج محلی حتی در صورت خطای شبکه انجام می‌شود.
    } finally {
      logout();
      message.success("با موفقیت از سامانه خارج شدید.");
      navigate("/login", { replace: true });
    }
  };

  const profileItems: MenuProps["items"] = [
    {
      key: "profile",
      icon: <UserOutlined />,
      label: "پروفایل کاربری",
      disabled: true,
    },
    { type: "divider" },
    {
      key: "logout",
      icon: <LogoutOutlined />,
      label: "خروج از حساب",
      danger: true,
      onClick: () => void handleLogout(),
    },
  ];

  const primaryRole =
    user?.roles?.[0]?.displayName ?? user?.roles?.[0]?.name ?? "کاربر";

  return (
    <Layout className="main-layout">
      <Header className="main-header">
        <Space size="middle" className="header-brand">
          <span className="header-logo">
            <SafetyCertificateOutlined />
          </span>
          <div>
            <Typography.Title level={4} className="header-title">
              CrmSuite
            </Typography.Title>
            <Typography.Text type="secondary" className="header-subtitle">
              سامانه مدیریت مشتریان و فروش
            </Typography.Text>
          </div>
        </Space>

        <Space size="middle" className="header-user">
          <Button
            type="text"
            icon={<BellOutlined />}
            aria-label="اعلان‌ها"
            className="header-action"
          />
          <Dropdown menu={{ items: profileItems }} trigger={["click"]}>
            <button type="button" className="user-trigger">
              <Avatar className="user-avatar">
                {user?.fullName?.trim().charAt(0) || "U"}
              </Avatar>
              <span className="user-meta">
                <strong>{user?.fullName ?? "کاربر سامانه"}</strong>
                <Tag color="blue" bordered={false}>
                  {primaryRole}
                </Tag>
              </span>
            </button>
          </Dropdown>
        </Space>
      </Header>

      <Layout>
        <Sider
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          trigger={null}
          width={248}
          collapsedWidth={72}
          theme="light"
          className="main-sider"
          style={{ height: "100vh", overflowY: "auto" }}
        >
          <div className="sider-toggle">
            <Button
              type="text"
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => setCollapsed((value) => !value)}
              aria-label={collapsed ? "باز کردن منو" : "بستن منو"}
            />
          </div>

          <Menu
            mode="inline"
            selectedKeys={[selectedKey]}
            items={items?.length ? items : fallbackMenuItems}
            onClick={({ key }) => navigate(key)}
            className="main-menu"
          />

          <div className="sider-footer">
            {!collapsed && (
              <Space direction="vertical" size={2}>
                <Typography.Text type="secondary" className="sider-footer-title">
                  <AppstoreOutlined /> نسخه مدیریت
                </Typography.Text>
                <Typography.Text type="secondary" className="sider-footer-version">
                  ۰.۱.۰
                </Typography.Text>
              </Space>
            )}
          </div>
        </Sider>

        <Layout className="content-layout">
          <Content className="main-content">
            <div className="content-panel">
              <Outlet />
            </div>
          </Content>
        </Layout>
      </Layout>
    </Layout>
  );
}
