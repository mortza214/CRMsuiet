export type FormEntityType =
  | "None"
  | "Customer"
  | "Order"
  | "Service"
  | "DataCenter"
  | "Requirement"
  | "Feasibility"
  | "Ticket";

export type FormDefinitionStatus = "Draft" | "Published" | "Archived";

export type FormFieldType =
  | "Text"
  | "TextArea"
  | "Number"
  | "Select"
  | "MultiSelect"
  | "Checkbox"
  | "Radio"
  | "Date"
  | "Email"
  | "Phone"
  | "File";

export type FormSubmissionStatus = "Draft" | "Submitted" | "Approved" | "Rejected";

export interface FormField {
  id: string;
  key: string;
  label: string;
  fieldType: FormFieldType;
  isRequired: boolean;
  defaultValue?: string | null;
  optionsJson?: string | null;
  minValue?: string | null;
  maxValue?: string | null;
  regexPattern?: string | null;
  order: number;
  visibleIfJson?: string | null;
}

export interface FormFieldInput {
  key: string;
  label: string;
  type: string;
  required?: boolean;
  defaultValue?: string;
  min?: number | string;
  max?: number | string;
  regex?: string;
  order?: number;
  options?: Array<{ value: string; label: string }>;
  visibleIf?: { field: string; equals: string } | null;
}

export interface FormTemplate {
  id: string;
  code: string;
  title: string;
  schemaJson: string;
  version: number;
  isActive: boolean;
  status: FormDefinitionStatus;
  entityType: FormEntityType;
  targetServiceId?: string | null;
  targetServiceCategoryId?: string | null;
  publishedAt?: string | null;
  publishedAtJalali?: string | null;
  createdAt: string;
  createdAtJalali: string;
  fields: FormField[];
}

export interface FormSubmission {
  id: string;
  formTemplateId: string;
  formTitle?: string | null;
  formVersion: number;
  entityType: FormEntityType;
  entityId?: string | null;
  customerId?: string | null;
  customerName?: string | null;
  orderId?: string | null;
  submittedDataJson: string;
  status: FormSubmissionStatus;
  submittedByUserId: string;
  submittedAt: string;
  submittedAtJalali: string;
  createdAt: string;
  createdAtJalali: string;
  reviewedByUserId?: string | null;
  reviewedAt?: string | null;
  reviewedAtJalali?: string | null;
  reviewNote?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
