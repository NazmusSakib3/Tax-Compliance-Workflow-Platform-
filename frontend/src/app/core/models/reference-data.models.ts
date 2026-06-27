export interface OrganizationListItem {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  legalEntityCount: number;
}

export interface OrganizationDetail {
  id: string;
  name: string;
  code: string;
  description: string;
  isActive: boolean;
}

export interface SaveOrganizationRequest {
  name: string;
  code: string;
  description: string;
  isActive: boolean;
}

export interface LegalEntityListItem {
  id: string;
  organizationId: string;
  organizationName: string;
  name: string;
  registrationNumber: string;
  taxIdentifier: string;
  isActive: boolean;
}

export interface LegalEntityDetail extends LegalEntityListItem {
}

export interface SaveLegalEntityRequest {
  organizationId: string;
  name: string;
  registrationNumber: string;
  taxIdentifier: string;
  isActive: boolean;
}

export interface JurisdictionListItem {
  id: string;
  name: string;
  countryCode: string;
  regionCode: string;
  filingAuthority: string;
  isActive: boolean;
}

export interface JurisdictionDetail extends JurisdictionListItem {
}

export interface SaveJurisdictionRequest {
  name: string;
  countryCode: string;
  regionCode: string;
  filingAuthority: string;
  isActive: boolean;
}

export interface ComplianceTemplateListItem {
  id: string;
  name: string;
  filingType: string;
  reminderDaysBeforeDue: number;
  isActive: boolean;
}

export interface ComplianceTemplateDetail extends ComplianceTemplateListItem {
  description: string;
}

export interface SaveComplianceTemplateRequest {
  name: string;
  filingType: string;
  description: string;
  reminderDaysBeforeDue: number;
  isActive: boolean;
}

export interface ComplianceTaskRuleListItem {
  id: string;
  title: string;
  legalEntityName: string;
  jurisdictionName: string;
  templateName: string;
  recurrenceType: number;
  dueDayOfMonth: number;
  dueMonthOfYear?: number | null;
  isActive: boolean;
}

export interface ComplianceTaskRuleDetail {
  id: string;
  legalEntityId: string;
  jurisdictionId: string;
  complianceTemplateId: string;
  title: string;
  description: string;
  legalEntityName: string;
  jurisdictionName: string;
  templateName: string;
  recurrenceType: number;
  dueDayOfMonth: number;
  dueMonthOfYear?: number | null;
  isActive: boolean;
}

export interface SaveComplianceTaskRuleRequest {
  legalEntityId: string;
  jurisdictionId: string;
  complianceTemplateId: string;
  title: string;
  description: string;
  recurrenceType: number;
  dueDayOfMonth: number;
  dueMonthOfYear?: number | null;
  isActive: boolean;
}

