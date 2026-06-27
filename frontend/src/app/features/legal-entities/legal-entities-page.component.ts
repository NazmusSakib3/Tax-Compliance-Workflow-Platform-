import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LegalEntityListItem, OrganizationListItem } from '../../core/models/reference-data.models';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReferenceDataApiService } from '../../core/services/reference-data-api.service';
import { fieldError } from '../../core/utils/form-field-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-legal-entities-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="crud-page">
      <form class="editor page-card" [formGroup]="form" (ngSubmit)="save()">
        <div>
          <p class="eyebrow">Legal entity registry</p>
          <h2 class="section-title">{{ editingId ? 'Edit legal entity' : 'Create legal entity' }}</h2>
          <p class="section-subtitle">Each legal entity belongs to one organization.</p>
        </div>

        <label>
          <span>Organization</span>
          <select formControlName="organizationId"
            [attr.aria-invalid]="isInvalid('organizationId') ? 'true' : null"
            [attr.aria-describedby]="controlError('organizationId', 'Organization') ? 'le-organization-error' : null">
            <option value="">Select organization</option>
            <option *ngFor="let organization of organizations" [value]="organization.id">{{ organization.name }}</option>
          </select>
          <p class="inline-field-error" id="le-organization-error" *ngIf="controlError('organizationId', 'Organization')">{{ controlError('organizationId', 'Organization') }}</p>
        </label>
        <label>
          <span>Name</span>
          <input formControlName="name"
            [attr.aria-invalid]="isInvalid('name') ? 'true' : null"
            [attr.aria-describedby]="controlError('name', 'Name') ? 'le-name-error' : null">
          <p class="inline-field-error" id="le-name-error" *ngIf="controlError('name', 'Name')">{{ controlError('name', 'Name') }}</p>
        </label>
        <label>
          <span>Registration Number</span>
          <input formControlName="registrationNumber"
            [attr.aria-invalid]="isInvalid('registrationNumber') ? 'true' : null"
            [attr.aria-describedby]="controlError('registrationNumber', 'Registration number') ? 'le-registration-error' : null">
          <p class="inline-field-error" id="le-registration-error" *ngIf="controlError('registrationNumber', 'Registration number')">{{ controlError('registrationNumber', 'Registration number') }}</p>
        </label>
        <label>
          <span>Tax Identifier</span>
          <input formControlName="taxIdentifier"
            [attr.aria-invalid]="isInvalid('taxIdentifier') ? 'true' : null"
            [attr.aria-describedby]="controlError('taxIdentifier', 'Tax identifier') ? 'le-tax-error' : null">
          <p class="inline-field-error" id="le-tax-error" *ngIf="controlError('taxIdentifier', 'Tax identifier')">{{ controlError('taxIdentifier', 'Tax identifier') }}</p>
        </label>
        <label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Active</span></label>

        <div class="actions">
          <button type="submit" class="primary-button">{{ editingId ? 'Update' : 'Create' }}</button>
          <button type="button" class="secondary-button" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="page-card list-card">
        <div class="list-header">
          <h2 class="section-title">Legal Entities</h2>
          <p class="section-subtitle">Manage registered entities under each organization.</p>
        </div>

        <div class="list-toolbar">
          <label class="sr-only" for="legal-entities-search">Search legal entities</label>
          <input #searchBox id="legal-entities-search" type="search" placeholder="Search legal entities..." aria-label="Search legal entities" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
          <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
        </div>

        <app-loading-state *ngIf="isLoading" title="Loading legal entities" subtitle="Fetching registered entities for this workspace."></app-loading-state>

        <div class="table-shell" *ngIf="!isLoading && legalEntities.length">
          <table>
            <caption class="sr-only">Legal entities</caption>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Organization</th>
                <th scope="col">Registration</th>
                <th scope="col">Tax ID</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let legalEntity of legalEntities">
                <td>{{ legalEntity.name }}</td>
                <td>{{ legalEntity.organizationName }}</td>
                <td>{{ legalEntity.registrationNumber }}</td>
                <td>{{ legalEntity.taxIdentifier }}</td>
                <td>{{ legalEntity.isActive ? 'Active' : 'Inactive' }}</td>
                <td class="row-actions">
                  <button type="button" class="secondary-button" (click)="edit(legalEntity)">Edit</button>
                  <button type="button" class="danger-button" (click)="remove(legalEntity)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-empty-state *ngIf="!isLoading && !legalEntities.length"
          title="No legal entities yet"
          subtitle="Create your first legal entity under an organization to get started."></app-empty-state>

        <app-pagination *ngIf="!isLoading && totalCount > 0"
          [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
          (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
      </section>
    </section>
  `
})
export class LegalEntitiesPageComponent implements OnInit {
  private readonly apiService = inject(ReferenceDataApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form = this.formBuilder.nonNullable.group({
    organizationId: ['', Validators.required],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    registrationNumber: ['', [Validators.required, Validators.maxLength(100)]],
    taxIdentifier: ['', [Validators.required, Validators.maxLength(100)]],
    isActive: [true]
  });

  protected organizations: OrganizationListItem[] = [];
  protected legalEntities: LegalEntityListItem[] = [];
  protected editingId: string | null = null;
  protected isLoading = true;
  protected page = 1;
  protected totalPages = 1;
  protected totalCount = 0;
  protected search = '';
  protected pageSize = 25;

  ngOnInit(): void {
    this.loadOrganizations();
    this.loadPage(1);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();
    const isEditing = !!this.editingId;
    const action = this.editingId
      ? this.apiService.updateLegalEntity(this.editingId, request)
      : this.apiService.createLegalEntity(request);

    action.subscribe({
      next: () => {
        this.notificationService.success(isEditing ? 'Legal entity updated.' : 'Legal entity created.');
        this.resetForm();
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to save legal entity.')
    });
  }

  protected edit(legalEntity: LegalEntityListItem): void {
    this.editingId = legalEntity.id;
    this.form.patchValue({
      organizationId: legalEntity.organizationId,
      name: legalEntity.name,
      registrationNumber: legalEntity.registrationNumber,
      taxIdentifier: legalEntity.taxIdentifier,
      isActive: legalEntity.isActive
    });
  }

  protected async remove(legalEntity: LegalEntityListItem): Promise<void> {
    const confirmed = await this.confirmDialogService.confirm({
      title: 'Delete legal entity',
      message: `Delete legal entity "${legalEntity.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger'
    });

    if (!confirmed) {
      return;
    }

    this.apiService.deleteLegalEntity(legalEntity.id).subscribe({
      next: () => {
        this.notificationService.success('Legal entity deleted.');
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to delete legal entity.')
    });
  }

  protected resetForm(): void {
    this.editingId = null;
    this.form.reset({ organizationId: '', name: '', registrationNumber: '', taxIdentifier: '', isActive: true });
  }

  protected controlError(controlName: string, label: string): string {
    return fieldError(this.form.get(controlName), label);
  }

  protected isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && control.touched;
  }

  private loadOrganizations(): void {
    this.apiService.listAllOrganizations().subscribe({
      next: (organizations) => this.organizations = organizations,
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to load organizations.')
    });
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

    this.apiService.getLegalEntities({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe({
      next: (result) => {
        this.legalEntities = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load legal entities.');
        this.isLoading = false;
      }
    });
  }
}
