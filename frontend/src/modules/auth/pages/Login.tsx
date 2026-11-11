import { useState } from "react";
import {
  LockOutlined,
  LoginOutlined,
  SafetyCertificateOutlined,
  UserOutlined,
} from "@ant-design/icons";
import { App, Button, Card, Form, Input, Typography } from "antd";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuthStore } from "@/modules/auth/store";
import type { ApiEnvelope, AuthResult } from "@/modules/auth/types";
import { api, getApiErrorMessage } from "@/shared/api/client";

interface LoginValues {
  username: string;
  password: string;
}

interface LoginLocationState {
  from?: string;
}

export function Login() {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { message } = App.useApp();
  const login = useAuthStore((state) => state.login);
  const accessToken = useAuthStore((state) => state.accessToken);

  if (accessToken) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleSubmit = async (values: LoginValues) => {
    setLoading(true);
    try {
      const response = await api.post<ApiEnvelope<AuthResult>>(
        "/auth/login",
        values,
      );

      const result = response.data.data;
      if (!result?.accessToken) {
        message.error("پاسخ سرور معتبر نیست. دوباره تلاش کنید.");
        return;
      }

      login(result);
      const destination =
        (location.state as LoginLocationState | null)?.from ?? "/dashboard";
      navigate(destination, { replace: true });
    } catch (error) {
      message.error(
        getApiErrorMessage(error) === "خطایی در ارتباط با سرور رخ داد."
          ? "نام کاربری یا رمز عبور اشتباه است."
          : getApiErrorMessage(error),
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-glow login-glow-one" />
      <div className="login-glow login-glow-two" />

      <Card className="login-card" variant="borderless">
        <div className="login-brand">
          <span className="login-brand-icon">
            <SafetyCertificateOutlined />
          </span>
          <Typography.Title level={2} className="login-title">
            CrmSuite
          </Typography.Title>
          <Typography.Text type="secondary">
            سامانه یکپارچه مدیریت مشتریان، فروش و پشتیبانی
          </Typography.Text>
        </div>

        <Typography.Title level={4} className="login-heading">
          ورود به سامانه
        </Typography.Title>

        <Form<LoginValues>
          layout="vertical"
          requiredMark={false}
          onFinish={handleSubmit}
          autoComplete="off"
          size="large"
        >
          <Form.Item
            name="username"
            label="نام کاربری"
            rules={[{ required: true, message: "نام کاربری را وارد کنید." }]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder="نام کاربری"
              autoFocus
            />
          </Form.Item>

          <Form.Item
            name="password"
            label="رمز عبور"
            rules={[{ required: true, message: "رمز عبور را وارد کنید." }]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="رمز عبور"
            />
          </Form.Item>

          <Button
            type="primary"
            htmlType="submit"
            block
            loading={loading}
            icon={<LoginOutlined />}
            className="login-submit"
          >
            ورود به سیستم
          </Button>
        </Form>

        <div className="login-footer">
          <Typography.Text type="secondary">
            برای ورود آزمایشی: superadmin / Admin@123
          </Typography.Text>
        </div>
      </Card>

      <Typography.Text className="login-copyright">
        © ۱۴۰۵ CrmSuite — نسخه ۰.۱.۰
      </Typography.Text>
    </div>
  );
}
