import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedListQuery, PagedResult } from '../models/paged-result.models';
import {
  ComplianceTaskRuleDetail,
  ComplianceTaskRuleListItem,
  ComplianceTemplateDetail,
  ComplianceTemplateListItem,
  JurisdictionDetail,
  JurisdictionListItem,
  LegalEntityDetail,
  LegalEntityListItem,
  OrganizationDetail,
  OrganizationListItem,
  SaveComplianceTaskRuleRequest,
  SaveComplianceTemplateRequest,
  SaveJurisdictionRequest,
  SaveLegalEntityRequest,
  SaveOrganizationRequest
} from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class ReferenceDataApiService {
  private static readonly MAX_PAGE_SIZE = 100;
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly httpClient: HttpClient) {}

  private buildParams(query?: PagedListQuery): HttpParams {
    let params = new HttpParams()
      .set('page', query?.page ?? 1)
      .set('pageSize', query?.pageSize ?? 50);

    if (query?.search) {
      params = params.set('search', query.search);
    }

    return params;
  }

  getOrganizations(query?: PagedListQuery): Observable<PagedResult<OrganizationListItem>> {
    return this.httpClient.get<PagedResult<OrganizationListItem>>(`${this.apiBaseUrl}/organizations`, {
      params: this.buildParams(query)
    });
  }

  getOrganizationById(id: string): Observable<OrganizationDetail> {
    return this.httpClient.get<OrganizationDetail>(`${this.apiBaseUrl}/organizations/${id}`);
  }

  createOrganization(request: SaveOrganizationRequest): Observable<OrganizationDetail> {
    return this.httpClient.post<OrganizationDetail>(`${this.apiBaseUrl}/organizations`, request);
  }

  updateOrganization(id: string, request: SaveOrganizationRequest): Observable<OrganizationDetail> {
    return this.httpClient.put<OrganizationDetail>(`${this.apiBaseUrl}/organizations/${id}`, request);
  }

  deleteOrganization(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiBaseUrl}/organizations/${id}`);
  }

  getLegalEntities(query?: PagedListQuery): Observable<PagedResult<LegalEntityListItem>> {
    return this.httpClient.get<PagedResult<LegalEntityListItem>>(`${this.apiBaseUrl}/legal-entities`, {
      params: this.buildParams(query)
    });
  }

  getLegalEntityById(id: string): Observable<LegalEntityDetail> {
    return this.httpClient.get<LegalEntityDetail>(`${this.apiBaseUrl}/legal-entities/${id}`);
  }

  createLegalEntity(request: SaveLegalEntityRequest): Observable<LegalEntityDetail> {
    return this.httpClient.post<LegalEntityDetail>(`${this.apiBaseUrl}/legal-entities`, request);
  }

  updateLegalEntity(id: string, request: SaveLegalEntityRequest): Observable<LegalEntityDetail> {
    return this.httpClient.put<LegalEntityDetail>(`${this.apiBaseUrl}/legal-entities/${id}`, request);
  }

  deleteLegalEntity(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiBaseUrl}/legal-entities/${id}`);
  }

  getJurisdictions(query?: PagedListQuery): Observable<PagedResult<JurisdictionListItem>> {
    return this.httpClient.get<PagedResult<JurisdictionListItem>>(`${this.apiBaseUrl}/jurisdictions`, {
      params: this.buildParams(query)
    });
  }

  getJurisdictionById(id: string): Observable<JurisdictionDetail> {
    return this.httpClient.get<JurisdictionDetail>(`${this.apiBaseUrl}/jurisdictions/${id}`);
  }

  createJurisdiction(request: SaveJurisdictionRequest): Observable<JurisdictionDetail> {
    return this.httpClient.post<JurisdictionDetail>(`${this.apiBaseUrl}/jurisdictions`, request);
  }

  updateJurisdiction(id: string, request: SaveJurisdictionRequest): Observable<JurisdictionDetail> {
    return this.httpClient.put<JurisdictionDetail>(`${this.apiBaseUrl}/jurisdictions/${id}`, request);
  }

  deleteJurisdiction(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiBaseUrl}/jurisdictions/${id}`);
  }

  getComplianceTemplates(query?: PagedListQuery): Observable<PagedResult<ComplianceTemplateListItem>> {
    return this.httpClient.get<PagedResult<ComplianceTemplateListItem>>(`${this.apiBaseUrl}/compliance-templates`, {
      params: this.buildParams(query)
    });
  }

  getComplianceTemplateById(id: string): Observable<ComplianceTemplateDetail> {
    return this.httpClient.get<ComplianceTemplateDetail>(`${this.apiBaseUrl}/compliance-templates/${id}`);
  }

  createComplianceTemplate(request: SaveComplianceTemplateRequest): Observable<ComplianceTemplateDetail> {
    return this.httpClient.post<ComplianceTemplateDetail>(`${this.apiBaseUrl}/compliance-templates`, request);
  }

  updateComplianceTemplate(id: string, request: SaveComplianceTemplateRequest): Observable<ComplianceTemplateDetail> {
    return this.httpClient.put<ComplianceTemplateDetail>(`${this.apiBaseUrl}/compliance-templates/${id}`, request);
  }

  deleteComplianceTemplate(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiBaseUrl}/compliance-templates/${id}`);
  }

  getComplianceTaskRules(query?: PagedListQuery): Observable<PagedResult<ComplianceTaskRuleListItem>> {
    return this.httpClient.get<PagedResult<ComplianceTaskRuleListItem>>(`${this.apiBaseUrl}/compliance-task-rules`, {
      params: this.buildParams(query)
    });
  }

  getComplianceTaskRuleById(id: string): Observable<ComplianceTaskRuleDetail> {
    return this.httpClient.get<ComplianceTaskRuleDetail>(`${this.apiBaseUrl}/compliance-task-rules/${id}`);
  }

  createComplianceTaskRule(request: SaveComplianceTaskRuleRequest): Observable<ComplianceTaskRuleDetail> {
    return this.httpClient.post<ComplianceTaskRuleDetail>(`${this.apiBaseUrl}/compliance-task-rules`, request);
  }

  updateComplianceTaskRule(id: string, request: SaveComplianceTaskRuleRequest): Observable<ComplianceTaskRuleDetail> {
    return this.httpClient.put<ComplianceTaskRuleDetail>(`${this.apiBaseUrl}/compliance-task-rules/${id}`, request);
  }

  deleteComplianceTaskRule(id: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiBaseUrl}/compliance-task-rules/${id}`);
  }

  listAllOrganizations(): Observable<OrganizationListItem[]> {
    return this.listAllPages((query) => this.getOrganizations(query));
  }

  listAllLegalEntities(): Observable<LegalEntityListItem[]> {
    return this.listAllPages((query) => this.getLegalEntities(query));
  }

  listAllJurisdictions(): Observable<JurisdictionListItem[]> {
    return this.listAllPages((query) => this.getJurisdictions(query));
  }

  listAllComplianceTemplates(): Observable<ComplianceTemplateListItem[]> {
    return this.listAllPages((query) => this.getComplianceTemplates(query));
  }

  listAllComplianceTaskRules(): Observable<ComplianceTaskRuleListItem[]> {
    return this.listAllPages((query) => this.getComplianceTaskRules(query));
  }

  private listAllPages<T>(
    fetchPage: (query: PagedListQuery) => Observable<PagedResult<T>>
  ): Observable<T[]> {
    return fetchPage({ page: 1, pageSize: ReferenceDataApiService.MAX_PAGE_SIZE }).pipe(
      expand((result) =>
        result.page < result.totalPages
          ? fetchPage({ page: result.page + 1, pageSize: ReferenceDataApiService.MAX_PAGE_SIZE })
          : EMPTY
      ),
      map((result) => result.items),
      reduce((accumulated, items) => accumulated.concat(items), [] as T[])
    );
  }
}
