export interface ComplianceTaskOccurrenceListItem {
  id: string;
  complianceTaskRuleId: string;
  ruleTitle: string;
  legalEntityName: string;
  jurisdictionName: string;
  periodStartDate: string;
  periodEndDate: string;
  dueDate: string;
  status: number;
  assignedToUserId: string;
  assignedToDisplayName: string;
}

export interface ComplianceTaskOccurrenceDetail extends ComplianceTaskOccurrenceListItem {
  ruleDescription: string;
  templateName: string;
}

export interface AssignableUser {
  userId: string;
  displayName: string;
  email: string;
}

export interface TaskComment {
  id: string;
  commentText: string;
  createdByUserId: string;
  createdByDisplayName: string;
  createdUtc: string;
}

export interface TaskDocument {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  uploadedByDisplayName: string;
  createdUtc: string;
}

export interface AuditLogEntry {
  id: string;
  actionType: string;
  description: string;
  performedByUserId: string;
  performedByDisplayName: string;
  createdUtc: string;
}

export interface UpdateTaskOccurrenceAssignmentRequest {
  assignedToUserId: string;
}

export interface UpdateTaskOccurrenceStatusRequest {
  status: number;
}

export interface CreateTaskCommentRequest {
  commentText: string;
}

