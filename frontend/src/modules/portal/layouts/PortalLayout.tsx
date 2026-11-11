import {
  AppstoreOutlined,
  FileTextOutlined,
  LogoutOutlined,
  MessageOutlined,
  ShoppingOutlined,
  UserOutlined,
  WalletOutlined,
} from "@ant-design/icons";
import { Button, Menu, Space, Typography, type MenuProps } from "antd";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { usePortalProfile } from "@/modules/portal/hooks/usePortal";
import { usePortalAuth } from "@/modules/portal/store";

const menuItems: MenuProps["items"] = [
  {
    key: "/portal/dashboard",
    icon: <AppstoreOutlined />,
    label: "داشبورد",
  },
  {
    key: "/portal/orders",
    icon: <ShoppingOutlined />,
    label: "سفارش‌های من",
  },
  {
    key: "/portal/services",
    icon: <AppstoreOutlined />,
    label: "سرویس‌های من",
  },
  {
    key: "/portal/invoices",
    icon: <FileTextOutlined />,
    label: "فاکتورهای من",
  },
  {
    key: "/portal/tickets",
    icon: <MessageOutlined />,
    label: "تیکت‌های من",
  },
  {
    key: "/portal/wallet",
    icon: <WalletOutlined />,
    label: "کیف پول",
  },
  {
    key: "/portal/profile",
    icon: <UserOutlined />,
    label: "پروفایل",
  },
];

export function PortalLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const storedCustomer = usePortalAuth((state) => state.customer);
  const logout = usePortalAuth((state) => state.logout);
  const profile = usePortalProfile();
  const customer = profile.data ?? storedCustomer;

  const currentKey =
    menuItems
      ?.map((item) => String(item?.key))
      .filter((key) => location.pathname === key || location.pathname.startsWith(`${key}/`))
      .sort((a, b) => b.length - a.length)[0] ?? "/portal/dashboard";

  return (
    <div className="portal-shell">
      <header className="portal-header">
        <div className="portal-header-inner">
          <div className="portal-header-brand">
            <div className="portal-brand-mark small">C</div>
            <div>
              <Typography.Text strong className="portal-brand-title">
                پورتال مشتریان
              </Typography.Text>
              <Typography.Text type="secondary" className="portal-brand-subtitle">
                {customer?.displayName ?? "خوش آمدید"}
              </Typography.Text>
            </div>
          </div>

          <Space size="middle">
            <Typography.Text type="secondary" className="portal-header-mobile">
              {customer?.mobile}
            </Typography.Text>
            <Button
              type="text"
              icon={<LogoutOutlined />}
              onClick={() => {
                logout();
                navigate("/portal/login", { replace: true });
              }}
            >
              خروج
            </Button>
          </Space>
        </div>
      </header>

      <nav className="portal-nav">
        <Menu
          mode="horizontal"
          selectedKeys={[currentKey]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          className="portal-nav-menu"
        />
      </nav>

      <main className="portal-content">
        <Outlet />
      </main>

      <footer className="portal-footer">
        <Typography.Text type="secondary">
          پشتیبانی: از طریق تیکت‌های پورتال با ما در ارتباط باشید.
        </Typography.Text>
      </footer>
    </div>
  );
}
