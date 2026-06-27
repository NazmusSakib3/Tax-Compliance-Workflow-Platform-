import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GlobalAuditLogEntry } from '../models/audit-log.models';
import { PagedResult } from '../models/paged-result.models';

@Injectable({ providedIn: 'root' })
export class AuditLogApiService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly httpClient: HttpClient) {}

  getGlobalAuditLog(page = 1, pageSize = 50, actionType?: string): Observable<PagedResult<GlobalAuditLogEntry>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (actionType) {
      params = params.set('actionType', actionType);
    }

    return this.httpClient.get<PagedResult<GlobalAuditLogEntry>>(`${this.apiBaseUrl}/audit-log`, { params });
  }
}
