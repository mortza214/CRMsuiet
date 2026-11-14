import { Button, Card, Space, Tag } from "antd";
import { Link } from "react-router-dom";
import { useInstancesByEntity, useWorkflowInstance } from "@/modules/workflow/hooks/useWorkflow";
import { useFormSubmissions, useFormTemplates } from "@/modules/forms/hooks/useForms";
import { WorkflowSubmissionSummary } from "./WorkflowSubmissionSummary";

export function OrderWorkflowSummary({ orderId }: { orderId?: string }) {
  const instances = useInstancesByEntity("Order", orderId);
  const workflow = useWorkflowInstance(instances.data?.[0]?.id);
  const templates = useFormTemplates(false);
  const submissions = useFormSubmissions({ page: 1, pageSize: 100, entityType: "Order", entityId: orderId }, Boolean(orderId));
  if (!workflow.data) return null;
  const current = workflow.data.tasks.filter(task => task.status === "Pending" || task.status === "InProgress");
  return <Card title="گردش درخواست و نتیجه فرم‌ها" variant="borderless" className="detail-card"
    extra={<Link to={`/workflow/instances/${workflow.data.id}`}><Button>مشاهده گردش و تاریخچه درخواست</Button></Link>}>
    <Space wrap style={{ marginBottom: 16 }}>
      <Tag color="blue">{workflow.data.currentStateName}</Tag>
      {current.map(task => <Tag key={task.id}>کارتابل: {task.assigneeName ?? "در حال دریافت"}</Tag>)}
    </Space>
    <WorkflowSubmissionSummary submissions={submissions.data?.items ?? []} templates={templates.data ?? []} />
  </Card>;
}
