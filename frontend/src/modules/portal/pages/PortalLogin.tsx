import { useState } from "react";
import { ArrowRightOutlined, LockOutlined, MobileOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Segmented,
  Input,
  Space,
  Steps,
  Typography,
} from "antd";
import { useMutation } from "@tanstack/react-query";
import { Navigate, useNavigate } from "react-router-dom";
import type { ApiEnvelope } from "@/modules/auth/types";
import {
  getPortalErrorMessage,
  portalApi,
} from "@/modules/portal/api";
import { usePortalAuth } from "@/modules/portal/store";
import type { PortalAuthResult } from "@/modules/portal/store";

interface MobileForm {
  mobile: string;
}

export function PortalLogin() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const accessToken = usePortalAuth((state) => state.accessToken);
  const login = usePortalAuth((state) => state.login);
  const [step, setStep] = useState(0);
  const [mobile, setMobile] = useState("");
  const [code, setCode] = useState("");
  const [loginMode, setLoginMode] = useState<"otp" | "password">("otp");
  const [mobileForm] = Form.useForm<MobileForm>();
  const [passwordForm] = Form.useForm<{ mobile: string; password: string }>();

  const requestOtp = useMutation({
    mutationFn: async (mobileNumber: string) => {
      const response = await portalApi.post<ApiEnvelope<unknown>>(
        "/portal/auth/request-otp",
        { mobile: mobileNumber },
      );
      return response.data.data;
    },
  });

  const loginWithPassword = useMutation({
    mutationFn: async (payload: { mobile: string; password: string }) => {
      const response = await portalApi.post<ApiEnvelope<PortalAuthResult>>(
        "/portal/auth/login-password",
        payload,
      );
      return response.data.data as PortalAuthResult;
    },
  });

  const submitPassword = async (values: { mobile: string; password: string }) => {
    try {
      const result = await loginWithPassword.mutateAsync(values);
      login(result);
      message.success("ورود با موفقیت انجام شد.");
      navigate("/portal/dashboard", { replace: true });
    } catch (error) {
      message.error(getPortalErrorMessage(error) || "رمز عبور یا شماره موبایل صحیح نیست.");
    }
  };

  const verifyOtp = useMutation({
    mutationFn: async (payload: { mobile: string; code: string }) => {
      const response = await portalApi.post<ApiEnvelope<PortalAuthResult>>(
        "/portal/auth/verify-otp",
        payload,
      );
      return response.data.data as PortalAuthResult;
    },
  });

  if (accessToken) {
    return <Navigate to="/portal/dashboard" replace />;
  }

  const submitMobile = async (values: MobileForm) => {
    try {
      await requestOtp.mutateAsync(values.mobile.trim());
      setMobile(values.mobile.trim());
      setStep(1);
      message.success("کد تایید به شماره شما ارسال شد.");
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    }
  };

  const submitCode = async () => {
    if (code.length !== 6) {
      message.warning("کد ۶ رقمی را کامل وارد کنید.");
      return;
    }

    try {
      const result = await verifyOtp.mutateAsync({ mobile, code });
      login(result);
      message.success("ورود با موفقیت انجام شد.");
      navigate("/portal/dashboard", { replace: true });
    } catch (error) {
      message.error(
        getPortalErrorMessage(error) ||
          "کد وارد شده صحیح نیست یا منقضی شده است",
      );
    }
  };

  return (
    <div className="portal-login-page">
      <div className="portal-login-overlay" />
      <Card className="portal-login-card" variant="borderless">
        <div className="portal-login-brand">
          <div className="portal-brand-mark">C</div>
          <Typography.Title level={2}>پورتال مشتریان</Typography.Title>
          <Typography.Text type="secondary">
            مدیریت سفارش‌ها، فاکتورها، تیکت‌ها و کیف پول
          </Typography.Text>
        </div>

        <Segmented
          block
          value={loginMode}
          onChange={(value) => setLoginMode(value as "otp" | "password")}
          options={[
            { value: "otp", label: "ورود با کد یکبار مصرف" },
            { value: "password", label: "ورود با رمز عبور" },
          ]}
          style={{ marginBottom: 16 }}
        />

        {loginMode === "password" ? (
          <Form
            form={passwordForm}
            layout="vertical"
            onFinish={submitPassword}
            requiredMark={false}
          >
            <Form.Item
              name="mobile"
              label="شماره موبایل"
              rules={[
                { required: true, message: "شماره موبایل را وارد کنید." },
                { pattern: /^09\d{9}$/, message: "شماره موبایل معتبر نیست." },
              ]}
            >
              <Input size="large" prefix={<MobileOutlined />} placeholder="09xxxxxxxxx" dir="ltr" />
            </Form.Item>
            <Form.Item
              name="password"
              label="رمز عبور"
              rules={[{ required: true, message: "رمز عبور را وارد کنید." }]}
            >
              <Input.Password size="large" prefix={<LockOutlined />} dir="ltr" />
            </Form.Item>
            <Typography.Text type="secondary" style={{ display: "block", marginBottom: 12 }}>
              رمز عبور پیش‌فرض، کد ملی شماست.
            </Typography.Text>
            <Button type="primary" size="large" block htmlType="submit" loading={loginWithPassword.isPending}>
              ورود به پورتال
            </Button>
          </Form>
        ) : (
        <>
        <Steps
          current={step}
          size="small"
          className="portal-login-steps"
          items={[
            { title: "شماره موبایل" },
            { title: "کد تایید" },
          ]}
        />

        {step === 0 ? (
          <Form
            form={mobileForm}
            layout="vertical"
            onFinish={submitMobile}
            requiredMark={false}
          >
            <Form.Item
              name="mobile"
              label="شماره موبایل"
              rules={[
                { required: true, message: "شماره موبایل را وارد کنید." },
                { pattern: /^09\d{9}$/, message: "شماره موبایل معتبر نیست." },
              ]}
            >
              <Input
                size="large"
                prefix={<MobileOutlined />}
                placeholder="09xxxxxxxxx"
                dir="ltr"
                autoComplete="tel"
              />
            </Form.Item>

            <Button
              type="primary"
              size="large"
              block
              htmlType="submit"
              loading={requestOtp.isPending}
            >
              دریافت کد تایید
            </Button>
          </Form>
        ) : (
          <Space direction="vertical" size="middle" className="module-stack">
            <Typography.Text type="secondary">
              کد ۶ رقمی ارسال‌شده به <strong dir="ltr">{mobile}</strong> را وارد
              کنید.
            </Typography.Text>

            <Input.OTP
              length={6}
              value={code}
              onChange={setCode}
              size="large"
              dir="ltr"
              autoFocus
            />

            <Button
              type="primary"
              size="large"
              block
              icon={<LockOutlined />}
              loading={verifyOtp.isPending}
              onClick={() => void submitCode()}
            >
              ورود به پورتال
            </Button>

            <Button
              type="link"
              icon={<ArrowRightOutlined />}
              onClick={() => {
                setStep(0);
                setCode("");
              }}
            >
              تغییر شماره موبایل
            </Button>

            <Typography.Text type="secondary" className="portal-otp-hint">
              در محیط توسعه، کد تایید در لاگ سرور ثبت می‌شود.
            </Typography.Text>
          </Space>
        )}
        </>
        )}
      </Card>
    </div>
  );
}
