import { BuildOutlined } from "@ant-design/icons";
import { Card, Space, Typography } from "antd";

interface ModulePlaceholderProps {
  title: string;
}

export function ModulePlaceholder({ title }: ModulePlaceholderProps) {
  return (
    <Space direction="vertical" size={20} className="placeholder-stack">
      <div className="page-heading">
        <div>
          <Typography.Title level={2}>{title}</Typography.Title>
          <Typography.Text type="secondary">
            این بخش در مرحله بعدی توسعه رابط کاربری فعال می‌شود.
          </Typography.Text>
        </div>
      </div>

      <Card className="placeholder-card" variant="borderless">
        <Space direction="vertical" size="middle" align="center">
          <span className="placeholder-icon">
            <BuildOutlined />
          </span>
          <Typography.Title level={4}>در حال آماده‌سازی</Typography.Title>
          <Typography.Text type="secondary" className="placeholder-text">
            زیرساخت بک‌اند این ماژول آماده است و صفحه مدیریت آن به‌زودی در همین
            چیدمان اضافه خواهد شد.
          </Typography.Text>
        </Space>
      </Card>
    </Space>
  );
}
