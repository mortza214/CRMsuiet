import { useEffect } from "react";
import {
  App,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Switch,
} from "antd";
import dayjs, { type Dayjs } from "dayjs";
import {
  useCreateTaxRule,
  useUpdateTaxRule,
} from "@/modules/settings/hooks/useSettings";
import type { TaxRule } from "@/modules/settings/types";

interface TaxRuleModalProps {
  open: boolean;
  rule?: TaxRule | null;
  onClose: () => void;
  onSaved: () => void;
}

interface TaxRuleForm {
  name: string;
  percentage: number;
  isActive: boolean;
  effectiveFrom: Dayjs;
}

export function TaxRuleModal({
  open,
  rule,
  onClose,
  onSaved,
}: TaxRuleModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<TaxRuleForm>();
  const createRule = useCreateTaxRule();
  const updateRule = useUpdateTaxRule();
  const isEdit = Boolean(rule);
  const saving = createRule.isPending || updateRule.isPending;

  useEffect(() => {
    if (!open) return;

    if (rule) {
      form.setFieldsValue({
        name: rule.name,
        percentage: rule.percentage,
        isActive: rule.isActive,
        effectiveFrom: dayjs(rule.effectiveFrom),
      });
    } else {
      form.resetFields();
      form.setFieldsValue({
        percentage: 10,
        isActive: true,
        effectiveFrom: dayjs(),
      });
    }
  }, [form, open, rule]);

  const submit = async (values: TaxRuleForm) => {
    const payload = {
      name: values.name.trim(),
      percentage: values.percentage,
      isActive: values.isActive,
      effectiveFromUtc: values.effectiveFrom.startOf("day").toISOString(),
    };

    try {
      if (isEdit && rule) {
        await updateRule.mutateAsync({ id: rule.id, payload });
        message.success("قانون مالیاتی ویرایش شد.");
      } else {
        await createRule.mutateAsync(payload);
        message.success("قانون مالیاتی ایجاد شد.");
      }
      onSaved();
      onClose();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  return (
    <Modal
      title={isEdit ? "ویرایش قانون مالیاتی" : "افزودن قانون مالیاتی"}
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText={isEdit ? "ذخیره تغییرات" : "ایجاد قانون"}
      cancelText="انصراف"
      confirmLoading={saving}
      forceRender
    >
      <Form form={form} layout="vertical" onFinish={submit}>
        <Form.Item
          name="name"
          label="نام قانون"
          rules={[{ required: true, message: "نام قانون را وارد کنید." }]}
        >
          <Input placeholder="مثلاً مالیات بر ارزش افزوده" />
        </Form.Item>

        <Form.Item
          name="percentage"
          label="درصد"
          rules={[
            { required: true, message: "درصد را وارد کنید." },
            {
              type: "number",
              min: 0,
              max: 100,
              message: "درصد باید بین ۰ تا ۱۰۰ باشد.",
            },
          ]}
        >
          <InputNumber min={0} max={100} style={{ width: "100%" }} />
        </Form.Item>

        <Form.Item
          name="effectiveFrom"
          label="تاریخ شروع اعتبار"
          rules={[{ required: true, message: "تاریخ شروع را انتخاب کنید." }]}
        >
          <DatePicker style={{ width: "100%" }} />
        </Form.Item>

        <Form.Item
          name="isActive"
          label="وضعیت"
          valuePropName="checked"
        >
          <Switch checkedChildren="فعال" unCheckedChildren="غیرفعال" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
