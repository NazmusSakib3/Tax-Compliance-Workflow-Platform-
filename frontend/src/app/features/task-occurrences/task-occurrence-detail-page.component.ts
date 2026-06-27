import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { NotificationService } from '../../core/services/notification.service';
import { TaskOccurrenceApiService } from '../../core/services/task-occurrence-api.service';
import { taskOccurrenceStatusLabel, taskOccurrenceStatusClass } from '../../core/utils/task-occurrence.utils';
import {
  AssignableUser,
  AuditLogEntry,
  ComplianceTaskOccurrenceDetail,
  TaskComment,
  TaskDocument
} from '../../core/models/task-occurrence.models';

@Component({
  selector: 'app-task-occurrence-detail-page',
  standalone: true,
  imports: [NgIf, NgFor, ReactiveFormsModule, DatePipe, RouterLink],
  template: `
    <a routerLink="/task-occurrences" class="back-link">Back to occurrences</a>

    <section class="detail-grid" *ngIf="occurrence">
      <article class="page-card summary-card">
        <p class="eyebrow">{{ isContributorTaskView ? 'Assigned task' : 'Task occurrence' }}</p>
        <h1>{{ occurrence.ruleTitle }}</h1>
        <p class="section-subtitle">{{ occurrence.ruleDescription || 'No rule description provided.' }}</p>
        <p class="contributor-note" *ngIf="isContributorTaskView">You can update status, leave comments, and upload documents for this assignment.</p>

        <dl class="meta-grid">
          <div><dt>Legal Entity</dt><dd>{{ occurrence.legalEntityName }}</dd></div>
          <div><dt>Jurisdiction</dt><dd>{{ occurrence.jurisdictionName }}</dd></div>
          <div><dt>Template</dt><dd>{{ occurrence.templateName }}</dd></div>
          <div><dt>Period</dt><dd>{{ occurrence.periodStartDate | date:'mediumDate' }} to {{ occurrence.periodEndDate | date:'mediumDate' }}</dd></div>
          <div><dt>Due Date</dt><dd>{{ occurrence.dueDate | date:'mediumDate' }}</dd></div>
          <div><dt>Status</dt><dd><span class="status-badge" [class]="statusClass(occurrence.status)">{{ statusLabel(occurrence.status) }}</span></dd></div>
          <div><dt>Assigned To</dt><dd>{{ occurrence.assignedToDisplayName || 'Unassigned' }}</dd></div>
        </dl>
      </article>

      <article class="page-card editor-card" *ngIf="canManageAssignment">
        <h2 class="section-title">Assignment</h2>
        <form [formGroup]="assignmentForm" (ngSubmit)="saveAssignment()">
          <label>
            <span>Assign To</span>
            <select formControlName="assignedToUserId">
              <option value="">Select user</option>
              <option *ngFor="let user of assignableUsers" [value]="user.userId">{{ user.displayName }} ({{ user.email }})</option>
            </select>
          </label>
          <button type="submit" [disabled]="isSavingAssignment">{{ isSavingAssignment ? 'Saving...' : 'Save Assignment' }}</button>
        </form>
      </article>

      <article class="page-card editor-card" *ngIf="canUpdateWorkflow">
        <h2 class="section-title">Status</h2>
        <form #statusFormElement [formGroup]="statusForm" (ngSubmit)="saveStatus()">
          <label>
            <span>New Status</span>
            <select formControlName="status">
              <option [value]="1">Draft</option>
              <option [value]="2">Pending</option>
              <option [value]="3">In Progress</option>
              <option [value]="4">Completed</option>
              <option [value]="5">Overdue</option>
              <option [value]="6">Cancelled</option>
            </select>
          </label>
          <p class="status-preview">
            <span class="status-preview-label">Preview</span>
            <span class="status-badge" [class]="statusClass(previewStatus)">{{ statusLabel(previewStatus) }}</span>
          </p>
          <button type="submit" [disabled]="isSavingStatus">{{ isSavingStatus ? 'Updating...' : 'Update Status' }}</button>
        </form>
      </article>
    </section>

    <p class="error-message" *ngIf="errorMessage">{{ errorMessage }}</p>

    <section class="detail-grid" *ngIf="occurrence">
      <article class="page-card section-card">
        <h2 class="section-title">Comments</h2>
        <form #commentFormElement [formGroup]="commentForm" (ngSubmit)="addComment()" *ngIf="canUpdateWorkflow">
          <textarea formControlName="commentText" rows="4" placeholder="Add context or progress notes"></textarea>
          <button type="submit">Add Comment</button>
        </form>
        <div class="timeline" *ngIf="comments.length; else noComments">
          <div class="timeline-item" *ngFor="let comment of comments">
            <p class="timeline-title">{{ comment.createdByDisplayName }} on {{ comment.createdUtc | date:'medium' }}</p>
            <p>{{ comment.commentText }}</p>
          </div>
        </div>
        <ng-template #noComments><p class="section-subtitle">No comments yet.</p></ng-template>
      </article>

      <article class="page-card section-card">
        <h2 class="section-title">Documents</h2>
        <div class="upload-row" *ngIf="canUpdateWorkflow">
          <input #uploadInput type="file" (change)="onFileSelected($event)">
          <button type="button" (click)="uploadSelectedFile()" [disabled]="!selectedFile">Upload</button>
        </div>
        <div class="timeline" *ngIf="documents.length; else noDocuments">
          <div class="timeline-item" *ngFor="let taskDocument of documents">
            <p class="timeline-title">{{ taskDocument.fileName }} uploaded by {{ taskDocument.uploadedByDisplayName }}</p>
            <p>{{ taskDocument.createdUtc | date:'medium' }} | {{ taskDocument.fileSizeBytes }} bytes</p>
            <button type="button" class="link-button" (click)="downloadDocument(taskDocument)">Download</button>
          </div>
        </div>
        <ng-template #noDocuments><p class="section-subtitle">No documents uploaded yet.</p></ng-template>
      </article>
    </section>

    <section class="page-card section-card" *ngIf="occurrence">
      <h2 class="section-title">Audit Log</h2>
      <div class="timeline" *ngIf="auditLogEntries.length; else noAuditEntries">
        <div class="timeline-item" *ngFor="let entry of auditLogEntries">
          <p class="timeline-title">{{ entry.actionType }} by {{ entry.performedByDisplayName }}</p>
          <p>{{ entry.description }}</p>
          <p class="timeline-meta">{{ entry.createdUtc | date:'medium' }}</p>
        </div>
      </div>
      <ng-template #noAuditEntries><p class="section-subtitle">No audit entries recorded yet.</p></ng-template>
    </section>

    <section class="mobile-action-bar page-card" *ngIf="occurrence && canUpdateWorkflow">
      <button type="button" (click)="focusStatusForm()">Status</button>
      <button type="button" (click)="focusCommentForm()">Comment</button>
      <button type="button" (click)="focusUploadInput()">Upload</button>
    </section>
  `,
  styles: [`
    .back-link { display: inline-block; margin-bottom: 16px; color: var(--primary); text-decoration: none; font-weight: 600; }
    .detail-grid { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 24px; margin-bottom: 24px; }
    .summary-card, .editor-card, .section-card { padding: 24px; }
    .contributor-note { margin: 16px 0 0; padding: 12px 14px; border: 1px solid var(--border); border-radius: 14px; background: var(--surface-muted); color: var(--text-muted); }
    .meta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-top: 18px; }
    .status-preview { display: flex; align-items: center; gap: 10px; margin: 0; }
    .status-preview-label { color: var(--text-muted); font-size: 0.8125rem; text-transform: uppercase; letter-spacing: 0.04em; font-weight: 600; }
    dt { color: var(--text-muted); font-size: 0.9rem; }
    dd { margin: 4px 0 0; font-weight: 600; }
    form { display: grid; gap: 12px; }
    .upload-row { display: flex; gap: 12px; align-items: center; margin-bottom: 16px; }
    .timeline { display: grid; gap: 14px; }
    .timeline-item { padding: 14px; border: 1px solid var(--border); border-radius: 14px; background: var(--surface-muted); }
    .timeline-title { margin: 0 0 6px; font-weight: 700; }
    .timeline-meta { margin: 6px 0 0; color: var(--text-muted); font-size: 0.9rem; }
    .link-button { padding: 0; border: 0; background: transparent; color: var(--primary); }
    .error-message { margin: 0 0 18px; color: var(--danger); }
    .mobile-action-bar {
      display: none;
      position: sticky;
      bottom: 12px;
      padding: 12px;
      gap: 10px;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      z-index: 2;
    }
    .mobile-action-bar button {
      width: 100%;
    }
    @media (max-width: 1100px) { .detail-grid { grid-template-columns: 1fr; } .meta-grid { grid-template-columns: 1fr; } }
    @media (max-width: 720px) {
      .mobile-action-bar { display: grid; }
      button[type="submit"], .upload-row button { min-height: 44px; }
      textarea, select, .upload-row input { min-height: 44px; }
    }
  `]
})
export class TaskOccurrenceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly apiService = inject(TaskOccurrenceApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly notificationService = inject(NotificationService);

  protected occurrence: ComplianceTaskOccurrenceDetail | null = null;
  protected assignableUsers: AssignableUser[] = [];
  protected comments: TaskComment[] = [];
  protected documents: TaskDocument[] = [];
  protected auditLogEntries: AuditLogEntry[] = [];
  protected selectedFile: File | null = null;
  protected errorMessage = '';
  protected isSavingAssignment = false;
  protected isSavingStatus = false;

  protected readonly assignmentForm = this.formBuilder.nonNullable.group({
    assignedToUserId: ['', Validators.required]
  });

  protected readonly statusForm = this.formBuilder.nonNullable.group({
    status: [2, Validators.required]
  });

  protected readonly commentForm = this.formBuilder.nonNullable.group({
    commentText: ['', [Validators.required, Validators.maxLength(2000)]]
  });

  @ViewChild('statusFormElement') private statusFormElement?: ElementRef<HTMLElement>;
  @ViewChild('commentFormElement') private commentFormElement?: ElementRef<HTMLElement>;
  @ViewChild('uploadInput') private uploadInput?: ElementRef<HTMLInputElement>;

  protected get canManageAssignment(): boolean {
    return this.authService.hasRole('Admin') || this.authService.hasRole('ComplianceManager');
  }

  protected get canUpdateWorkflow(): boolean {
    if (this.authService.hasRole('Admin') || this.authService.hasRole('ComplianceManager')) {
      return true;
    }

    const userId = this.authService.getUserId();
    return !!userId &&
      this.authService.hasRole('Contributor') &&
      this.occurrence?.assignedToUserId === userId;
  }

  protected get isContributorTaskView(): boolean {
    return this.authService.hasRole('Contributor') &&
      !this.authService.hasRole('Admin') &&
      !this.authService.hasRole('ComplianceManager');
  }

  protected get previewStatus(): number {
    return Number(this.statusForm.controls.status.value);
  }

  ngOnInit(): void {
    const occurrenceId = this.route.snapshot.paramMap.get('id');
    if (!occurrenceId) {
      this.errorMessage = 'Task occurrence id is missing.';
      return;
    }

    this.loadOccurrence(occurrenceId);
    this.loadComments(occurrenceId);
    this.loadDocuments(occurrenceId);
    this.loadAuditLog(occurrenceId);

    if (this.canManageAssignment) {
      this.apiService.getAssignableUsers().subscribe({
        next: (users) => this.assignableUsers = users,
        error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to load assignable users.'
      });
    }
  }

  protected saveAssignment(): void {
    if (!this.occurrence || this.assignmentForm.invalid || this.isSavingAssignment) {
      return;
    }

    this.isSavingAssignment = true;
    this.apiService.assignOccurrence(this.occurrence.id, this.assignmentForm.getRawValue()).subscribe({
      next: (occurrence) => {
        this.occurrence = occurrence;
        this.isSavingAssignment = false;
        this.notificationService.success(
          occurrence.assignedToDisplayName
            ? `Task assigned to ${occurrence.assignedToDisplayName}.`
            : 'Assignment updated.'
        );
        this.loadAuditLog(occurrence.id);
      },
      error: (error: HttpErrorResponse) => {
        this.isSavingAssignment = false;
        this.errorMessage = error.error?.message ?? 'Unable to assign task occurrence.';
        this.notificationService.error(this.errorMessage);
      }
    });
  }

  protected async saveStatus(): Promise<void> {
    if (!this.occurrence || this.statusForm.invalid || this.isSavingStatus) {
      return;
    }

    const targetStatus = Number(this.statusForm.controls.status.value);

    if (targetStatus === 4 || targetStatus === 6) {
      const targetLabel = this.statusLabel(targetStatus);
      const confirmed = await this.confirmDialogService.confirm({
        title: `Mark as ${targetLabel}?`,
        message: `This will set the task status to "${targetLabel}". You can change it again later if needed.`,
        confirmLabel: `Mark ${targetLabel}`,
        tone: 'primary'
      });

      if (!confirmed) {
        return;
      }
    }

    this.isSavingStatus = true;
    this.apiService.changeStatus(this.occurrence.id, { status: targetStatus }).subscribe({
      next: (occurrence) => {
        this.occurrence = occurrence;
        this.isSavingStatus = false;
        this.notificationService.success(`Status updated to ${this.statusLabel(occurrence.status)}.`);
        this.loadAuditLog(occurrence.id);
      },
      error: (error: HttpErrorResponse) => {
        this.isSavingStatus = false;
        this.errorMessage = error.error?.message ?? 'Unable to update task status.';
        this.notificationService.error(this.errorMessage);
      }
    });
  }

  protected addComment(): void {
    if (!this.occurrence || this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.apiService.addComment(this.occurrence.id, this.commentForm.getRawValue()).subscribe({
      next: () => {
        this.commentForm.reset({ commentText: '' });
        this.loadComments(this.occurrence!.id);
      },
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to add comment.'
    });
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.item(0) ?? null;
  }

  protected uploadSelectedFile(): void {
    if (!this.occurrence || !this.selectedFile) {
      return;
    }

    this.apiService.uploadDocument(this.occurrence.id, this.selectedFile).subscribe({
      next: () => {
        this.selectedFile = null;
        this.loadDocuments(this.occurrence!.id);
        this.loadAuditLog(this.occurrence!.id);
      },
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to upload document.'
    });
  }

  protected downloadDocument(taskDocument: TaskDocument): void {
    this.apiService.downloadDocument(taskDocument.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = window.document.createElement('a');
        anchor.href = url;
        anchor.download = taskDocument.fileName;
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to download document.'
    });
  }

  protected statusLabel(status: number): string {
    return taskOccurrenceStatusLabel(status);
  }

  protected statusClass(status: number): string {
    return taskOccurrenceStatusClass(status);
  }

  protected focusStatusForm(): void {
    this.statusFormElement?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  protected focusCommentForm(): void {
    this.commentFormElement?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  protected focusUploadInput(): void {
    this.uploadInput?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
    this.uploadInput?.nativeElement.click();
  }

  private loadOccurrence(occurrenceId: string): void {
    this.apiService.getOccurrenceById(occurrenceId).subscribe({
      next: (occurrence) => {
        this.occurrence = occurrence;
        this.assignmentForm.patchValue({ assignedToUserId: occurrence.assignedToUserId });
        this.statusForm.patchValue({ status: occurrence.status });
      },
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to load task occurrence.'
    });
  }

  private loadComments(occurrenceId: string): void {
    this.apiService.getComments(occurrenceId).subscribe({
      next: (comments) => this.comments = comments,
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to load comments.'
    });
  }

  private loadDocuments(occurrenceId: string): void {
    this.apiService.getDocuments(occurrenceId).subscribe({
      next: (documents) => this.documents = documents,
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to load documents.'
    });
  }

  private loadAuditLog(occurrenceId: string): void {
    this.apiService.getAuditLog(occurrenceId).subscribe({
      next: (entries) => this.auditLogEntries = entries,
      error: (error: HttpErrorResponse) => this.errorMessage = error.error?.message ?? 'Unable to load audit log.'
    });
  }
}
