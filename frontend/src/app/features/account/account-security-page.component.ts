import { NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthenticatedUser, MfaSetupResponse } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { fieldError as getFieldError } from '../../core/utils/form-field-error.util';

@Component({
  selector: 'app-account-security-page',
  standalone: true,
  imports: [NgIf, ReactiveFormsModule],
  template: `
    <section class="security-page">
      <section class="page-card security-card">
        <div class="page-header">
          <div>
            <p class="eyebrow">Account security</p>
            <h2 class="section-title">Multi-factor authentication</h2>
            <p class="section-subtitle">
              Add an authenticator app code requirement to protect access to your account.
            </p>
          </div>
        </div>

        <section class="loading-state" *ngIf="isLoading">
          <h3 class="section-title">Loading account security</h3>
        </section>

        <ng-container *ngIf="!isLoading && user">
          <div class="status-panel" [class.enabled]="user.isMfaEnabled">
            <span class="status-dot"></span>
            <div>
              <h3>{{ user.isMfaEnabled ? 'MFA is enabled' : 'MFA is disabled' }}</h3>
              <p class="section-subtitle">
                {{ user.isMfaEnabled
                  ? 'Sign-ins require your password and an authenticator app code.'
                  : 'Set up MFA to require an authenticator app code at sign-in.' }}
              </p>
            </div>
          </div>

          <section class="setup-section" *ngIf="!user.isMfaEnabled">
            <button type="button" class="primary-button" (click)="startSetup()" [disabled]="isSaving">
              {{ isSaving ? 'Preparing...' : 'Set up MFA' }}
            </button>

            <div class="setup-details" *ngIf="setupResponse">
              <div>
                <h3>Scan or enter this setup key</h3>
                <p class="field-hint">Use an authenticator app, then enter the current 6-digit code to enable MFA.</p>
              </div>

              <label>
                <span>Shared key</span>
                <input readonly [value]="setupResponse.sharedKey">
              </label>

              <label>
                <span>Authenticator URI</span>
                <textarea readonly rows="4">{{ setupResponse.authenticatorUri }}</textarea>
              </label>

              <form class="setup-form" [formGroup]="setupForm" (ngSubmit)="enableMfa()">
                <label>
                  <span>Verification code</span>
                  <input inputmode="numeric" autocomplete="one-time-code" formControlName="setupCode" placeholder="123456">
                  <p class="inline-field-error" *ngIf="fieldError(setupForm.controls.setupCode, 'Verification code')">
                    {{ fieldError(setupForm.controls.setupCode, 'Verification code') }}
                  </p>
                </label>

                <button type="submit" class="primary-button" [disabled]="isSaving">
                  {{ isSaving ? 'Enabling...' : 'Enable MFA' }}
                </button>
              </form>
            </div>
          </section>

          <form class="disable-form" *ngIf="user.isMfaEnabled" [formGroup]="disableForm" (ngSubmit)="disableMfa()">
            <div>
              <h3>Disable MFA</h3>
              <p class="section-subtitle">Enter your current authenticator code to remove MFA from this account.</p>
            </div>

            <label>
              <span>Authenticator code</span>
              <input inputmode="numeric" autocomplete="one-time-code" formControlName="disableCode" placeholder="123456">
              <p class="inline-field-error" *ngIf="fieldError(disableForm.controls.disableCode, 'Authenticator code')">
                {{ fieldError(disableForm.controls.disableCode, 'Authenticator code') }}
              </p>
            </label>

            <button type="submit" class="danger-button" [disabled]="isSaving">
              {{ isSaving ? 'Disabling...' : 'Disable MFA' }}
            </button>
          </form>
        </ng-container>

        <p class="status-message" *ngIf="successMessage">{{ successMessage }}</p>
        <p class="status-message error-message" *ngIf="errorMessage">{{ errorMessage }}</p>
      </section>
    </section>
  `,
  styles: [`
    .security-page { display: grid; gap: 20px; }
    .security-card { padding: 24px; display: grid; gap: 20px; }
    .status-panel { display: grid; grid-template-columns: auto 1fr; gap: 14px; align-items: start; padding: 18px; border: 1px solid var(--border); border-radius: 16px; background: var(--surface-muted); }
    .status-panel h3, .setup-details h3, .disable-form h3 { margin: 0 0 8px; }
    .status-dot { width: 14px; height: 14px; margin-top: 4px; border-radius: 50%; background: var(--warning); }
    .status-panel.enabled .status-dot { background: var(--success); }
    .setup-section, .setup-details, .setup-form, .disable-form { display: grid; gap: 16px; }
    .setup-details, .disable-form { padding: 18px; border: 1px solid var(--border); border-radius: 16px; background: color-mix(in srgb, var(--surface-muted) 78%, transparent); }
    label { display: grid; gap: 8px; }
    textarea { resize: vertical; }
    @media (min-width: 900px) { .security-page { max-width: 760px; } }
  `]
})
export class AccountSecurityPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

  protected user: AuthenticatedUser | null = null;
  protected setupResponse: MfaSetupResponse | null = null;
  protected isLoading = true;
  protected isSaving = false;
  protected successMessage = '';
  protected errorMessage = '';
  protected readonly setupForm = this.formBuilder.group({
    setupCode: ['', [Validators.required]]
  });
  protected readonly disableForm = this.formBuilder.group({
    disableCode: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadUser();
  }

  protected startSetup(): void {
    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.authService.setupMfa().subscribe({
      next: (response) => {
        this.setupResponse = response;
        this.isSaving = false;
      },
      error: (error: HttpErrorResponse) => {
        this.isSaving = false;
        this.errorMessage = error.error?.message ?? 'Unable to start MFA setup.';
      }
    });
  }

  protected enableMfa(): void {
    if (this.setupForm.invalid || this.isSaving) {
      this.setupForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.authService.enableMfa({ code: this.setupForm.value.setupCode ?? '' }).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.setupResponse = null;
        this.setupForm.reset();
        this.isSaving = false;
        this.loadUser();
      },
      error: (error: HttpErrorResponse) => {
        this.isSaving = false;
        this.errorMessage = error.error?.message ?? 'Unable to enable MFA.';
      }
    });
  }

  protected disableMfa(): void {
    if (this.disableForm.invalid || this.isSaving) {
      this.disableForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.authService.disableMfa({ code: this.disableForm.value.disableCode ?? '' }).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.disableForm.reset();
        this.isSaving = false;
        this.loadUser();
      },
      error: (error: HttpErrorResponse) => {
        this.isSaving = false;
        this.errorMessage = error.error?.message ?? 'Unable to disable MFA.';
      }
    });
  }

  protected readonly fieldError = getFieldError;

  private loadUser(): void {
    this.isLoading = true;
    this.authService.getCurrentUser().subscribe({
      next: (user) => {
        this.user = user;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.message ?? 'Unable to load account security.';
        this.isLoading = false;
      }
    });
  }
}
