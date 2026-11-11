import { useEffect } from "react";
import {
  App,
  Form,
  Input,
  Modal,
  Select,
  Switch,
} from "antd";
import {
  useCreateUser,
  useUpdateUser,
} from "@/modules/settings/hooks/useSettings";
import type {
  AppRole,
  AppUser,
  Department,
} from "@/modules/settings/types";

interface UserModalProps {
  open: boolean;
  user?: AppUser | null;
  roles: AppRole[];
  departments: Department[];
  onClose: () => void;
  onSaved: () => void;
}

interface UserForm {
  username: string;
  password?: string;
  fullName: string;
  mobile: string;
  email?: string;
  isActive: boolean;
  roleIds: string[];
  departmentIds: string[];
}

export function UserModal({
  open,
  user,
  roles,
  departments,
  onClose,
  onSaved,
}: UserModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<UserForm>();
  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const isEdit = Boolean(user);
  const saving = createUser.isPending || updateUser.isPending;

  useEffect(() => {
    if (!open) return;

    if (user) {
      form.setFieldsValue({
        username: user.username,
        fullName: user.fullName,
        mobile: user.mobile,
        email: user.email ?? undefined,
        isActive: user.isActive,
        roleIds: user.roles.map((role) => role.id),
        departmentIds: user.departments.map((department) => department.id),
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ isActive: true, roleIds: [], departmentIds: [] });
    }
  }, [form, open, user]);

  const submit = async (values: UserForm) => {
    try {
      if (isEdit && user) {
        await updateUser.mutateAsync({
          id: user.id,
          payload: {
            username: user.username,
            password: values.password || undefined,
            fullName: values.fullName.trim(),
            mobile: values.mobile.trim(),
            email: values.email?.trim() || null,
            isActive: values.isActive,
            roleIds: values.roleIds,
            departmentIds: values.departmentIds ?? [],
          },
        });
        message.success("کاربر با موفقیت ویرایش شد.");
      } else {
        await createUser.mutateAsync({
          username: values.username.trim(),
          password: values.password,
          fullName: values.fullName.trim(),
          mobile: values.mobile.trim(),
          email: values.email?.trim() || null,
          isActive: values.isActive,
          roleIds: values.roleIds,
          departmentIds: values.departmentIds ?? [],
        });
        message.success("کاربر با موفقیت ایجاد شد.");
      }

      onSaved();
      onClose();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  return (
    <Modal
      title={isEdit ? "ویرایش کاربر" : "افزودن کاربر جدید"}
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText={isEdit ? "ذخیره تغییرات" : "ایجاد کاربر"}
      cancelText="انصراف"
      confirmLoading={saving}
      width={560}
      forceRender
    >
      <Form form={form} layout="vertical" onFinish={submit}>
        <Form.Item
          name="username"
          label="نام کاربری"
          rules={[{ required: true, message: "نام کاربری را وارد کنید." }]}
        >
          <Input placeholder="username" dir="ltr" disabled={isEdit} />
        </Form.Item>

        <Form.Item
          name="password"
          label={isEdit ? "رمز عبور جدید (اختیاری)" : "رمز عبور"}
          rules={
            isEdit
              ? [{ min: 6, message: "رمز عبور حداقل ۶ کاراکتر باشد." }]
              : [
                  { required: true, message: "رمز عبور را وارد کنید." },
                  { min: 6, message: "رمز عبور حداقل ۶ کاراکتر باشد." },
                ]
          }
        >
          <Input.Password
            placeholder={isEdit ? "برای تغییر رمز وارد کنید" : "رمز عبور"}
            autoComplete="new-password"
          />
        </Form.Item>

        <Form.Item
          name="fullName"
          label="نام کامل"
          rules={[{ required: true, message: "نام کامل را وارد کنید." }]}
        >
          <Input placeholder="نام و نام خانوادگی" />
        </Form.Item>

        <Form.Item
          name="mobile"
          label="موبایل"
          rules={[
            { required: true, message: "موبایل را وارد کنید." },
            { pattern: /^09\d{9}$/, message: "شماره موبایل معتبر نیست." },
          ]}
        >
          <Input placeholder="09xxxxxxxxx" dir="ltr" />
        </Form.Item>

        <Form.Item
          name="email"
          label="ایمیل"
          rules={[{ type: "email", message: "ایمیل معتبر نیست." }]}
        >
          <Input placeholder="name@example.com" dir="ltr" />
        </Form.Item>

        <Form.Item
          name="roleIds"
          label="نقش‌ها"
          rules={[{ required: true, message: "حداقل یک نقش انتخاب کنید." }]}
        >
          <Select
            mode="multiple"
            placeholder="انتخاب نقش‌ها"
            options={roles.map((role) => ({
              value: role.id,
              label: role.displayName || role.name,
            }))}
          />
        </Form.Item>

        <Form.Item name="departmentIds" label="دپارتمان‌ها">
          <Select
            mode="multiple"
            placeholder="انتخاب دپارتمان"
            options={departments.map((department) => ({
              value: department.id,
              label: department.name,
            }))}
          />
        </Form.Item>

        <Form.Item
          name="isActive"
          label="وضعیت کاربر"
          valuePropName="checked"
        >
          <Switch checkedChildren="فعال" unCheckedChildren="غیرفعال" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
