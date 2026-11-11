import { useState } from "react";
import {
  EyeOutlined,
  MessageOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from "antd";
import { useNavigate } from "react-router-dom";
import {
  ticketPriorityMeta,
  ticketPriorityOptions,
  ticketStatusMeta,
} from "@/modules/tickets/constants";
import type {
  TicketListItem,
  TicketPriority,
  TicketStatus,
} from "@/modules/tickets/types";
import { getPortalErrorMessage } from "@/modules/portal/api";
import {
  usePortalCreateTicket,
  usePortalDepartments,
  usePortalServices,
  usePortalTickets,
} from "@/modules/portal/hooks/usePortal";

interface PortalTicketForm {
  subject: string;
  departmentId: string;
  serviceId?: string;
  priority: TicketPriority;
  message: string;
}

export function PortalTickets() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [createOpen, setCreateOpen] = useState(false);
  const [form] = Form.useForm<PortalTicketForm>();
  const tickets = usePortalTickets();
  const departments = usePortalDepartments();
  const services = usePortalServices();
  const createTicket = usePortalCreateTicket();

  const openCreate = () => {
    form.resetFields();
    form.setFieldsValue({ priority: "Medium" });
    setCreateOpen(true);
  };

  const submit = async (values: PortalTicketForm) => {
    try {
      const ticket = await createTicket.mutateAsync({
        subject: values.subject.trim(),
        departmentId: values.departmentId,
        serviceId: values.serviceId ?? null,
        priority: values.priority,
        message: values.message.trim(),
      });
      message.success("تیکت با موفقیت ثبت شد.");
      setCreateOpen(false);
      navigate(`/portal/tickets/${ticket.id}`);
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    }
  };

  const columns: TableColumnsType<TicketListItem> = [
    {
      title: "شماره تیکت",
      dataIndex: "ticketNumber",
      key: "ticketNumber",
      width: 155,
      render: (value: string) => (
        <Typography.Text strong dir="ltr">
          {value}
        </Typography.Text>
      ),
    },
    {
      title: "موضوع",
      dataIndex: "subject",
      key: "subject",
      render: (value: string) => value,
    },
    {
      title: "اولویت",
      dataIndex: "priority",
      key: "priority",
      width: 110,
      render: (value: TicketPriority) => {
        const meta = ticketPriorityMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      width: 145,
      render: (value: TicketStatus) => {
        const meta = ticketStatusMeta[value];
        return (
          <Tag color={meta.color} bordered={false}>
            {meta.label}
          </Tag>
        );
      },
    },
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
      width: 125,
    },
    {
      title: "عملیات",
      key: "actions",
      width: 100,
      render: (_value, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => navigate(`/portal/tickets/${record.id}`)}
        >
          مشاهده
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <div>
          <Typography.Title level={2}>تیکت‌های من</Typography.Title>
          <Typography.Text type="secondary">
            ثبت درخواست پشتیبانی و پیگیری پاسخ‌ها
          </Typography.Text>
        </div>
        <Space>
          <span className="dashboard-badge">
            <MessageOutlined /> {tickets.data?.length ?? 0} تیکت
          </span>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            ثبت تیکت جدید
          </Button>
        </Space>
      </div>

      <Card className="portal-table-card" variant="borderless">
        <Table<TicketListItem>
          rowKey="id"
          columns={columns}
          dataSource={tickets.data ?? []}
          loading={tickets.isLoading}
          scroll={{ x: 850 }}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          locale={{ emptyText: "تیکتی ثبت نشده است." }}
        />
      </Card>

      <Modal
        title="ثبت تیکت جدید"
        open={createOpen}
        onCancel={() => setCreateOpen(false)}
        onOk={() => form.submit()}
        okText="ثبت تیکت"
        cancelText="انصراف"
        confirmLoading={createTicket.isPending}
      >
        <Form form={form} layout="vertical" onFinish={submit}>
          <Form.Item
            name="subject"
            label="موضوع"
            rules={[{ required: true, message: "موضوع را وارد کنید." }]}
          >
            <Input placeholder="موضوع درخواست" maxLength={200} showCount />
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
            name="priority"
            label="اولویت"
            rules={[{ required: true, message: "اولویت را انتخاب کنید." }]}
          >
            <Select options={ticketPriorityOptions} />
          </Form.Item>

          <Form.Item
            name="message"
            label="متن پیام"
            rules={[{ required: true, message: "متن پیام را وارد کنید." }]}
          >
            <Input.TextArea rows={5} placeholder="شرح مشکل یا درخواست..." />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
