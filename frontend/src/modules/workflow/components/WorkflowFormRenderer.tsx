import { useMemo, useRef, useState } from "react";
import type { Dayjs } from "dayjs";
import {
  Checkbox,
  App,
  Button,
  DatePicker,
  Input,
  InputNumber,
  Select,
  Space,
  Typography,
  Upload,
} from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { api } from "@/shared/api/client";
import { portalApi } from "@/modules/portal/api";
import { resolveApiFileUrl } from "@/shared/utils/format";
import type { ApiEnvelope } from "@/modules/auth/types";
import { useFormTemplateByCode } from "@/modules/forms/hooks/useForms";

interface SchemaField {
  key: string;
  label: string;
  type: string;
  required?: boolean;
  min?: number;
  max?: number;
  options?: Array<{ value: string; label: string }>;
  visibleIf?: { field: string; equals?: unknown; notEquals?: unknown };
}

interface Props {
  portal?: boolean;
  onUploadingChange?: (uploading: boolean) => void;
  formCode?: string;
  schemaJson?: string | null;
  readOnly?: boolean;
  values?: Record<string, unknown>;
  onChange?: (values: Record<string, unknown>) => void;
  onDataChange?: (values: Record<string, unknown>) => void;
}

function parseFields(schemaJson?: string | null): SchemaField[] {
  if (!schemaJson) return [];
  try {
    const schema = JSON.parse(schemaJson) as { fields?: Array<Record<string, unknown>> };
    return (schema.fields ?? []).map((field) => ({
      key: String(field.key ?? ""),
      label: String(field.label ?? field.key ?? ""),
      type: String(field.type ?? "text"),
      required: Boolean(field.required),
      min: typeof field.min === "number" ? field.min : undefined,
      max: typeof field.max === "number" ? field.max : undefined,
      options: Array.isArray(field.options)
        ? field.options.map((option) => {
            const item = option as Record<string, unknown>;
            return { value: String(item.value ?? ""), label: String(item.label ?? item.value ?? "") };
          })
        : undefined,
      visibleIf:
        field.visibleIf && typeof field.visibleIf === "object"
          ? (field.visibleIf as SchemaField["visibleIf"])
          : undefined,
    }));
  } catch {
    return [];
  }
}

function visible(field: SchemaField, values: Record<string, unknown>) {
  if (!field.visibleIf) return true;
  const actual = values[field.visibleIf.field];
  if ("equals" in field.visibleIf) return actual === field.visibleIf.equals;
  if ("notEquals" in field.visibleIf) return actual !== field.visibleIf.notEquals;
  return true;
}

export function validateWorkflowForm(schemaJson: string | null | undefined, values: Record<string, unknown>): string | null {
  const fields = parseFields(schemaJson);
  for (const field of fields.filter(item => visible(item, values))) {
    const value = values[field.key];
    if (field.required && (value === undefined || value === null || value === "" || (typeof value === "string" && !value.trim())))
      return `فیلد «${field.label}» الزامی است.`;
    if (typeof value === "number" && ((field.min !== undefined && value < field.min) || (field.max !== undefined && value > field.max)))
      return `مقدار «${field.label}» خارج از محدوده مجاز است.`;
  }
  return null;
}

export function WorkflowFormRenderer({
  portal = false,
  onUploadingChange,
  formCode,
  schemaJson,
  readOnly = false,
  values,
  onChange,
  onDataChange,
}: Props) {
  const { message } = App.useApp();
  const [uploading, setUploading] = useState(false);
  const template = useFormTemplateByCode(formCode);
  const [internalValues, setInternalValues] = useState<Record<string, unknown>>({});
  const actualValues = values ?? internalValues;
  const valuesRef = useRef(actualValues);
  valuesRef.current = actualValues;
  const actualSchema = schemaJson ?? template.data?.schemaJson;
  const fields = useMemo(() => parseFields(actualSchema), [actualSchema]);

  if (template.isLoading && !actualSchema) {
    return <Typography.Text type="secondary">در حال دریافت فرم...</Typography.Text>;
  }

  if (fields.length === 0) {
    return <Typography.Text type="secondary">فرمی برای این مرحله تعریف نشده است.</Typography.Text>;
  }

  const update = (key: string, value: unknown) => {
    const next = { ...valuesRef.current, [key]: value };
    valuesRef.current = next;
    setInternalValues(next);
    onChange?.(next);
    onDataChange?.(next);
  };

  return (
    <Space direction="vertical" size={12} style={{ width: "100%" }}>
      {fields
        .filter((field) => visible(field, actualValues))
        .map((field) => (
          <div key={field.key}>
            <Typography.Text>
              {field.label}
              {field.required && <span style={{ color: "#ff4d4f" }}> *</span>}
            </Typography.Text>
            <div style={{ marginTop: 4 }}>
              {field.options?.length ? (
                <Select
                  style={{ width: "100%" }}
                  value={actualValues[field.key] as string | undefined}
                  onChange={(value) => update(field.key, value)}
                  options={field.options}
                  placeholder={field.label}
                  allowClear={!field.required}
                  disabled={readOnly}
                />
              ) : field.type === "number" ? (
                <InputNumber
                  min={field.min}
                  max={field.max}
                  style={{ width: "100%" }}
                  value={typeof actualValues[field.key] === "number" ? (actualValues[field.key] as number) : undefined}
                  onChange={(value) => update(field.key, value)}
                  placeholder={field.label}
                  disabled={readOnly}
                />
              ) : field.type === "checkbox" ? (
                <Checkbox
                  checked={Boolean(actualValues[field.key])}
                  disabled={readOnly}
                  onChange={(event) => update(field.key, event.target.checked)}
                >
                  {field.label}
                </Checkbox>
              ) : field.type === "date" ? (
                <DatePicker
                  style={{ width: "100%" }}
                  disabled={readOnly}
                  onChange={(value: Dayjs | null) =>
                    update(field.key, value ? value.toISOString() : null)
                  }
                />
              ) : field.type === "file" ? (
                <Space direction="vertical">
                  {!readOnly && <Upload accept=".jpg,.jpeg,.png,.pdf" showUploadList={false}
                    customRequest={({ file, onSuccess, onError }) => {
                      setUploading(true);
                      onUploadingChange?.(true);
                      const body = new FormData(); body.append("file", file as File);
                      void (portal ? portalApi : api).post<ApiEnvelope<{ fileUrl: string }>>("/files/upload", body,
                        { headers: { "Content-Type": "multipart/form-data" } })
                        .then(response => { update(field.key, response.data.data?.fileUrl); onSuccess?.(undefined); })
                        .catch(error => { message.error("بارگذاری فایل ناموفق بود."); onError?.(error); })
                        .finally(() => { setUploading(false); onUploadingChange?.(false); });
                    }}><Button loading={uploading} icon={<UploadOutlined />}>بارگذاری عکس / فایل</Button></Upload>}
                  {typeof actualValues[field.key] === "string" && /^\/(?:uploads|api\/files)\//.test(String(actualValues[field.key])) &&
                    <Typography.Link href={resolveApiFileUrl(String(actualValues[field.key]))} target="_blank" rel="noopener noreferrer">مشاهده پیوست</Typography.Link>}
                </Space>
              ) : field.type === "textarea" ? (
                <Input.TextArea
                  rows={3}
                  value={String(actualValues[field.key] ?? "")}
                  onChange={(event) => update(field.key, event.target.value)}
                  placeholder={field.label}
                  disabled={readOnly}
                />
              ) : (
                <Input
                  value={String(actualValues[field.key] ?? "")}
                  onChange={(event) => update(field.key, event.target.value)}
                  placeholder={field.label}
                  type={field.type === "file" ? "text" : field.type}
                  disabled={readOnly}
                />
              )}
            </div>
          </div>
        ))}
    </Space>
  );
}
