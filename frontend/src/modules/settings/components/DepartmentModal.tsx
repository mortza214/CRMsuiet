import { useEffect } from "react";
import { App, Form, Input, Modal, Select } from "antd";
import { useCreateDepartment } from "@/modules/settings/hooks/useSettings";
import type { Department } from "@/modules/settings/types";

interface DepartmentModalProps {
  open: boolean;
  departments: Department[];
  onClose: () => void;
  onSaved: () => void;
}

interface DepartmentForm {
  name: string;
  parentId?: string;
}

export function DepartmentModal({
  open,
  departments,
  onClose,
  onSaved,
}: DepartmentModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<DepartmentForm>();
  const createDepartment = useCreateDepartment();

  useEffect(() => {
    if (open) {
      form.resetFields();
    }
  }, [form, open]);

  const submit = async (values: DepartmentForm) => {
    try {
      await createDepartment.mutateAsync({
        name: values.name.trim(),
        parentId: values.parentId || null,
      });
      message.success("دپارتمان ایجاد شد.");
      onSaved();
      onClose();
    } catch {
      // NotificationProvider خطا را نمایش می‌دهد.
    }
  };

  return (
    <Modal
      title="افزودن دپارتمان"
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText="ایجاد دپارتمان"
      cancelText="انصراف"
      confirmLoading={createDepartment.isPending}
      forceRender
    >
      <Form form={form} layout="vertical" onFinish={submit}>
        <Form.Item
          name="name"
          label="نام دپارتمان"
          rules={[{ required: true, message: "نام دپارتمان الزامی است." }]}
        >
          <Input placeholder="کارشناس فنی" />
        </Form.Item>
        <Form.Item name="parentId" label="دپارتمان والد (اختیاری)">
          <Select
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="بدون والد"
            options={departments.map((department) => ({
              value: department.id,
              label: department.name,
            }))}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
