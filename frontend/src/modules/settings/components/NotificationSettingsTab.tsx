import { useEffect } from "react";
import { App, Button, Card, Form, Input, Space, Switch, Typography } from "antd";
import {
  useSettingsGroup,
  useUpdateSettings,
} from "@/modules/settings/hooks/useSettings";

interface NotificationForm {
  smsEnabled: boolean;
  baleEnabled: boolean;
  baleToken?: string;
}

export function NotificationSettingsTab() {
  const { message } = App.useApp();
  const settings = useSettingsGroup("Notifications");
  const update = useUpdateSettings("Notifications");
  const [form] = Form.useForm<NotificationForm>();

  useEffect(() => {
    const value = (key: string) =>
      settings.data?.find((item) => item.key === key)?.value;
    form.setFieldsValue({
      smsEnabled: value("Sms.Enabled") !== "false",
      baleEnabled: value("Bale.Enabled") === "true",
      baleToken: value("Bale.Token") ?? "",
    });
  }, [form, settings.data]);

  const save = async (values: NotificationForm) => {
    try {
      await update.mutateAsync([
        { key: "Sms.Enabled", value: String(values.smsEnabled), group: "Notifications" },
        { key: "Bale.Enabled", value: String(values.baleEnabled), group: "Notifications" },
        { key: "Bale.Token", value: values.baleToken?.trim() ?? "", group: "Notifications", isSecret: true },
      ]);
      message.success("تنظیمات اطلاع‌رسانی ذخیره شد.");
    } catch {
      // handled by global notifier
    }
  };

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <Card
        title="کانال‌های اطلاع‌رسانی"
        variant="borderless"
        className="settings-section-card"
      >
        <Typography.Paragraph type="secondary">
          کانال پیامک از تنظیمات IPPanel و کانال بله با توکن زیر مدیریت می‌شود.
        </Typography.Paragraph>
        <Form form={form} layout="vertical" onFinish={save}>
          <Form.Item name="smsEnabled" label="پیامک IPPanel" valuePropName="checked">
            <Switch checkedChildren="فعال" unCheckedChildren="غیرفعال" />
          </Form.Item>
          <Form.Item name="baleEnabled" label="بله (Bale)" valuePropName="checked">
            <Switch checkedChildren="فعال" unCheckedChildren="غیرفعال" />
          </Form.Item>
          <Form.Item name="baleToken" label="توکن API بله">
            <Input.Password dir="ltr" placeholder="Bale bot token" />
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={update.isPending}>
            ذخیره تنظیمات
          </Button>
        </Form>
      </Card>
    </Space>
  );
}
