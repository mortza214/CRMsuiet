import { useState } from "react";
import {
  App,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Space,
  Table,
  Tag,
  Typography,
  Upload,
  type TableColumnsType,
} from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { ApiEnvelope } from "@/modules/auth/types";
import { getPortalErrorMessage, portalApi } from "@/modules/portal/api";
import { usePortalInvoices } from "@/modules/portal/hooks/usePortal";
import type { WorkflowTask } from "@/modules/workflow/types";
import { formatCurrency, resolveApiFileUrl } from "@/shared/utils/format";

interface TaskPage {
  items: WorkflowTask[];
  totalCount: number;
}

export function PortalRequests() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<WorkflowTask | null>(null);
  const [manualOpen, setManualOpen] = useState(false);
  const [receiptUrl, setReceiptUrl] = useState<string | null>(null);
  const [manualForm] = Form.useForm<{ referenceNumber: string; amount: number }>();
  const [uploading, setUploading] = useState(false);
  const [busy, setBusy] = useState(false);

  const tasks = useQuery({
    queryKey: ["portal", "workflow-tasks"],
    queryFn: async () => {
      const response = await portalApi.get<ApiEnvelope<TaskPage>>(
        "/workflow/my-tasks",
        { params: { page: 1, pageSize: 50 } },
      );
      return response.data.data;
    },
  });
  const invoices = usePortalInvoices();
  const invoice = invoices.data?.find(
    (item) => item.orderId === selected?.entityId,
  );

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ["portal"] });
  };

  const execute = async (
    transitionId: string,
    actionData: Record<string, unknown>,
  ) => {
    if (!selected) return;
    setBusy(true);
    try {
      await portalApi.post(
        `/portal/workflow/instances/${selected.instanceId}/action`,
        {
          transitionId,
          actionDataJson: JSON.stringify(actionData),
        },
      );
      message.success("درخواست شما ثبت شد.");
      setSelected(null);
      await refresh();
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  const uploadReceipt = async (file: File) => {
    setUploading(true);
    try {
      const body = new FormData();
      body.append("file", file);
      const response = await portalApi.post<ApiEnvelope<{ fileUrl: string }>>(
        "/files/upload",
        body,
        { headers: { "Content-Type": "multipart/form-data" } },
      );
      setReceiptUrl(response.data.data?.fileUrl ?? null);
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    } finally {
      setUploading(false);
    }
  };

  const submitManual = async (values: {
    referenceNumber: string;
    amount: number;
  }) => {
    if (!invoice || !receiptUrl) {
      message.warning("تصویر فیش را آپلود کنید.");
      return;
    }
    try {
      await portalApi.post("/portal/payments/manual", {
        invoiceId: invoice.id,
        amount: values.amount,
        paymentMethod: "ManualTransfer",
        referenceNumber: values.referenceNumber,
        receiptImageUrl: receiptUrl,
      });
      message.success("فیش ثبت شد و در انتظار تأیید مالی است.");
      setManualOpen(false);
      setReceiptUrl(null);
      manualForm.resetFields();
      await refresh();
    } catch (error) {
      message.error(getPortalErrorMessage(error));
    }
  };

  const approveTransition = selected?.availableTransitions.find(
    (transition) =>
      transition.name.includes("تایید") && !transition.name.includes("انصراف"),
  );
  const cancelTransition = selected?.availableTransitions.find((transition) =>
    transition.name.includes("انصراف"),
  );

  const columns: TableColumnsType<WorkflowTask> = [
    { title: "فرایند", dataIndex: "definitionName", key: "definitionName" },
    { title: "مرحله", dataIndex: "currentStateName", key: "currentStateName" },
    {
      title: "وضعیت",
      dataIndex: "status",
      key: "status",
      render: (value: string) => <Tag color="blue">{value}</Tag>,
    },
    {
      title: "تاریخ",
      dataIndex: "createdAtJalali",
      key: "createdAtJalali",
    },
    {
      title: "عملیات",
      key: "action",
      render: (_value, record) => (
        <Button type="primary" size="small" onClick={() => setSelected(record)}>
          مشاهده و اقدام
        </Button>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={18} className="module-stack">
      <div className="portal-page-heading">
        <div>
          <Typography.Title level={2}>درخواست‌های در انتظار من</Typography.Title>
          <Typography.Text type="secondary">
            تسک‌هایی که نیاز به تایید یا اقدام شما دارند
          </Typography.Text>
        </div>
      </div>

      <Card className="portal-table-card" variant="borderless">
        <Table<WorkflowTask>
          rowKey="id"
          columns={columns}
          dataSource={tasks.data?.items ?? []}
          loading={tasks.isLoading}
          pagination={false}
          locale={{ emptyText: "درخواستی در انتظار شما نیست." }}
        />
      </Card>

      <Modal
        title={selected ? `جزئیات: ${selected.definitionName}` : ""}
        open={Boolean(selected)}
        onCancel={() => setSelected(null)}
        footer={null}
        width={640}
      >
        {selected && (
          <Space direction="vertical" size={12} style={{ width: "100%" }}>
            <Typography.Text>
              مرحله: <strong>{selected.currentStateName}</strong>
            </Typography.Text>
            {invoice ? (
              <Card size="small" title="فاکتور">
                <Space direction="vertical">
                  <Typography.Text>
                    شماره: <strong dir="ltr">{invoice.invoiceNumber}</strong>
                  </Typography.Text>
                  <Typography.Text>
                    مبلغ: <strong>{formatCurrency(invoice.totalAmount)}</strong>
                  </Typography.Text>
                  <Tag color={invoice.status === "Paid" ? "green" : "orange"}>
                    {invoice.status === "Paid" ? "پرداخت شده" : "پرداخت نشده"}
                  </Tag>
                </Space>
              </Card>
            ) : (
              <Typography.Text type="secondary">فاکتوری یافت نشد.</Typography.Text>
            )}

            <Space wrap>
              {invoice && invoice.requiresPayment !== false && invoice.status !== "Paid" && (
                <>
                  <Button
                    type="primary"
                    onClick={async () => {
                      try {
                        const response = await portalApi.post<
                          ApiEnvelope<{ paymentUrl: string }>
                        >("/portal/payments/saman/initiate", {
                          invoiceId: invoice.id,
                          callbackUrl: `${window.location.origin}/portal/requests`,
                        });
                        if (response.data.data?.paymentUrl) {
                          window.open(response.data.data.paymentUrl, "_blank");
                        }
                      } catch (error) {
                        message.error(getPortalErrorMessage(error));
                      }
                    }}
                  >
                    پرداخت آنلاین
                  </Button>
                  <Button onClick={() => setManualOpen(true)}>ثبت فیش</Button>
                </>
              )}
              {approveTransition && (
                <Button
                  type="primary"
                  disabled={Boolean(
                    invoice &&
                      invoice.requiresPayment !== false &&
                      invoice.status !== "Paid",
                  )}
                  loading={busy}
                  onClick={() =>
                    void execute(approveTransition.id, {
                      approved: true,
                      requiresPayment: !invoice,
                    })
                  }
                >
                  تایید
                </Button>
              )}
              {cancelTransition && (
                <Button
                  danger
                  loading={busy}
                  onClick={() =>
                    void execute(cancelTransition.id, { decision: "cancel" })
                  }
                >
                  انصراف
                </Button>
              )}
            </Space>
          </Space>
        )}
      </Modal>

      <Modal
        title="ثبت فیش پرداخت"
        open={manualOpen}
        onCancel={() => setManualOpen(false)}
        onOk={() => manualForm.submit()}
        okText="ثبت فیش"
        cancelText="انصراف"
      >
        <Form form={manualForm} layout="vertical" onFinish={submitManual}>
          <Form.Item
            name="referenceNumber"
            label="شماره پیگیری"
            rules={[{ required: true }]}
          >
            <Input dir="ltr" />
          </Form.Item>
          <Form.Item
            name="amount"
            label="مبلغ"
            initialValue={invoice?.totalAmount}
            rules={[{ required: true }]}
          >
            <InputNumber style={{ width: "100%" }} min={1} />
          </Form.Item>
          <Upload
            accept=".jpg,.jpeg,.png,.pdf"
            maxCount={1}
            showUploadList={false}
            customRequest={({ file, onSuccess, onError }) => {
              void uploadReceipt(file as File).then(
                () => onSuccess?.(undefined),
                (error) => onError?.(error as Error),
              );
            }}
          >
            <Button icon={<UploadOutlined />} loading={uploading}>
              آپلود تصویر فیش
            </Button>
          </Upload>
          {receiptUrl && (
            <Typography.Paragraph style={{ marginTop: 8 }}>
              فایل آپلود شد.{" "}
              <Typography.Link href={resolveApiFileUrl(receiptUrl)} target="_blank">
                مشاهده
              </Typography.Link>
            </Typography.Paragraph>
          )}
        </Form>
      </Modal>
    </Space>
  );
}
