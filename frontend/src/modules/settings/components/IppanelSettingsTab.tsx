import { useEffect, useState } from "react";
import {
  ApiOutlined,
  SendOutlined,
} from "@ant-design/icons";
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  Modal,
  Row,
  Space,
  Switch,
  Tag,
  Typography,
} from "antd";
import {
  useIppanelSettings,
  useTestIppanelSettings,
  useUpdateIppanelSettings,
} from "@/modules/settings/hooks/useSettings";
import type { SmsResult } from "@/modules/settings/types";

interface IppanelForm {
  enabled: boolean;
  apiKey?: string;
  fromNumber: string;
  patternCode: string;
  baseUrl: string;
  patternParamName: string;
}

interface TestForm {
  mobile: string;
}

const sourceMeta: Record<string, { label: string; color: string }> = {
  Environment: { label: "Environment Variable", color: "blue" },
  Database: { label: "Database (Settings)", color: "green" },
  None: { label: "تنظیم نشده", color: "default" },
};

export function IppanelSettingsTab() {
  const { message } = App.useApp();
  const settings = useIppanelSettings();
  const update = useUpdateIppanelSettings();
  const test = useTestIppanelSettings();
  const [form] = Form.useForm<IppanelForm>();
  const [testForm] = Form.useForm<TestForm>();
  const [testOpen, setTestOpen] = useState(false);
  const [testResult, setTestResult] = useState<SmsResult | null>(null);
  const [savedMask, setSavedMask] = useState<string | null>(null);

  useEffect(() => {
    if (!settings.data) return;
    setSavedMask(settings.data.apiKeyMasked ?? null);
    form.setFieldsValue({
      enabled: settings.data.enabled,
      apiKey: "",
      fromNumber: settings.data.fromNumber,
      patternCode: settings.data.patternCode,
      baseUrl:
        settings.data.baseUrl || "https://edge.ippanel.com/v1/api/send",
      patternParamName: settings.data.patternParamName || "code",
    });
  }, [form, settings.data]);

  const save = async (values: IppanelForm) => {
    try {
      const saved = await update.mutateAsync({
        enabled: values.enabled,
        apiKey: values.apiKey?.trim() || null,
        fromNumber: values.fromNumber.trim(),
        patternCode: values.patternCode.trim(),
        baseUrl:
          values.baseUrl.trim() || "https://edge.ippanel.com/v1/api/send",
        patternParamName: values.patternParamName.trim() || "code",
      });
      setSavedMask(saved.apiKeyMasked ?? null);
      form.setFieldValue("apiKey", "");
      message.success(
        values.apiKey?.trim()
          ? "تنظیمات و کلید API ذخیره شد."
          : "تنظیمات ذخیره شد؛ کلید API قبلی حفظ شد.",
      );
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  const submitTest = async (values: TestForm) => {
    try {
      const result = await test.mutateAsync(values.mobile.trim());
      setTestResult(result);
    } catch {
      setTestResult({
        isSuccess: false,
        errorMessage: "درخواست تست ارسال نشد.",
      });
    }
  };

  const source = sourceMeta[settings.data?.source ?? "None"] ?? sourceMeta.None;

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <Card
        title={
          <Space>
            <ApiOutlined />
            <span>اتصال پیامک IPPanel</span>
          </Space>
        }
        variant="borderless"
        className="settings-section-card"
        extra={
          <Space>
            <Typography.Text type="secondary">منبع تنظیمات فعلی:</Typography.Text>
            <Tag color={source.color} bordered={false}>
              {source.label}
            </Tag>
          </Space>
        }
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 18 }}
          message="کلید API پس از ذخیره قابل مشاهده نیست؛ فقط می‌توانید آن را بازنویسی کنید."
        />
        {savedMask && (
          <Alert
            type="success"
            showIcon
            style={{ marginBottom: 18 }}
            message={`کلید API ذخیره شده است: ${savedMask}`}
            description="خالی بودن فیلد ApiKey طبیعی است؛ برای تغییر کلید، مقدار جدید را وارد کنید."
          />
        )}
        {settings.data?.source === "Environment" && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 18 }}
            message="Environment Variable اولویت دارد"
            description="مقادیر Environment بر تنظیمات این صفحه غالب هستند. برای اعمال تنظیمات پنل، متغیرهای محیطی IPPanel را حذف یا خالی کنید."
          />
        )}

        <Form form={form} layout="vertical" onFinish={save}>
          <Form.Item
            name="enabled"
            label="فعال‌سازی ارسال پیامک"
            valuePropName="checked"
          >
            <Switch checkedChildren="فعال" unCheckedChildren="غیرفعال" />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="apiKey"
                label="API Key"
                extra={
                  settings.data?.apiKeyMasked
                    ? `کلید ذخیره‌شده: ${settings.data.apiKeyMasked}`
                    : "هنوز کلیدی ذخیره نشده است."
                }
              >
                <Input.Password
                  dir="ltr"
                  autoComplete="new-password"
                  placeholder={
                    settings.data?.apiKeyMasked
                      ? `${settings.data.apiKeyMasked} (برای تغییر، مقدار جدید وارد کنید)`
                      : "API Key را وارد کنید"
                  }
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="fromNumber"
                label="شماره فرستنده"
                rules={[{ required: true, message: "شماره فرستنده الزامی است." }]}
              >
                <Input dir="ltr" placeholder="+983000000001" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="patternCode"
                label="Pattern Code"
                rules={[{ required: true, message: "کد پترن الزامی است." }]}
              >
                <Input dir="ltr" placeholder="PATTERN_CODE" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="patternParamName"
                label="نام پارامتر پترن (حساس به حروف)"
                rules={[{ required: true, message: "نام پارامتر پترن الزامی است." }]}
                extra="باید دقیقاً با نام متغیر پترن در پنل IPPanel یکسان باشد؛ معمولاً CODE با حروف بزرگ است."
              >
                <Input dir="ltr" placeholder="code" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            name="baseUrl"
            label="Base URL"
            rules={[
              { required: true, message: "Base URL الزامی است." },
              { type: "url", message: "Base URL معتبر نیست." },
            ]}
          >
            <Input
              dir="ltr"
              placeholder="https://edge.ippanel.com/v1/api/send"
            />
          </Form.Item>

          <Space>
            <Button
              type="primary"
              htmlType="submit"
              loading={update.isPending}
            >
              ذخیره تنظیمات
            </Button>
            <Button
              icon={<SendOutlined />}
              onClick={() => {
                setTestResult(null);
                testForm.resetFields();
                setTestOpen(true);
              }}
              disabled={!settings.data?.enabled}
            >
              تست ارسال
            </Button>
          </Space>
        </Form>
      </Card>

      <Modal
        title="تست ارسال پیامک IPPanel"
        open={testOpen}
        onCancel={() => setTestOpen(false)}
        onOk={() => testForm.submit()}
        okText="ارسال تست"
        cancelText="انصراف"
        confirmLoading={test.isPending}
        forceRender
      >
        <Form form={testForm} layout="vertical" onFinish={submitTest}>
          <Form.Item
            name="mobile"
            label="موبایل تست"
            rules={[
              { required: true, message: "موبایل تست را وارد کنید." },
              { pattern: /^09\d{9}$/, message: "شماره موبایل معتبر نیست." },
            ]}
          >
            <Input dir="ltr" placeholder="09121234567" />
          </Form.Item>
        </Form>

        {testResult && (
          <Alert
            style={{ marginTop: 12 }}
            type={testResult.isSuccess ? "success" : "error"}
            showIcon
            message={testResult.isSuccess ? "ارسال تست موفق بود" : "ارسال تست ناموفق بود"}
            description={
              testResult.isSuccess
                ? `MessageId: ${testResult.messageId ?? "-"}`
                : testResult.errorMessage || "خطای نامشخص"
            }
          />
        )}
      </Modal>
    </Space>
  );
}
