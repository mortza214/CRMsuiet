import { CheckCircleOutlined, DatabaseOutlined, SafetyCertificateOutlined } from "@ant-design/icons";
import { Card, Col, Row, Space, Typography } from "antd";

const foundations = [
  {
    icon: <DatabaseOutlined />,
    title: "Backend آماده توسعه",
    description: "ASP.NET Core 8، EF Core، MySQL، MediatR، Hangfire و Serilog",
  },
  {
    icon: <SafetyCertificateOutlined />,
    title: "امنیت پایه",
    description: "JWT، Refresh Token، RBAC با Permission و ثبت Audit",
  },
  {
    icon: <CheckCircleOutlined />,
    title: "Frontend فارسی",
    description: "React 18، TypeScript، Ant Design RTL و React Query",
  },
];

export function FoundationHome() {
  return (
    <Space direction="vertical" size="large" className="home-stack">
      <div>
        <Typography.Title level={2}>پایه سامانه آماده است</Typography.Title>
        <Typography.Paragraph type="secondary" className="home-lead">
          این صفحه فقط وضعیت اسکلت پروژه را نشان می‌دهد. صفحات هر ماژول در مرحله بعدی و
          پس از تأیید ساختار اضافه می‌شوند.
        </Typography.Paragraph>
      </div>

      <Row gutter={[16, 16]}>
        {foundations.map((item) => (
          <Col xs={24} md={8} key={item.title}>
            <Card className="foundation-card">
              <Space direction="vertical" size="small">
                <span className="foundation-icon">{item.icon}</span>
                <Typography.Title level={5}>{item.title}</Typography.Title>
                <Typography.Text type="secondary">{item.description}</Typography.Text>
              </Space>
            </Card>
          </Col>
        ))}
      </Row>

      <Card className="foundation-note">
        <Typography.Text>
          مرحله بعد: ماژول <strong>Foundation</strong> شامل Audit، تقویم شمسی،
          شماره‌گذاری و تنظیمات پایه.
        </Typography.Text>
      </Card>
    </Space>
  );
}
