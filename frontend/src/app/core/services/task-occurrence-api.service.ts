import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedListQuery, PagedResult } from '../models/paged-result.models';
import {
  AssignableUser,
  AuditLogEntry,
  ComplianceTaskOccurrenceDetail,
  ComplianceTaskOccurrenceListItem,
  CreateTaskCommentRequest,
  TaskComment,
  TaskDocument,
  UpdateTaskOccurrenceAssignmentRequest,
  UpdateTaskOccurrenceStatusRequest
} from '../models/task-occurrence.models';

export interface TaskOccurrenceListQuery extends PagedListQuery {
  status?: string;
  assignedOnly?: boolean;
}

@Injectable({ providedIn: 'root' })
export class TaskOccurrenceApiService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly httpClient: HttpClient) {}

  getOccurrences(query?: TaskOccurrenceListQuery): Observable<PagedResult<ComplianceTaskOccurrenceListItem>> {
    let params = new HttpParams()
      .set('page', query?.page ?? 1)
      .set('pageSize', query?.pageSize ?? 50);

    if (query?.search) {
      params = params.set('search', query.search);
    }
    if (query?.status) {
      params = params.set('status', query.status);
    }
    if (query?.assignedOnly) {
      params = params.set('assignedOnly', 'true');
    }

    return this.httpClient.get<PagedResult<ComplianceTaskOccurrenceListItem>>(`${this.apiBaseUrl}/compliance-task-occurrences`, { params });
  }

  getOccurrenceById(occurrenceId: string): Observable<ComplianceTaskOccurrenceDetail> {
    return this.httpClient.get<ComplianceTaskOccurrenceDetail>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}`);
  }

  getAssignableUsers(): Observable<AssignableUser[]> {
    return this.httpClient.get<AssignableUser[]>(`${this.apiBaseUrl}/compliance-task-occurrences/assignable-users`);
  }

  assignOccurrence(occurrenceId: string, request: UpdateTaskOccurrenceAssignmentRequest): Observable<ComplianceTaskOccurrenceDetail> {
    return this.httpClient.post<ComplianceTaskOccurrenceDetail>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/assignment`, request);
  }

  changeStatus(occurrenceId: string, request: UpdateTaskOccurrenceStatusRequest): Observable<ComplianceTaskOccurrenceDetail> {
    return this.httpClient.post<ComplianceTaskOccurrenceDetail>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/status`, request);
  }

  getComments(occurrenceId: string): Observable<TaskComment[]> {
    return this.httpClient.get<TaskComment[]>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/comments`);
  }

  addComment(occurrenceId: string, request: CreateTaskCommentRequest): Observable<TaskComment> {
    return this.httpClient.post<TaskComment>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/comments`, request);
  }

  getDocuments(occurrenceId: string): Observable<TaskDocument[]> {
    return this.httpClient.get<TaskDocument[]>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/documents`);
  }

  uploadDocument(occurrenceId: string, file: File): Observable<TaskDocument> {
    const formData = new FormData();
    formData.append('file', file);
    return this.httpClient.post<TaskDocument>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/documents`, formData);
  }

  downloadDocument(documentId: string): Observable<Blob> {
    return this.httpClient.get(`${this.apiBaseUrl}/compliance-task-occurrences/documents/${documentId}/download`, {
      responseType: 'blob'
    });
  }

  getAuditLog(occurrenceId: string): Observable<AuditLogEntry[]> {
    return this.httpClient.get<AuditLogEntry[]>(`${this.apiBaseUrl}/compliance-task-occurrences/${occurrenceId}/audit-log`);
  }
}
