import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { JurisdictionListItem } from '../../core/models/reference-data.models';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReferenceDataApiService } from '../../core/services/reference-data-api.service';
import { fieldError } from '../../core/utils/form-field-error.util';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-jurisdictions-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="crud-page">
      <form class="editor page-card" [formGroup]="form" (ngSubmit)="save()">
        <div>
          <p class="eyebrow">Jurisdiction catalog</p>
          <h2 class="section-title">{{ editingId ? 'Edit jurisdiction' : 'Create jurisdiction' }}</h2>
          <p class="section-subtitle">Jurisdictions represent tax authorities and filing regions.</p>
        </div>

        <label>
          <span>Name</span>
          <input formControlName="name"
            [attr.aria-invalid]="isInvalid('name') ? 'true' : null"
            [attr.aria-describedby]="controlError('name', 'Name') ? 'jur-name-error' : null">
          <p class="inline-field-error" id="jur-name-error" *ngIf="controlError('name', 'Name')">{{ controlError('name', 'Name') }}</p>
        </label>
        <label>
          <span>Country Code</span>
          <input formControlName="countryCode"
            [attr.aria-invalid]="isInvalid('countryCode') ? 'true' : null"
            [attr.aria-describedby]="controlError('countryCode', 'Country code') ? 'jur-country-error' : null">
          <p class="inline-field-error" id="jur-country-error" *ngIf="controlError('countryCode', 'Country code')">{{ controlError('countryCode', 'Country code') }}</p>
        </label>
        <label>
          <span>Region Code</span>
          <input formControlName="regionCode"
            [attr.aria-invalid]="isInvalid('regionCode') ? 'true' : null"
            [attr.aria-describedby]="controlError('regionCode', 'Region code') ? 'jur-region-error' : null">
          <p class="inline-field-error" id="jur-region-error" *ngIf="controlError('regionCode', 'Region code')">{{ controlError('regionCode', 'Region code') }}</p>
        </label>
        <label>
          <span>Filing Authority</span>
          <input formControlName="filingAuthority"
            [attr.aria-invalid]="isInvalid('filingAuthority') ? 'true' : null"
            [attr.aria-describedby]="controlError('filingAuthority', 'Filing authority') ? 'jur-authority-error' : null">
          <p class="inline-field-error" id="jur-authority-error" *ngIf="controlError('filingAuthority', 'Filing authority')">{{ controlError('filingAuthority', 'Filing authority') }}</p>
        </label>
        <label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Active</span></label>

        <div class="actions">
          <button type="submit" class="primary-button">{{ editingId ? 'Update' : 'Create' }}</button>
          <button type="button" class="secondary-button" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="page-card list-card">
        <div class="list-header">
          <h2 class="section-title">Jurisdictions</h2>
          <p class="section-subtitle">Country and region combinations used by task rules.</p>
        </div>

        <div class="list-toolbar">
          <label class="sr-only" for="jurisdictions-search">Search jurisdictions</label>
          <input #searchBox id="jurisdictions-search" type="search" placeholder="Search jurisdictions..." aria-label="Search jurisdictions" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
          <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
        </div>

        <app-loading-state *ngIf="isLoading" title="Loading jurisdictions" subtitle="Fetching tax authorities and filing regions."></app-loading-state>

        <div class="table-shell" *ngIf="!isLoading && jurisdictions.length">
          <table>
            <caption class="sr-only">Jurisdictions</caption>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Country</th>
                <th scope="col">Region</th>
                <th scope="col">Authority</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let jurisdiction of jurisdictions">
                <td>{{ jurisdiction.name }}</td>
                <td>{{ jurisdiction.countryCode }}</td>
                <td>{{ jurisdiction.regionCode }}</td>
                <td>{{ jurisdiction.filingAuthority }}</td>
                <td>{{ jurisdiction.isActive ? 'Active' : 'Inactive' }}</td>
                <td class="row-actions">
                  <button type="button" class="secondary-button" (click)="edit(jurisdiction)">Edit</button>
                  <button type="button" class="danger-button" (click)="remove(jurisdiction)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-empty-state *ngIf="!isLoading && !jurisdictions.length"
          title="No jurisdictions yet"
          subtitle="Add the countries and regions you file in to use them in task rules."></app-empty-state>

        <app-pagination *ngIf="!isLoading && totalCount > 0"
          [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
          (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
      </section>
    </section>
  `
})
export class JurisdictionsPageComponent implements OnInit {
  private readonly apiService = inject(ReferenceDataApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    countryCode: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(2)]],
    regionCode: ['', [Validators.required, Validators.maxLength(10)]],
    filingAuthority: ['', [Validators.required, Validators.maxLength(150)]],
    isActive: [true]
  });

  protected jurisdictions: JurisdictionListItem[] = [];
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
      ? this.apiService.updateJurisdiction(this.editingId, request)
      : this.apiService.createJurisdiction(request);

    action.subscribe({
      next: () => {
        this.notificationService.success(isEditing ? 'Jurisdiction updated.' : 'Jurisdiction created.');
        this.resetForm();
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to save jurisdiction.')
    });
  }

  protected edit(jurisdiction: JurisdictionListItem): void {
    this.editingId = jurisdiction.id;
    this.form.patchValue(jurisdiction);
  }

  protected async remove(jurisdiction: JurisdictionListItem): Promise<void> {
    const confirmed = await this.confirmDialogService.confirm({
      title: 'Delete jurisdiction',
      message: `Delete jurisdiction "${jurisdiction.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger'
    });

    if (!confirmed) {
      return;
    }

    this.apiService.deleteJurisdiction(jurisdiction.id).subscribe({
      next: () => {
        this.notificationService.success('Jurisdiction deleted.');
        this.loadPage(this.page);
      },
      error: (error: HttpErrorResponse) => this.notificationService.error(error.error?.message ?? 'Unable to delete jurisdiction.')
    });
  }

  protected resetForm(): void {
    this.editingId = null;
    this.form.reset({ name: '', countryCode: '', regionCode: '', filingAuthority: '', isActive: true });
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

    this.apiService.getJurisdictions({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe({
      next: (result) => {
        this.jurisdictions = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.notificationService.error(error.error?.message ?? 'Unable to load jurisdictions.');
        this.isLoading = false;
      }
    });
  }
}
