import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ComplianceTemplateListItem } from '../../core/models/reference-data.models';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReferenceDataApiService } from '../../core/services/reference-data-api.service';
import { fieldError } from '../../core/utils/form-field-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-compliance-templates-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="crud-page">
      <form class="editor page-card" [formGroup]="form" (ngSubmit)="save()">
        <div>
          <p class="eyebrow">Template library</p>
          <h2 class="section-title">{{ editingId ? 'Edit template' : 'Create template' }}</h2>
          <p class="section-subtitle">Templates describe the filing type and reminder timing for rules.</p>
        </div>

        <label>
          <span>Name</span>
          <input formControlName="name"
            [attr.aria-invalid]="isInvalid('name') ? 'true' : null"
            [attr.aria-describedby]="controlError('name', 'Name') ? 'tpl-name-error' : null">
          <p class="inline-field-error" id="tpl-name-error" *ngIf="controlError('name', 'Name')">{{ controlError('name', 'Name') }}</p>
        </label>
        <label>
          <span>Filing Type</span>
          <input formControlName="filingType"
            [attr.aria-invalid]="isInvalid('filingType') ? 'true' : null"
            [attr.aria-describedby]="controlError('filingType', 'Filing type') ? 'tpl-filing-error' : null">
          <p class="inline-field-error" id="tpl-filing-error" *ngIf="controlError('filingType', 'Filing type')">{{ controlError('filingType', 'Filing type') }}</p>
        </label>
        <label>
          <span>Description</span>
          <textarea formControlName="description" rows="3"
            [attr.aria-invalid]="isInvalid('description') ? 'true' : null"
            [attr.aria-describedby]="controlError('description', 'Description') ? 'tpl-description-error' : null"></textarea>
          <p class="inline-field-error" id="tpl-description-error" *ngIf="controlError('description', 'Description')">{{ controlError('description', 'Description') }}</p>
        </label>
        <label>
          <span>Reminder Days Before Due</span>
          <input type="number" formControlName="reminderDaysBeforeDue"
            [attr.aria-invalid]="isInvalid('reminderDaysBeforeDue') ? 'true' : null"
            [attr.aria-describedby]="controlError('reminderDaysBeforeDue', 'Reminder days before due') ? 'tpl-reminder-error' : null">
          <p class="inline-field-error" id="tpl-reminder-error" *ngIf="controlError('reminderDaysBeforeDue', 'Reminder days before due')">{{ controlError('reminderDaysBeforeDue', 'Reminder days before due') }}</p>
        </label>
        <label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Active</span></label>

        <div class="actions">
          <button type="submit" class="primary-button">{{ editingId ? 'Update' : 'Create' }}</button>
          <button type="button" class="secondary-button" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="page-card list-card">
        <div class="list-header">
          <h2 class="section-title">Compliance Templates</h2>
          <p class="section-subtitle">Reusable filing blueprints for compliance rules.</p>
        </div>

        <div class="list-toolbar">
          <label class="sr-only" for="templates-search">Search templates</label>
          <input #searchBox id="templates-search" type="search" placeholder="Search templates..." aria-label="Search templates" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
          <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
        </div>

        <app-loading-state *ngIf="isLoading" title="Loading compliance templates" subtitle="Fetching reusable filing blueprints."></app-loading-state>

        <div class="table-shell" *ngIf="!isLoading && templates.length">
          <table>
            <caption class="sr-only">Compliance templates</caption>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Filing Type</th>
                <th scope="col">Reminder</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let template of templates">
                <td>{{ template.name }}</td>
                <td>{{ template.filingType }}</td>
                <td>{{ template.reminderDaysBeforeDue }} days</td>
                <td>{{ template.isActive ? 'Active' : 'Inactive' }}</td>
                <td class="row-actions">
                  <button type="button" class="secondary-button" (click)="edit(template)">Edit</button>
                  <button type="button" class="danger-button" (click)="remove(template)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-empty-state *ngIf="!isLoading && !templates.length"
          title="No compliance templates yet"
          subtitle="Create a template to define filing types and reminder timing for rules."></app-empty-state>

        <app-pagination *ngIf="!isLoading && totalCount > 0"
          [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
          (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
      </section>
    </section>
  `
})
export class ComplianceTemplatesPageComponent implements OnInit {
  private readonly apiService = inject(ReferenceDataApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    filingType: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
    reminderDaysBeforeDue: [0, [Validators.required, Validators.min(0), Validators.max(365)]],
    isActive: [true]
  });

  protected templates: ComplianceTemplateListItem[] = [];
  protected editingId: string | null = null;
  protected isLoading = true;
  protected page = 1;
  protected totalPages = 1;
  protected totalCount = 0;
  protected search = '';
  protected pageSize = 25;

  ngOnInit(): void {
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
      ? this.apiService.updateComplianceTemplate(this.editingId, request)
      : this.apiService.createComplianceTemplate(request);

    action.subscribe({
      next: () => {
        this.notificationService.success(isEditing ? 'Template updated.' : 'Template created.');
        this.resetForm();
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to save compliance template.')
    });
  }

  protected edit(template: ComplianceTemplateListItem): void {
    this.apiService.getComplianceTemplateById(template.id).subscribe({
      next: (detail) => {
        this.editingId = detail.id;
        this.form.patchValue({
          name: detail.name,
          filingType: detail.filingType,
          description: detail.description,
          reminderDaysBeforeDue: detail.reminderDaysBeforeDue,
          isActive: detail.isActive
        });
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to load template details.')
    });
  }

  protected async remove(template: ComplianceTemplateListItem): Promise<void> {
    const confirmed = await this.confirmDialogService.confirm({
      title: 'Delete template',
      message: `Delete compliance template "${template.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger'
    });

    if (!confirmed) {
      return;
    }

    this.apiService.deleteComplianceTemplate(template.id).subscribe({
      next: () => {
        this.notificationService.success('Template deleted.');
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to delete compliance template.')
    });
  }

  protected resetForm(): void {
    this.editingId = null;
    this.form.reset({ name: '', filingType: '', description: '', reminderDaysBeforeDue: 0, isActive: true });
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

    this.apiService.getComplianceTemplates({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe({
      next: (result) => {
        this.templates = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load compliance templates.');
        this.isLoading = false;
      }
    });
  }
}
