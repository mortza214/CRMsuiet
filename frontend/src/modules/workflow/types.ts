export type WorkflowEntityType =
  | "Order"
  | "Ticket"
  | "Customer"
  | "ManualTask";

export type WorkflowDefinitionStatus = "Draft" | "Published" | "Archived";
export type WorkflowStateType = "Start" | "Normal" | "End";
export type WorkflowAssigneeType =
  | "User"
  | "Role"
  | "Department"
  | "CustomerOwner"
  | "None";
export type WorkflowGuardType = "None" | "PermissionOnly" | "Plugin";
export type WorkflowInstanceStatus = "Running" | "Completed" | "Cancelled";
export type WorkflowTaskStatus = "Pending" | "InProgress" | "Done" | "Cancelled";

export interface WorkflowState {
  id: string;
  code: string;
  name: string;
  stateType: WorkflowStateType;
  order: number;
  isFinal: boolean;
  assigneeType: WorkflowAssigneeType;
  assigneeValue?: string | null;
  formCode?: string | null;
  actionKey?: string | null;
  color: string;
  x?: number;
  y?: number;
}

export interface WorkflowTransition {
  id: string;
  fromStateId: string;
  toStateId: string;
  name: string;
  guardType: WorkflowGuardType;
  requiredPermission?: string | null;
  guardPluginKey?: string | null;
  requiresComment: boolean;
  sortOrder: number;
}

export interface WorkflowDefinitionSummary {
  id: string;
  code: string;
  name: string;
  entityType: WorkflowEntityType;
  version: number;
  status: WorkflowDefinitionStatus;
  description?: string | null;
  stateCount: number;
  transitionCount: number;
  publishedAt?: string | null;
  publishedAtJalali?: string | null;
  createdAt: string;
  createdAtJalali: string;
}

export interface WorkflowDefinition {
  id: string;
  code: string;
  name: string;
  entityType: WorkflowEntityType;
  version: number;
  status: WorkflowDefinitionStatus;
  description?: string | null;
  publishedAt?: string | null;
  publishedAtJalali?: string | null;
  createdAt: string;
  createdAtJalali: string;
  states: WorkflowState[];
  transitions: WorkflowTransition[];
}

export interface WorkflowTask {
  id: string;
  instanceId: string;
  stateId: string;
  stateName: string;
  status: WorkflowTaskStatus;
  assignedUserId?: string | null;
  assignedRoleId?: string | null;
  assignedDepartmentId?: string | null;
  dueDate?: string | null;
  dueDateJalali?: string | null;
  createdAt: string;
  createdAtJalali: string;
  definitionCode: string;
  definitionName: string;
  entityType: WorkflowEntityType;
  entityId: string;
  currentStateName: string;
  availableTransitions: WorkflowTransition[];
  entityDisplayName?: string | null;
  entityReference?: string | null;
  actionKey?: string | null;
  formCode?: string | null;
}

export interface WorkflowHistory {
  id: string;
  fromStateId?: string | null;
  toStateId: string;
  fromStateName?: string | null;
  toStateName: string;
  transitionId?: string | null;
  transitionName?: string | null;
  actionBy?: string | null;
  actionAt: string;
  actionAtJalali: string;
  comment?: string | null;
  payloadJson?: string | null;
}

export interface WorkflowInstance {
  id: string;
  definitionId: string;
  definitionCode: string;
  definitionName: string;
  definitionVersion: number;
  entityType: WorkflowEntityType;
  entityId: string;
  currentStateId: string;
  currentStateCode: string;
  currentStateName: string;
  status: WorkflowInstanceStatus;
  startedBy?: string | null;
  startedAt: string;
  startedAtJalali: string;
  completedAt?: string | null;
  completedAtJalali?: string | null;
  correlationId?: string | null;
  tasks: WorkflowTask[];
  history: WorkflowHistory[];
  availableTransitions: WorkflowTransition[];
}

export interface WorkflowListParams {
  page: number;
  pageSize: number;
  status?: WorkflowDefinitionStatus;
  entityType?: WorkflowEntityType;
  search?: string;
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
