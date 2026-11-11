import { useEffect } from "react";
import { App, Form, Input, Modal, Select } from "antd";
import { useCreateRole } from "@/modules/settings/hooks/useSettings";
import type { PermissionGroup } from "@/modules/settings/types";

interface RoleModalProps {
  open: boolean;
  groups: PermissionGroup[];
  onClose: () => void;
  onSaved: () => void;
}

interface RoleForm {
  name: string;
  displayName: string;
  permissionIds: string[];
}

export function RoleModal({ open, groups, onClose, onSaved }: RoleModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<RoleForm>();
  const createRole = useCreateRole();

  useEffect(() => {
    if (open) {
      form.resetFields();
      form.setFieldsValue({ permissionIds: [] });
    }
  }, [form, open]);

  const submit = async (values: RoleForm) => {
    try {
      await createRole.mutateAsync({
        name: values.name.trim(),
        displayName: values.displayName.trim(),
        permissionIds: values.permissionIds ?? [],
      });
      message.success("نقش با موفقیت ایجاد شد.");
      onSaved();
      onClose();
    } catch {
      // NotificationProvider خطا را نمایش می‌دهد.
    }
  };

  const permissionOptions = groups.map((group) => ({
    label: group.group,
    options: group.permissions.map((permission) => ({
      value: permission.id,
      label: permission.displayName,
    })),
  }));

  return (
    <Modal
      title="افزودن نقش"
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText="ایجاد نقش"
      cancelText="انصراف"
      confirmLoading={createRole.isPending}
      width={620}
      forceRender
    >
      <Form form={form} layout="vertical" onFinish={submit}>
        <Form.Item
          name="name"
          label="کد نقش"
          rules={[{ required: true, message: "کد نقش الزامی است." }]}
        >
          <Input dir="ltr" placeholder="TechnicalExpert" />
        </Form.Item>
        <Form.Item
          name="displayName"
          label="عنوان نقش"
          rules={[{ required: true, message: "عنوان نقش الزامی است." }]}
        >
          <Input placeholder="کارشناس فنی" />
        </Form.Item>
        <Form.Item name="permissionIds" label="دسترسی‌ها">
          <Select
            mode="multiple"
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="انتخاب دسترسی‌ها"
            options={permissionOptions}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
