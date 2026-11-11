import { useEffect } from "react";
import { App, Button, Col, Drawer, Form, Input, Radio, Row, Select, Space, Switch, Typography, type FormInstance } from "antd";
import type { AxiosError } from "axios";
import { useCustomerOwners, useCreateCustomer, useUpdateCustomer } from "@/modules/customers/hooks/useCustomers";
import type { CustomerDetail, CustomerPayload, CustomerType } from "@/modules/customers/types";
import { getApiErrorMessage } from "@/shared/api/client";
import type { ApiEnvelope } from "@/modules/auth/types";

interface CustomerModalProps {
  open: boolean;
  customer?: CustomerDetail | null;
  onClose: () => void;
  onSaved?: (customer: CustomerDetail) => void;
}

interface CustomerFormValues {
  type: CustomerType;
  firstName?: string;
  lastName?: string;
  companyName?: string;
  economicCode?: string;
  nationalId: string;
  mobile: string;
  phone?: string;
  email?: string;
  address?: string;
  isReseller: boolean;
  ownerUserId?: string | null;
}

export function CustomerModal({ open, customer, onClose, onSaved }: CustomerModalProps) {
  const [form] = Form.useForm<CustomerFormValues>();
  const { message } = App.useApp();
  const isEdit = Boolean(customer);
  const type = Form.useWatch("type", form) as CustomerType | undefined;
  const owners = useCustomerOwners(open);
  const createCustomer = useCreateCustomer();
  const updateCustomer = useUpdateCustomer(customer?.id);
  const saving = createCustomer.isPending || updateCustomer.isPending;

  useEffect(() => {
    if (!open) return;

    if (customer) {
      form.setFieldsValue({
        type: customer.type,
        firstName: customer.firstName ?? undefined,
        lastName: customer.lastName ?? undefined,
        companyName: customer.companyName ?? undefined,
        economicCode: customer.economicCode ?? undefined,
        nationalId: customer.nationalId,
        mobile: customer.mobile,
        phone: customer.phone ?? undefined,
        email: customer.email ?? undefined,
        address: customer.address ?? undefined,
        isReseller: customer.isReseller,
        ownerUserId: customer.ownerUserId ?? undefined,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ type: "Natural", isReseller: false });
    }
  }, [customer, form, open]);

  const handleSubmit = async (values: CustomerFormValues) => {
    const isLegal = values.type === "Legal";
    const payload: CustomerPayload = {
      type: values.type,
      firstName: values.firstName?.trim() || null,
      lastName: values.lastName?.trim() || null,
      companyName: isLegal ? values.companyName?.trim() || null : null,
      economicCode: isLegal ? values.economicCode?.trim() || null : null,
      nationalId: values.nationalId.trim(),
      mobile: values.mobile.trim(),
      phone: values.phone?.trim() || null,
      email: values.email?.trim() || null,
      address: values.address?.trim() || null,
      ownerUserId: values.ownerUserId ?? null,
      parentCustomerId: customer?.parentCustomerId ?? null,
      isReseller: values.isReseller,
      groupId: customer?.group?.id ?? null,
      tagIds: customer?.tags.map((tag) => tag.id) ?? [],
    };

    try {
      const saved = isEdit && customer
        ? await updateCustomer.mutateAsync(payload)
        : await createCustomer.mutateAsync(payload);

      message.success(
        isEdit ? "مشتری با موفقیت ویرایش شد." : "مشتری با موفقیت ثبت شد.",
      );
      onSaved?.(saved);
      onClose();
    } catch (error) {
      applyServerErrors(error as AxiosError<ApiEnvelope<unknown>>, form, message);
    }
  };

  return (
    <Drawer
      title={isEdit ? "ویرایش مشتری" : "افزودن مشتری جدید"}
      placement="left"
      width={540}
      open={open}
      onClose={onClose}
      footer={
        <Space className="drawer-actions">
          <Button onClick={onClose}>انصراف</Button>
          <Button type="primary" loading={saving} onClick={() => form.submit()}>
            {isEdit ? "ذخیره تغییرات" : "ذخیره مشتری"}
          </Button>
        </Space>
      }
    >
      <Form
        form={form}
        layout="vertical"
        requiredMark="optional"
        onFinish={handleSubmit}
        initialValues={{ type: "Natural", isReseller: false }}
      >
        <Form.Item
          name="type"
          label="نوع مشتری"
          rules={[{ required: true, message: "نوع مشتری را انتخاب کنید." }]}
        >
          <Radio.Group optionType="button" buttonStyle="solid">
            <Radio.Button value="Natural">حقیقی</Radio.Button>
            <Radio.Button value="Legal">حقوقی</Radio.Button>
          </Radio.Group>
        </Form.Item>

        {type === "Legal" ? (
          <>
            <Form.Item
              name="companyName"
              label="نام شرکت"
              rules={[{ required: true, message: "نام شرکت را وارد کنید." }]}
            >
              <Input placeholder="نام شرکت" />
            </Form.Item>

            <Row gutter={12}>
              <Col span={12}>
                <Form.Item
                  name="firstName"
                  label="نام شخص رابط"
                  rules={[{ required: true, message: "نام شخص رابط را وارد کنید." }]}
                >
                  <Input placeholder="نام" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name="lastName"
                  label="نام خانوادگی شخص رابط"
                  rules={[{ required: true, message: "نام خانوادگی را وارد کنید." }]}
                >
                  <Input placeholder="نام خانوادگی" />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col span={12}>
                <Form.Item
                  name="nationalId"
                  label="شناسه ملی"
                  rules={[
                    { required: true, message: "شناسه ملی را وارد کنید." },
                    { min: 10, message: "شناسه ملی معتبر نیست." },
                  ]}
                >
                  <Input placeholder="شناسه ملی" dir="ltr" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="economicCode" label="کد اقتصادی">
                  <Input placeholder="کد اقتصادی" dir="ltr" />
                </Form.Item>
              </Col>
            </Row>
          </>
        ) : (
          <>
            <Row gutter={12}>
              <Col span={12}>
                <Form.Item
                  name="firstName"
                  label="نام"
                  rules={[{ required: true, message: "نام را وارد کنید." }]}
                >
                  <Input placeholder="نام" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name="lastName"
                  label="نام خانوادگی"
                  rules={[{ required: true, message: "نام خانوادگی را وارد کنید." }]}
                >
                  <Input placeholder="نام خانوادگی" />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item
              name="nationalId"
              label="کد ملی"
              rules={[
                { required: true, message: "کد ملی را وارد کنید." },
                { min: 10, message: "کد ملی معتبر نیست." },
              ]}
            >
              <Input placeholder="کد ملی" dir="ltr" />
            </Form.Item>
          </>
        )}

        <Typography.Title level={5} className="form-section-title">
          اطلاعات تماس
        </Typography.Title>

        <Row gutter={12}>
          <Col span={12}>
            <Form.Item
              name="mobile"
              label="موبایل"
              rules={[
                { required: true, message: "شماره موبایل الزامی است." },
                { pattern: /^09\d{9}$/, message: "شماره موبایل معتبر نیست." },
              ]}
            >
              <Input placeholder="09xxxxxxxxx" dir="ltr" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="phone" label="تلفن ثابت">
              <Input placeholder="021xxxxxxxx" dir="ltr" />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name="email"
          label="ایمیل"
          rules={[{ type: "email", message: "ایمیل معتبر نیست." }]}
        >
          <Input placeholder="name@example.com" dir="ltr" />
        </Form.Item>

        <Form.Item name="address" label="آدرس">
          <Input.TextArea rows={3} placeholder="آدرس کامل مشتری" />
        </Form.Item>

        <Form.Item
          name="isReseller"
          label="آیا این مشتری ریسلر است؟"
          valuePropName="checked"
        >
          <Switch checkedChildren="بله" unCheckedChildren="خیر" />
        </Form.Item>

        <Form.Item name="ownerUserId" label="کارشناس فروش (مالک مشتری)">
          <Select
            allowClear
            showSearch
            loading={owners.isLoading}
            placeholder="انتخاب کارشناس فروش"
            optionFilterProp="label"
            options={(owners.data ?? []).map((user) => ({
              value: user.id,
              label: `${user.fullName} (${user.username})`,
            }))}
          />
        </Form.Item>
      </Form>
    </Drawer>
  );
}

function applyServerErrors(
  error: AxiosError<ApiEnvelope<unknown>>,
  form: FormInstance<CustomerFormValues>,
  message: { error: (content: string) => void },
) {
  const payload = error.response?.data;
  const errors = payload?.errors?.filter(Boolean) ?? [];
  const findError = (keywords: string[]) =>
    errors.find((item) => keywords.some((keyword) => item.includes(keyword)));

  const mobileError = findError(["موبایل", "mobile"]);
  const nationalIdError = findError(["ملی", "national"]);
  const emailError = findError(["ایمیل", "email"]);

  const fields: { name: keyof CustomerFormValues; errors: string[] }[] = [];
  if (mobileError) fields.push({ name: "mobile", errors: [mobileError] });
  if (nationalIdError) fields.push({ name: "nationalId", errors: [nationalIdError] });
  if (emailError) fields.push({ name: "email", errors: [emailError] });

  if (fields.length > 0) {
    form.setFields(fields);
  } else {
    message.error(payload?.message ?? getApiErrorMessage(error));
  }
}
