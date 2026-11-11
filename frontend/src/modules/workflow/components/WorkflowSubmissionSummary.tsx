import { Card, Descriptions, Empty, Space, Tag, Typography } from "antd";
import type { FormSubmission, FormTemplate } from "@/modules/forms/types";

interface Props {
  submissions: FormSubmission[];
  templates: FormTemplate[];
}

interface SchemaField {
  key: string;
  label: string;
  options?: Array<{ value: string; label: string }>;
}

function parseFields(template: FormTemplate | undefined): SchemaField[] {
  if (!template?.schemaJson) return [];
  try {
    const schema = JSON.parse(template.schemaJson) as {
      fields?: Array<Record<string, unknown>>;
    };
    return (schema.fields ?? []).map((field) => ({
      key: String(field.key ?? ""),
      label: String(field.label ?? field.key ?? ""),
      options: Array.isArray(field.options)
        ? field.options.map((option) => {
            const item = option as Record<string, unknown>;
            return {
              value: String(item.value ?? ""),
              label: String(item.label ?? item.value ?? ""),
            };
          })
        : undefined,
    }));
  } catch {
    return [];
  }
}

function parseValues(json: string): Record<string, unknown> {
  try {
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return {};
  }
}

export function WorkflowSubmissionSummary({ submissions, templates }: Props) {
  if (submissions.length === 0) {
    return <Empty description="داده فرمی ثبت نشده است." />;
  }

  return (
    <Space direction="vertical" size={12} style={{ width: "100%" }}>
      {submissions.map((submission) => {
        const template = templates.find(
          (item) => item.id === submission.formTemplateId,
        );
        const fields = parseFields(template);
        const values = parseValues(submission.submittedDataJson);
        return (
          <Card
            key={submission.id}
            size="small"
            title={submission.formTitle ?? template?.title ?? "فرم"}
            extra={<Tag>{submission.submittedAtJalali}</Tag>}
          >
            <Descriptions bordered size="small" column={{ xs: 1, md: 2 }}>
              {fields.map((field) => {
                const value = values[field.key];
                if (value === null || value === undefined || value === "") {
                  return null;
                }

                const readable = field.options?.find(
                  (option) => option.value === String(value),
                )?.label;
                return (
                  <Descriptions.Item key={field.key} label={field.label}>
                    {readable ?? String(value)}
                  </Descriptions.Item>
                );
              })}
            </Descriptions>
            {fields.length === 0 && (
              <Typography.Text type="secondary">
                قالب این فرم یافت نشد.
              </Typography.Text>
            )}
          </Card>
        );
      })}
    </Space>
  );
}
