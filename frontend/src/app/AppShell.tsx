import { Layout, Space, Tag, Typography } from "antd";
import { Outlet } from "react-router-dom";

const { Header, Content, Footer } = Layout;

export function AppShell() {
  return (
    <Layout className="app-shell">
      <Header className="app-header">
        <Space size="middle">
          <Typography.Title level={4} className="app-brand">
            CrmSuite
          </Typography.Title>
          <Tag color="blue">فاز ۱ — پایه</Tag>
        </Space>
        <Typography.Text type="secondary">نسخه ۰.۱.۰</Typography.Text>
      </Header>

      <Content className="app-content">
        <Outlet />
      </Content>

      <Footer className="app-footer">
        سامانه مدیریت مشتری، فروش سرویس، تیکت، کیف پول و پورسانت
      </Footer>
    </Layout>
  );
}
