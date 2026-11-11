import { useEffect, useState } from "react";
import { EditOutlined, UserOutlined } from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Descriptions,
  Form,
  Input,
  Modal,
  Result,
  Space,
  Spin,
  Tag,
  Typography,
} from "antd";
import { getPortalErrorMessage } from "@/modules/portal/api";
import {
  usePortalChangePassword,
  usePortalProfile,
  usePortalUpdateProfile,
} from "@/modules/portal/hooks/usePortal";

interface ProfileForm {
  phone?: string;
  email?: string;
  address?: string;
}

export function PortalProfile() {
  const { message } = App.useApp();
  const [editOpen, setEditOpen] = useState(false);
  const [form] = Form.useForm<ProfileForm>();
  const [passwordOpen, setPasswordOpen] = useState(false);
  const [passwordForm] = Form.useForm<{ currentPassword: string; newPassword: string }>();
  const profile = usePortalProfile();
  const updateProfile = usePortalUpdateProfile();
  const changePassword = usePortalChangePassword();

  useEffect(() => {
    if (!editOpen || !profile.data) return;
    form.setFieldsValue({
      phone: profile.data.phone ?? undefined,
      email: profile.data.email ?? undefined,
      address: profile.data.address ?? undefined,
    });
  }, [editOpen, form, profile.data]);

  if (profile.isLoading) {
    return (
      <div className="centered-state">
        <Spin size="large" />
      </div>
    );
  }

  if (profile.isError || !profile.data) {
    return (
      <Result
        status="500"
        title="دریافت پروفایل ناموفق بود"
        subTitle="لطفاً دوباره تلاش کنید."
      />
    );
  }

  const data = profile.data;

  const submit = async (values: ProfileForm) => {
    try {
      await updateProfile.mutateAsync({
        phone: values.phone?.trim() || null,
        email: values.email?.trim() || null,
        address: values.address?.trim() || null,
      });
      message.success("اطلاعات پروفایل به‌روزرسانی شد.");
      setEditOpen(false);
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    }
  };

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <div>
          <Typography.Title level={2}>پروفایل من</Typography.Title>
          <Typography.Text type="secondary">
            اطلاعات حساب و راه‌های ارتباطی شما
          </Typography.Text>
        </div>
        <Space>
          <Button onClick={() => setPasswordOpen(true)}>تغییر رمز عبور</Button>
          <Button
            type="primary"
            icon={<EditOutlined />}
            onClick={() => setEditOpen(true)}
          >
            ویرایش اطلاعات
          </Button>
        </Space>
      </div>

      <Card className="portal-profile-card" variant="borderless">
        <div className="portal-profile-head">
          <div className="portal-profile-avatar">
            <UserOutlined />
          </div>
          <div>
            <Typography.Title level={3}>{data.displayName}</Typography.Title>
            <Space size="small" wrap>
              <Tag color={data.isReseller ? "purple" : "green"} bordered={false}>
                {data.isReseller ? "ریسلر" : "مشتری"}
              </Tag>
              <Typography.Text type="secondary" dir="ltr">
                {data.mobile}
              </Typography.Text>
            </Space>
          </div>
        </div>

        <Descriptions
          bordered
          size="small"
          column={{ xs: 1, sm: 2 }}
          className="customer-descriptions"
        >
          <Descriptions.Item label="نام">
            {data.firstName || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="نام خانوادگی">
            {data.lastName || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="کد ملی / شناسه ملی">
            <span dir="ltr">{data.nationalId}</span>
          </Descriptions.Item>
          <Descriptions.Item label="موبایل">
            <span dir="ltr">{data.mobile}</span>
          </Descriptions.Item>
          <Descriptions.Item label="تلفن ثابت">
            {data.phone || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="ایمیل">
            {data.email || "-"}
          </Descriptions.Item>
          <Descriptions.Item label="آدرس" span={2}>
            {data.address || "-"}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Modal
        title="تغییر رمز عبور"
        open={passwordOpen}
        onCancel={() => setPasswordOpen(false)}
        onOk={() => passwordForm.submit()}
        okText="ذخیره رمز جدید"
        cancelText="انصراف"
        confirmLoading={changePassword.isPending}
      >
        <Form
          form={passwordForm}
          layout="vertical"
          onFinish={async (values) => {
            try {
              await changePassword.mutateAsync(values);
              message.success("رمز عبور تغییر کرد.");
              setPasswordOpen(false);
              passwordForm.resetFields();
            } catch (error) {
              message.error(getPortalErrorMessage(error));
            }
          }}
        >
          <Form.Item name="currentPassword" label="رمز عبور فعلی" rules={[{ required: true }]}>
            <Input.Password dir="ltr" />
          </Form.Item>
          <Form.Item name="newPassword" label="رمز عبور جدید" rules={[{ required: true, min: 6 }]}>
            <Input.Password dir="ltr" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="ویرایش اطلاعات"
        open={editOpen}
        onCancel={() => setEditOpen(false)}
        onOk={() => form.submit()}
        okText="ذخیره تغییرات"
        cancelText="انصراف"
        confirmLoading={updateProfile.isPending}
      >
        <Form form={form} layout="vertical" onFinish={submit}>
          <Form.Item name="phone" label="تلفن ثابت">
            <Input placeholder="021xxxxxxxx" dir="ltr" />
          </Form.Item>
          <Form.Item
            name="email"
            label="ایمیل"
            rules={[{ type: "email", message: "ایمیل معتبر نیست." }]}
          >
            <Input placeholder="name@example.com" dir="ltr" />
          </Form.Item>
          <Form.Item name="address" label="آدرس">
            <Input.TextArea rows={3} placeholder="آدرس خود را وارد کنید" />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
