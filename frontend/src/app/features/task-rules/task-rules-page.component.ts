import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  ComplianceTaskRuleListItem,
  ComplianceTemplateListItem,
  JurisdictionListItem,
  LegalEntityListItem,
  SaveComplianceTaskRuleRequest
} from '../../core/models/reference-data.models';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReferenceDataApiService } from '../../core/services/reference-data-api.service';
import { fieldError } from '../../core/utils/form-field-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-task-rules-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="crud-page">
      <form class="editor page-card" [formGroup]="form" (ngSubmit)="save()">
        <div>
          <p class="eyebrow">Recurring rules</p>
          <h2 class="section-title">{{ editingId ? 'Edit task rule' : 'Create task rule' }}</h2>
          <p class="section-subtitle">Rules connect one legal entity, one jurisdiction, and one template.</p>
        </div>

        <label>
          <span>Legal Entity</span>
          <select formControlName="legalEntityId"
            [attr.aria-invalid]="isInvalid('legalEntityId') ? 'true' : null"
            [attr.aria-describedby]="controlError('legalEntityId', 'Legal entity') ? 'rule-entity-error' : null">
            <option value="">Select legal entity</option>
            <option *ngFor="let legalEntity of legalEntities" [value]="legalEntity.id">{{ legalEntity.name }}</option>
          </select>
          <p class="inline-field-error" id="rule-entity-error" *ngIf="controlError('legalEntityId', 'Legal entity')">{{ controlError('legalEntityId', 'Legal entity') }}</p>
        </label>

        <label>
          <span>Jurisdiction</span>
          <select formControlName="jurisdictionId"
            [attr.aria-invalid]="isInvalid('jurisdictionId') ? 'true' : null"
            [attr.aria-describedby]="controlError('jurisdictionId', 'Jurisdiction') ? 'rule-jurisdiction-error' : null">
            <option value="">Select jurisdiction</option>
            <option *ngFor="let jurisdiction of jurisdictions" [value]="jurisdiction.id">{{ jurisdiction.name }}</option>
          </select>
          <p class="inline-field-error" id="rule-jurisdiction-error" *ngIf="controlError('jurisdictionId', 'Jurisdiction')">{{ controlError('jurisdictionId', 'Jurisdiction') }}</p>
        </label>

        <label>
          <span>Compliance Template</span>
          <select formControlName="complianceTemplateId"
            [attr.aria-invalid]="isInvalid('complianceTemplateId') ? 'true' : null"
            [attr.aria-describedby]="controlError('complianceTemplateId', 'Compliance template') ? 'rule-template-error' : null">
            <option value="">Select template</option>
            <option *ngFor="let template of templates" [value]="template.id">{{ template.name }}</option>
          </select>
          <p class="inline-field-error" id="rule-template-error" *ngIf="controlError('complianceTemplateId', 'Compliance template')">{{ controlError('complianceTemplateId', 'Compliance template') }}</p>
        </label>

        <label>
          <span>Title</span>
          <input formControlName="title"
            [attr.aria-invalid]="isInvalid('title') ? 'true' : null"
            [attr.aria-describedby]="controlError('title', 'Title') ? 'rule-title-error' : null">
          <p class="inline-field-error" id="rule-title-error" *ngIf="controlError('title', 'Title')">{{ controlError('title', 'Title') }}</p>
        </label>
        <label>
          <span>Description</span>
          <textarea formControlName="description" rows="3"
            [attr.aria-invalid]="isInvalid('description') ? 'true' : null"
            [attr.aria-describedby]="controlError('description', 'Description') ? 'rule-description-error' : null"></textarea>
          <p class="inline-field-error" id="rule-description-error" *ngIf="controlError('description', 'Description')">{{ controlError('description', 'Description') }}</p>
        </label>
        <label>
          <span>Recurrence Type</span>
          <select formControlName="recurrenceType">
            <option [value]="1">Monthly</option>
            <option [value]="2">Quarterly</option>
            <option [value]="3">Yearly</option>
          </select>
        </label>
        <label>
          <span>Due Day Of Month</span>
          <input type="number" formControlName="dueDayOfMonth"
            [attr.aria-invalid]="isInvalid('dueDayOfMonth') ? 'true' : null"
            [attr.aria-describedby]="controlError('dueDayOfMonth', 'Due day of month') ? 'rule-dueday-error' : null">
          <p class="inline-field-error" id="rule-dueday-error" *ngIf="controlError('dueDayOfMonth', 'Due day of month')">{{ controlError('dueDayOfMonth', 'Due day of month') }}</p>
        </label>
        <label *ngIf="showDueMonthField"><span>Due Month Of Year</span><input type="number" formControlName="dueMonthOfYear"></label>
        <label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Active</span></label>

        <div class="actions">
          <button type="submit" class="primary-button">{{ editingId ? 'Update' : 'Create' }}</button>
          <button type="button" class="secondary-button" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="page-card list-card">
        <div class="list-header">
          <h2 class="section-title">Task Rules</h2>
          <p class="section-subtitle">Rules drive recurring compliance work for each entity and jurisdiction pairing.</p>
        </div>

        <div class="list-toolbar">
          <label class="sr-only" for="task-rules-search">Search task rules</label>
          <input #searchBox id="task-rules-search" type="search" placeholder="Search task rules..." aria-label="Search task rules" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
          <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
        </div>

        <app-loading-state *ngIf="isLoading" title="Loading task rules" subtitle="Fetching recurring compliance rules."></app-loading-state>

        <div class="table-shell" *ngIf="!isLoading && taskRules.length">
          <table>
            <caption class="sr-only">Task rules</caption>
            <thead>
              <tr>
                <th scope="col">Title</th>
                <th scope="col">Legal Entity</th>
                <th scope="col">Jurisdiction</th>
                <th scope="col">Template</th>
                <th scope="col">Recurrence</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let taskRule of taskRules">
                <td>{{ taskRule.title }}</td>
                <td>{{ taskRule.legalEntityName }}</td>
                <td>{{ taskRule.jurisdictionName }}</td>
                <td>{{ taskRule.templateName }}</td>
                <td>{{ recurrenceLabel(taskRule.recurrenceType) }}</td>
                <td class="row-actions">
                  <button type="button" class="secondary-button" (click)="edit(taskRule)">Edit</button>
                  <button type="button" class="danger-button" (click)="remove(taskRule)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-empty-state *ngIf="!isLoading && !taskRules.length"
          title="No task rules yet"
          subtitle="Connect a legal entity, jurisdiction, and template to generate recurring work."></app-empty-state>

        <app-pagination *ngIf="!isLoading && totalCount > 0"
          [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
          (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
      </section>
    </section>
  `
})
export class TaskRulesPageComponent implements OnInit {
  private readonly apiService = inject(ReferenceDataApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form = this.formBuilder.group({
    legalEntityId: ['', Validators.required],
    jurisdictionId: ['', Validators.required],
    complianceTemplateId: ['', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(500)]],
    recurrenceType: [1, Validators.required],
    dueDayOfMonth: [1, [Validators.required, Validators.min(1), Validators.max(31)]],
    dueMonthOfYear: [null as number | null],
    isActive: [true]
  });

  protected legalEntities: LegalEntityListItem[] = [];
  protected jurisdictions: JurisdictionListItem[] = [];
  protected templates: ComplianceTemplateListItem[] = [];
  protected taskRules: ComplianceTaskRuleListItem[] = [];
  protected editingId: string | null = null;
  protected isLoading = true;
  protected page = 1;
  protected totalPages = 1;
  protected totalCount = 0;
  protected search = '';
  protected pageSize = 25;

  protected get showDueMonthField(): boolean {
    return Number(this.form.controls.recurrenceType.value) === 3;
  }

  ngOnInit(): void {
    this.loadLookups();
    this.loadPage(1);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: SaveComplianceTaskRuleRequest = {
      legalEntityId: this.form.controls.legalEntityId.value ?? '',
      jurisdictionId: this.form.controls.jurisdictionId.value ?? '',
      complianceTemplateId: this.form.controls.complianceTemplateId.value ?? '',
      title: this.form.controls.title.value ?? '',
      description: this.form.controls.description.value ?? '',
      isActive: this.form.controls.isActive.value ?? true,
      recurrenceType: Number(this.form.controls.recurrenceType.value),
      dueDayOfMonth: Number(this.form.controls.dueDayOfMonth.value),
      dueMonthOfYear: this.showDueMonthField && this.form.controls.dueMonthOfYear.value
        ? Number(this.form.controls.dueMonthOfYear.value)
        : null
    };

    const isEditing = !!this.editingId;
    const action = this.editingId
      ? this.apiService.updateComplianceTaskRule(this.editingId, request)
      : this.apiService.createComplianceTaskRule(request);

    action.subscribe({
      next: () => {
        this.notificationService.success(isEditing ? 'Task rule updated.' : 'Task rule created.');
        this.resetForm();
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to save task rule.')
    });
  }

  protected edit(taskRule: ComplianceTaskRuleListItem): void {
    this.apiService.getComplianceTaskRuleById(taskRule.id).subscribe({
      next: (detail) => {
        this.editingId = detail.id;
        this.form.patchValue({
          legalEntityId: detail.legalEntityId,
          jurisdictionId: detail.jurisdictionId,
          complianceTemplateId: detail.complianceTemplateId,
          title: detail.title,
          description: detail.description,
          recurrenceType: detail.recurrenceType,
          dueDayOfMonth: detail.dueDayOfMonth,
          dueMonthOfYear: detail.dueMonthOfYear ?? null,
          isActive: detail.isActive
        });
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to load task rule details.')
    });
  }

  protected async remove(taskRule: ComplianceTaskRuleListItem): Promise<void> {
    const confirmed = await this.confirmDialogService.confirm({
      title: 'Delete task rule',
      message: `Delete task rule "${taskRule.title}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger'
    });

    if (!confirmed) {
      return;
    }

    this.apiService.deleteComplianceTaskRule(taskRule.id).subscribe({
      next: () => {
        this.notificationService.success('Task rule deleted.');
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to delete task rule.')
    });
  }

  protected resetForm(): void {
    this.editingId = null;
    this.form.reset({
      legalEntityId: '',
      jurisdictionId: '',
      complianceTemplateId: '',
      title: '',
      description: '',
      recurrenceType: 1,
      dueDayOfMonth: 1,
      dueMonthOfYear: null,
      isActive: true
    });
  }

  protected recurrenceLabel(recurrenceType: number): string {
    if (recurrenceType === 1) {
      return 'Monthly';
    }

    if (recurrenceType === 2) {
      return 'Quarterly';
    }

    return 'Yearly';
  }

  protected controlError(controlName: string, label: string): string {
    return fieldError(this.form.get(controlName), label);
  }

  protected isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && control.touched;
  }

  private loadLookups(): void {
    this.apiService.listAllLegalEntities().subscribe({
      next: (legalEntities) => this.legalEntities = legalEntities,
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to load legal entities.')
    });

    this.apiService.listAllJurisdictions().subscribe({
      next: (jurisdictions) => this.jurisdictions = jurisdictions,
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to load jurisdictions.')
    });

    this.apiService.listAllComplianceTemplates().subscribe({
      next: (templates) => this.templates = templates,
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to load compliance templates.')
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

    this.apiService.getComplianceTaskRules({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe({
      next: (result) => {
        this.taskRules = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load task rules.');
        this.isLoading = false;
      }
    });
  }
}
