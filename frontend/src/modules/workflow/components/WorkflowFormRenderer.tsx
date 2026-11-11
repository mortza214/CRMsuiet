import { useMemo } from "react";
import type { Dayjs } from "dayjs";
import {
  Checkbox,
  DatePicker,
  Input,
  InputNumber,
  Select,
  Space,
  Typography,
} from "antd";

interface SchemaField {
  key: string;
  label: string;
  type: string;
  required?: boolean;
  options?: Array<{ value: string; label: string }>;
  visibleIf?: { field: string; equals?: unknown; notEquals?: unknown };
}

interface Props {
  schemaJson?: string | null;
  values: Record<string, unknown>;
  onChange: (values: Record<string, unknown>) => void;
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

export function WorkflowFormRenderer({ schemaJson, values, onChange }: Props) {
  const fields = useMemo(() => parseFields(schemaJson), [schemaJson]);
  if (fields.length === 0) {
    return <Typography.Text type="secondary">فرمی برای این مرحله تعریف نشده است.</Typography.Text>;
  }

  const update = (key: string, value: unknown) => {
    onChange({ ...values, [key]: value });
  };

  return (
    <Space direction="vertical" size={12} style={{ width: "100%" }}>
      {fields
        .filter((field) => visible(field, values))
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
                  value={values[field.key] as string | undefined}
                  onChange={(value) => update(field.key, value)}
                  options={field.options}
                  placeholder={field.label}
                  allowClear={!field.required}
                />
              ) : field.type === "number" ? (
                <InputNumber
                  style={{ width: "100%" }}
                  value={typeof values[field.key] === "number" ? (values[field.key] as number) : undefined}
                  onChange={(value) => update(field.key, value)}
                  placeholder={field.label}
                />
              ) : field.type === "checkbox" ? (
                <Checkbox
                  checked={Boolean(values[field.key])}
                  onChange={(event) => update(field.key, event.target.checked)}
                >
                  {field.label}
                </Checkbox>
              ) : field.type === "date" ? (
                <DatePicker
                  style={{ width: "100%" }}
                  onChange={(value: Dayjs | null) =>
                    update(field.key, value ? value.toISOString() : null)
                  }
                />
              ) : field.type === "textarea" ? (
                <Input.TextArea
                  rows={3}
                  value={String(values[field.key] ?? "")}
                  onChange={(event) => update(field.key, event.target.value)}
                  placeholder={field.label}
                />
              ) : (
                <Input
                  value={String(values[field.key] ?? "")}
                  onChange={(event) => update(field.key, event.target.value)}
                  placeholder={field.label}
                  type={field.type === "file" ? "text" : field.type}
                />
              )}
            </div>
          </div>
        ))}
    </Space>
  );
}
