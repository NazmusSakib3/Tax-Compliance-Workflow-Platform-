import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedListQuery, PagedResult } from '../models/paged-result.models';
import { CreateUserRequest, UpdateUserRequest, UserListItem } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly httpClient: HttpClient) {}

  getUsers(query?: PagedListQuery): Observable<PagedResult<UserListItem>> {
    let params = new HttpParams()
      .set('page', query?.page ?? 1)
      .set('pageSize', query?.pageSize ?? 50);

    if (query?.search) {
      params = params.set('search', query.search);
    }

    return this.httpClient.get<PagedResult<UserListItem>>(`${this.apiBaseUrl}/users`, { params });
  }

  createUser(request: CreateUserRequest): Observable<UserListItem> {
    return this.httpClient.post<UserListItem>(`${this.apiBaseUrl}/users`, request);
  }

  updateUser(userId: string, request: UpdateUserRequest): Observable<UserListItem> {
    return this.httpClient.put<UserListItem>(`${this.apiBaseUrl}/users/${userId}`, request);
  }
}
