import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardSummary } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  constructor(private readonly httpClient: HttpClient) {}

  getSummary(): Observable<DashboardSummary> {
    return this.httpClient.get<DashboardSummary>(`${environment.apiBaseUrl}/dashboard/summary`);
  }

  exportComplianceReport(): Observable<Blob> {
    return this.httpClient.get(`${environment.apiBaseUrl}/dashboard/export`, {
      responseType: 'blob'
    });
  }
}

