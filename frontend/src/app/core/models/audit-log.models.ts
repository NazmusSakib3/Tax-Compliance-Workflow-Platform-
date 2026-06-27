export interface GlobalAuditLogEntry {
  id: string;
  complianceTaskOccurrenceId: string;
  ruleTitle: string;
  legalEntityName: string;
  actionType: string;
  description: string;
  performedByUserId: string;
  performedByDisplayName: string;
  createdUtc: string;
}
