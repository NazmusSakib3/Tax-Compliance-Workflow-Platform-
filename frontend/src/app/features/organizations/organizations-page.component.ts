import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrganizationListItem } from '../../core/models/reference-data.models';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReferenceDataApiService } from '../../core/services/reference-data-api.service';
import { fieldError } from '../../core/utils/form-field-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-organizations-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="crud-page">
      <form class="editor page-card" [formGroup]="form" (ngSubmit)="save()">
        <div>
          <p class="eyebrow">Master data</p>
          <h2 class="section-title">{{ editingId ? 'Edit organization' : 'Create organization' }}</h2>
          <p class="section-subtitle">Organizations are the top-level owners for legal entities and compliance operations.</p>
        </div>

        <label>
          <span>Name</span>
          <input formControlName="name" placeholder="Northwind Holdings"
            [attr.aria-invalid]="isInvalid('name') ? 'true' : null"
            [attr.aria-describedby]="orgFieldError('name', 'Organization name') ? 'org-name-error' : null">
          <p class="inline-field-error" id="org-name-error" *ngIf="orgFieldError('name', 'Organization name')">{{ orgFieldError('name', 'Organization name') }}</p>
        </label>

        <label>
          <span>Code</span>
          <input formControlName="code" placeholder="NWH"
            [attr.aria-invalid]="isInvalid('code') ? 'true' : null"
            [attr.aria-describedby]="orgFieldError('code', 'Organization code') ? 'org-code-error' : 'org-code-hint'">
          <p class="field-hint" id="org-code-hint">Use a short, unique code that is easy to recognize in dropdown lists.</p>
          <p class="inline-field-error" id="org-code-error" *ngIf="orgFieldError('code', 'Organization code')">{{ orgFieldError('code', 'Organization code') }}</p>
        </label>

        <label>
          <span>Description</span>
          <textarea formControlName="description" rows="3" placeholder="Short description of the organization and its scope"
            [attr.aria-invalid]="isInvalid('description') ? 'true' : null"
            [attr.aria-describedby]="orgFieldError('description', 'Description') ? 'org-description-error' : null"></textarea>
          <p class="inline-field-error" id="org-description-error" *ngIf="orgFieldError('description', 'Description')">{{ orgFieldError('description', 'Description') }}</p>
        </label>

        <label class="checkbox">
          <input type="checkbox" formControlName="isActive">
          <span>Active organization</span>
        </label>

        <div class="actions">
          <button type="submit" class="primary-button" [disabled]="isSaving">{{ isSaving ? 'Saving...' : (editingId ? 'Update organization' : 'Create organization') }}</button>
          <button type="button" class="secondary-button" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="page-card list-card">
        <div class="page-header">
          <div>
            <h2 class="section-title">Organizations</h2>
            <p class="section-subtitle">Manage corporate owners and organizational groups before creating legal entities and task rules.</p>
          </div>
        </div>

        <app-loading-state *ngIf="isLoading" title="Loading organizations" subtitle="Fetching your current organization list."></app-loading-state>

        <div class="list-toolbar" *ngIf="!isLoading">
          <label class="sr-only" for="organizations-search">Search organizations</label>
          <input #searchBox id="organizations-search" type="search" placeholder="Search organizations..." aria-label="Search organizations" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
          <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
        </div>

        <div class="table-shell" *ngIf="!isLoading && organizations.length">
          <table>
            <caption class="sr-only">Organizations</caption>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Code</th>
                <th scope="col">Legal Entities</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let organization of organizations">
                <td>{{ organization.name }}</td>
                <td>{{ organization.code }}</td>
                <td>{{ organization.legalEntityCount }}</td>
                <td>{{ organization.isActive ? 'Active' : 'Inactive' }}</td>
                <td class="row-actions">
                  <button type="button" class="secondary-button" (click)="edit(organization)">Edit</button>
                  <button type="button" class="danger-button" (click)="remove(organization)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-empty-state *ngIf="!isLoading && !organizations.length"
          title="No organizations yet"
          subtitle="Create your first organization to start adding legal entities beneath it."></app-empty-state>

        <app-pagination *ngIf="!isLoading && totalCount > 0"
          [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
          (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
      </section>
    </section>
  `
})
export class OrganizationsPageComponent implements OnInit {
  private readonly referenceDataApiService = inject(ReferenceDataApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    code: ['', [Validators.required, Validators.maxLength(50)]],
    description: ['', [Validators.maxLength(500)]],
    isActive: [true]
  });

  protected organizations: OrganizationListItem[] = [];
  protected editingId: string | null = null;
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

    const request = this.form.getRawValue();
    const isEditing = !!this.editingId;
    const saveRequest = this.editingId
      ? this.referenceDataApiService.updateOrganization(this.editingId, request)
      : this.referenceDataApiService.createOrganization(request);

    saveRequest.subscribe({
      next: () => {
        this.isSaving = false;
        this.notificationService.success(isEditing ? 'Organization updated.' : 'Organization created.');
        this.resetForm();
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => {
        this.isSaving = false;
        this.notificationService.error(error.error?.message ?? 'Unable to save the organization right now.');
      }
    });
  }

  protected edit(organization: OrganizationListItem): void {
    this.referenceDataApiService.getOrganizationById(organization.id).subscribe({
      next: (organizationDetail) => {
        this.editingId = organizationDetail.id;
        this.form.patchValue({
          name: organizationDetail.name,
          code: organizationDetail.code,
          description: organizationDetail.description,
          isActive: organizationDetail.isActive
        });
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load organization details.');
      }
    });
  }

  protected async remove(organization: OrganizationListItem): Promise<void> {
    const confirmed = await this.confirmDialogService.confirm({
      title: 'Delete organization',
      message: `Delete organization "${organization.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger'
    });

    if (!confirmed) {
      return;
    }

    this.referenceDataApiService.deleteOrganization(organization.id).subscribe({
      next: () => {
        this.notificationService.success('Organization deleted.');
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to delete the organization.');
      }
    });
  }

  protected resetForm(): void {
    this.editingId = null;
    this.form.reset({ name: '', code: '', description: '', isActive: true });
  }

  protected orgFieldError(controlName: 'name' | 'code' | 'description', label: string): string {
    return fieldError(this.form.get(controlName), label);
  }

  protected isInvalid(controlName: 'name' | 'code' | 'description'): boolean {
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

    this.referenceDataApiService.getOrganizations({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe({
      next: (result) => {
        this.organizations = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load organizations.');
        this.isLoading = false;
      }
    });
  }
}
