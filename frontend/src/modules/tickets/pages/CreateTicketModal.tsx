import { useEffect, useState } from "react";
import {
  App,
  Form,
  Input,
  Modal,
  Select,
} from "antd";
import type { CustomerListItem } from "@/modules/customers/types";
import { useOrderCustomers, useOrderServices } from "@/modules/orders/hooks/useOrders";
import { ticketPriorityOptions } from "@/modules/tickets/constants";
import {
  useCreateTicket,
  useDepartments,
} from "@/modules/tickets/hooks/useTickets";
import type {
  TicketDetail,
  TicketPriority,
  TicketSource,
} from "@/modules/tickets/types";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";

interface CreateTicketModalProps {
  open: boolean;
  onClose: () => void;
  onCreated?: (ticket: TicketDetail) => void;
}

interface TicketForm {
  customerId: string;
  serviceId?: string;
  subject: string;
  departmentId: string;
  priority: TicketPriority;
  source: TicketSource;
  message: string;
}

export function CreateTicketModal({
  open,
  onClose,
  onCreated,
}: CreateTicketModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<TicketForm>();
  const [customerSearch, setCustomerSearch] = useState("");
  const debouncedCustomerSearch = useDebouncedValue(customerSearch, 350);
  const customers = useOrderCustomers(debouncedCustomerSearch);
  const services = useOrderServices("");
  const departments = useDepartments();
  const createTicket = useCreateTicket();

  useEffect(() => {
    if (!open) return;
    form.resetFields();
    form.setFieldsValue({ priority: "Medium", source: "Phone" });
    setCustomerSearch("");
  }, [form, open]);

  const submit = async (values: TicketForm) => {
    try {
      const ticket = await createTicket.mutateAsync({
        customerId: values.customerId,
        serviceId: values.serviceId ?? null,
        subject: values.subject.trim(),
        departmentId: values.departmentId,
        priority: values.priority,
        source: values.source,
        message: values.message.trim(),
      });
      message.success("تیکت با موفقیت ثبت شد.");
      onCreated?.(ticket);
      onClose();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  return (
    <Modal
      title="ثبت تیکت جدید"
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText="ثبت تیکت"
      cancelText="انصراف"
      confirmLoading={createTicket.isPending}
      width={640}
    >
      <Form form={form} layout="vertical" onFinish={submit}>
        <Form.Item
          name="customerId"
          label="مشتری"
          rules={[{ required: true, message: "مشتری را انتخاب کنید." }]}
        >
          <Select
            showSearch
            filterOption={false}
            onSearch={setCustomerSearch}
            loading={customers.isLoading}
            placeholder="جستجو و انتخاب مشتری..."
            options={(customers.data ?? []).map((customer: CustomerListItem) => ({
              value: customer.id,
              label: `${customer.displayName} — ${customer.mobile}`,
            }))}
          />
        </Form.Item>

        <Form.Item name="serviceId" label="سرویس مرتبط (اختیاری)">
          <Select
            allowClear
            showSearch
            optionFilterProp="label"
            loading={services.isLoading}
            placeholder="انتخاب سرویس"
            options={(services.data ?? []).map((service) => ({
              value: service.id,
              label: `${service.name} (${service.code})`,
            }))}
          />
        </Form.Item>

        <Form.Item
          name="subject"
          label="موضوع"
          rules={[{ required: true, message: "موضوع تیکت را وارد کنید." }]}
        >
          <Input placeholder="موضوع تیکت" maxLength={200} showCount />
        </Form.Item>

        <Form.Item
          name="departmentId"
          label="دپارتمان"
          rules={[{ required: true, message: "دپارتمان را انتخاب کنید." }]}
        >
          <Select
            loading={departments.isLoading}
            placeholder="انتخاب دپارتمان"
            options={(departments.data ?? []).map((department) => ({
              value: department.id,
              label: department.name,
            }))}
          />
        </Form.Item>

        <Form.Item
          name="priority"
          label="اولویت"
          rules={[{ required: true, message: "اولویت را انتخاب کنید." }]}
        >
          <Select options={ticketPriorityOptions} />
        </Form.Item>

        <Form.Item
          name="source"
          label="منبع تیکت"
          rules={[{ required: true, message: "منبع تیکت را انتخاب کنید." }]}
        >
          <Select
            options={[
              { value: "Phone", label: "تماس تلفنی" },
              { value: "Internal", label: "داخلی" },
            ]}
          />
        </Form.Item>

        <Form.Item
          name="message"
          label="متن پیام اولیه"
          rules={[{ required: true, message: "متن پیام را وارد کنید." }]}
        >
          <Input.TextArea rows={5} placeholder="توضیحات اولیه مشتری..." />
        </Form.Item>
      </Form>
    </Modal>
  );
}
