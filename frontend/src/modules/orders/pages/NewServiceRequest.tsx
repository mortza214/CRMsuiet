import { useState } from "react";
import { ArrowRightOutlined } from "@ant-design/icons";
import { Alert, App, Button, Card, Form, Select, Space, Typography } from "antd";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import type { ApiEnvelope } from "@/modules/auth/types";
import type { ServiceRequestResult } from "@/modules/orders/types";
import { portalApi, getPortalErrorMessage } from "@/modules/portal/api";
import { api } from "@/shared/api/client";
import { WorkflowFormRenderer, validateWorkflowForm } from "@/modules/workflow/components/WorkflowFormRenderer";
import { useOrderCustomers } from "@/modules/orders/hooks/useOrders";

interface IntakeOption {
  id: string;
  name: string;
  categoryId: string;
  categoryName: string;
  schemaJson?: string | null;
  workflowName: string;
}

export function NewServiceRequest({ portal = false }: { portal?: boolean }) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const client = portal ? portalApi : api;
  const [categoryId, setCategoryId] = useState<string>();
  const [serviceId, setServiceId] = useState<string>();
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [uploading, setUploading] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<string>();
  const [customerSearch, setCustomerSearch] = useState("");
  const customers = useOrderCustomers(customerSearch, !portal && !id);
  const options = useQuery({
    queryKey: [portal ? "portal" : "sales", "intake-options"],
    queryFn: async () => (await client.get<ApiEnvelope<IntakeOption[]>>("/service-intake/options")).data.data ?? [],
  });
  const service = options.data?.find(item => item.id === serviceId);
  const categories = [...new Map((options.data ?? []).map(item => [item.categoryId, { value: item.categoryId, label: item.categoryName }])).values()];
  const create = useMutation({
    mutationFn: async () => {
      const response = await client.post<ApiEnvelope<ServiceRequestResult>>(
        portal ? "/portal/sales/requests" : "/sales/service-requests",
        { customerId: id ?? selectedCustomer, serviceId, attributesJson: JSON.stringify(values) },
      );
      return response.data.data!;
    },
    onSuccess: async result => {
      await Promise.all(["portal", "workflow", "orders", "sales"].map(key => queryClient.invalidateQueries({ queryKey: [key] })));
      message.success(portal ? `درخواست ${result.orderNumber} ثبت شد و منتظر تأیید فروش است.` : `درخواست ${result.orderNumber} ثبت و به مدیر فنی ارسال شد.`);
      navigate(portal ? "/portal/services" : `/workflow/instances/${result.workflowInstanceId}`);
    },
    onError: error => { if (portal) message.error(getPortalErrorMessage(error)); },
  });
  const submit = () => {
    if (uploading) { message.info("تا پایان بارگذاری فایل صبر کنید."); return; }
    if (!portal && !id && !selectedCustomer) { message.error("مشتری را انتخاب کنید."); return; }
    if (!service) { message.error("گروه و نوع سرویس را انتخاب کنید."); return; }
    const error = validateWorkflowForm(service.schemaJson, values);
    if (error) { message.error(error); return; }
    create.mutate();
  };
  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="detail-heading"><Space>
        <Button type="text" icon={<ArrowRightOutlined />} onClick={() => navigate(portal ? "/portal/services" : id ? `/customers/${id}` : "/requests")} aria-label="بازگشت" />
        <div><Typography.Title level={2}>درخواست سرویس جدید</Typography.Title>
          <Typography.Text type="secondary">انتخاب سرویس و ثبت مشخصات درخواست</Typography.Text></div>
      </Space></div>
      <Card title="مشخصات درخواست" variant="borderless" className="detail-card" loading={options.isLoading}>
        {options.isError && <Alert type="error" message="دریافت سرویس‌ها ناموفق بود." action={<Button onClick={() => void options.refetch()}>تلاش دوباره</Button>} />}
        {!options.isLoading && options.data?.length === 0 && <Alert type="info" message="سرویس قابل درخواست وجود ندارد؛ مدیر سیستم باید یک گردش کار منتشرشده به سرویس فعال متصل کند." />}
        <Form layout="vertical" onFinish={submit}>
          {!portal && !id && <Form.Item label="مشتری" required><Select showSearch filterOption={false}
            onSearch={setCustomerSearch} value={selectedCustomer} onChange={setSelectedCustomer} loading={customers.isFetching}
            options={(customers.data ?? []).map(customer => ({ value: customer.id, label: `${customer.displayName} — ${customer.mobile}` }))}
            placeholder="جستجو و انتخاب مشتری" /></Form.Item>}
          <Form.Item label="گروه سرویس" required><Select value={categoryId} options={categories} placeholder="انتخاب گروه سرویس"
            onChange={value => { setCategoryId(value); setServiceId(undefined); setValues({}); }} /></Form.Item>
          <Form.Item label="نوع سرویس" required><Select value={serviceId} disabled={!categoryId} placeholder="انتخاب نوع سرویس"
            options={(options.data ?? []).filter(item => item.categoryId === categoryId).map(item => ({ value: item.id, label: item.name }))}
            onChange={value => { setServiceId(value); setValues({ cooperation_type: "invoice", billing_period_months: "1", extra_ip_count: 0 }); }} /></Form.Item>
          {service && <>
            <Alert type="info" showIcon style={{ marginBottom: 16 }} message={portal ? "درخواست ابتدا توسط فروش بررسی می‌شود؛ سپس امکان‌سنجی فنی انجام خواهد شد." : "ثبت این درخواست به منزله تأیید اولیه فروش است؛ درخواست برای امکان‌سنجی به مدیر فنی ارجاع می‌شود."} />
            <WorkflowFormRenderer key={service.id} portal={portal} onUploadingChange={setUploading} schemaJson={service.schemaJson} values={values} onDataChange={setValues} />
            <Button style={{ marginTop: 20 }} type="primary" htmlType="submit" loading={create.isPending}>ثبت درخواست</Button>
          </>}
        </Form>
      </Card>
    </Space>
  );
}
