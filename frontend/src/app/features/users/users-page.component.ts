import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { USER_ROLES, UserListItem } from '../../core/models/user.models';
import { NotificationService } from '../../core/services/notification.service';
import { UsersApiService } from '../../core/services/users-api.service';
import { fieldError } from '../../core/utils/form-field-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="crud-page">
      <form class="editor page-card" [formGroup]="form" (ngSubmit)="save()">
        <div>
          <p class="eyebrow">Administration</p>
          <h2 class="section-title">{{ editingUserId ? 'Edit user' : 'Create user' }}</h2>
          <p class="section-subtitle">Invite platform users and assign a single access role.</p>
        </div>

        <label *ngIf="!editingUserId">
          <span>Email</span>
          <input formControlName="email" type="email" placeholder="user@company.com"
            [attr.aria-invalid]="isInvalid('email') ? 'true' : null"
            [attr.aria-describedby]="controlError('email', 'Email') ? 'user-email-error' : null">
          <p class="inline-field-error" id="user-email-error" *ngIf="controlError('email', 'Email')">{{ controlError('email', 'Email') }}</p>
        </label>

        <label *ngIf="!editingUserId">
          <span>Password</span>
          <input formControlName="password" type="password" placeholder="Minimum 8 characters"
            [attr.aria-invalid]="isInvalid('password') ? 'true' : null"
            [attr.aria-describedby]="controlError('password', 'Password') ? 'user-password-error' : null">
          <p class="inline-field-error" id="user-password-error" *ngIf="controlError('password', 'Password')">{{ controlError('password', 'Password') }}</p>
        </label>

        <label>
          <span>Display name</span>
          <input formControlName="displayName" placeholder="Jane Compliance"
            [attr.aria-invalid]="isInvalid('displayName') ? 'true' : null"
            [attr.aria-describedby]="controlError('displayName', 'Display name') ? 'user-displayname-error' : null">
          <p class="inline-field-error" id="user-displayname-error" *ngIf="controlError('displayName', 'Display name')">{{ controlError('displayName', 'Display name') }}</p>
        </label>

        <label>
          <span>Role</span>
          <select formControlName="role">
            <option *ngFor="let role of roles" [value]="role">{{ role }}</option>
          </select>
        </label>

        <label class="checkbox" *ngIf="editingUserId">
          <input type="checkbox" formControlName="isActive">
          <span>Active user</span>
        </label>

        <div class="actions">
          <button type="submit" class="primary-button" [disabled]="isSaving">{{ isSaving ? 'Saving...' : (editingUserId ? 'Update user' : 'Create user') }}</button>
          <button type="button" class="secondary-button" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="page-card list-card">
        <div class="page-header">
          <div>
            <h2 class="section-title">Users</h2>
            <p class="section-subtitle">Manage who can access the compliance platform and what they can do.</p>
          </div>
        </div>

        <app-loading-state *ngIf="isLoading" title="Loading users" subtitle="Fetching platform users and their roles."></app-loading-state>

        <div class="list-toolbar" *ngIf="!isLoading">
          <label class="sr-only" for="users-search">Search users</label>
          <input #searchBox id="users-search" type="search" placeholder="Search users..." aria-label="Search users" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
          <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
        </div>

        <div class="table-shell" *ngIf="!isLoading && users.length">
          <table>
            <caption class="sr-only">Users</caption>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Email</th>
                <th scope="col">Role</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let user of users">
                <td>{{ user.displayName }}</td>
                <td>{{ user.email }}</td>
                <td>{{ user.roles[0] || '—' }}</td>
                <td>{{ user.isActive ? 'Active' : 'Inactive' }}</td>
                <td class="row-actions">
                  <button type="button" class="secondary-button" (click)="edit(user)">Edit</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-empty-state *ngIf="!isLoading && !users.length"
          title="No users yet"
          subtitle="Invite your first user and assign them an access role."></app-empty-state>

        <app-pagination *ngIf="!isLoading && totalCount > 0"
          [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
          (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
      </section>
    </section>
  `
})
export class UsersPageComponent implements OnInit {
  private readonly usersApiService = inject(UsersApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly notificationService = inject(NotificationService);

  protected readonly roles = USER_ROLES;
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    displayName: ['', [Validators.required, Validators.maxLength(150)]],
    role: ['Viewer', Validators.required],
    isActive: [true]
  });

  protected users: UserListItem[] = [];
  protected editingUserId: string | null = null;
  protected isLoading = true;
  protected isSaving = false;
  protected page = 1;
  protected totalPages = 1;
  protected totalCount = 0;
  protected search = '';
  protected pageSize = 25;

  ngOnInit(): void {
    this.loadPage(1);
  }

  protected save(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const value = this.form.getRawValue();
    const isEditing = !!this.editingUserId;

    const request$ = this.editingUserId
      ? this.usersApiService.updateUser(this.editingUserId, {
          displayName: value.displayName,
          role: value.role,
          isActive: value.isActive
        })
      : this.usersApiService.createUser({
          email: value.email,
          displayName: value.displayName,
          password: value.password,
          role: value.role
        });

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.notificationService.success(isEditing ? 'User updated.' : 'User created.');
        this.resetForm();
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => {
        this.isSaving = false;
        this.notificationService.error(error.error?.message ?? 'Unable to save the user.');
      }
    });
  }

  protected edit(user: UserListItem): void {
    this.editingUserId = user.userId;
    this.form.patchValue({
      displayName: user.displayName,
      role: user.roles[0] ?? 'Viewer',
      isActive: user.isActive
    });
    this.form.controls.email.disable();
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
  }

  protected resetForm(): void {
    this.editingUserId = null;
    this.form.enable();
    this.form.reset({
      email: '',
      password: '',
      displayName: '',
      role: 'Viewer',
      isActive: true
    });
    this.form.controls.password.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.controls.password.updateValueAndValidity();
  }

  protected controlError(controlName: string, label: string): string {
    return fieldError(this.form.get(controlName), label);
  }

  protected isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && control.touched;
  }

  protected onSearch(): void {
    this.loadPage(1);
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.loadPage(1);
  }

  protected loadPage(page: number): void {
    this.isLoading = true;

    this.usersApiService.getUsers({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe({
      next: (result) => {
        this.users = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load users.');
        this.isLoading = false;
      }
    });
  }
}
