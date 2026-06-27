import { NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { fieldError } from '../../core/utils/form-field-error.util';

@Component({
  selector: 'app-forgot-password-page',
  standalone: true,
  imports: [NgIf, ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-page">
      <form class="auth-card page-card" [formGroup]="form" (ngSubmit)="submit()">
        <div>
          <p class="eyebrow">Password recovery</p>
          <h2 class="section-title">Request a reset token</h2>
          <p class="section-subtitle">
            Enter your account email. If the account exists, the API will start the reset flow.
          </p>
        </div>

        <label>
          <span>Email</span>
          <input type="email" formControlName="email" placeholder="user@company.com">
          <p class="inline-field-error" *ngIf="emailFieldError('Email')">{{ emailFieldError('Email') }}</p>
        </label>

        <p class="status-message" *ngIf="successMessage">{{ successMessage }}</p>
        <p class="status-message error-message" *ngIf="errorMessage">{{ errorMessage }}</p>

        <button type="submit" class="primary-button" [disabled]="isSubmitting">
          {{ isSubmitting ? 'Requesting...' : 'Request reset token' }}
        </button>
        <a class="auth-link" routerLink="/reset-password">Already have a reset token?</a>
        <a class="auth-link" routerLink="/login">Back to sign in</a>
      </form>
    </section>
  `,
  styles: [`
    .auth-page { display: grid; place-items: center; padding: 20px; }
    .auth-card { width: min(520px, 100%); padding: 32px; display: grid; gap: 18px; }
    label { display: grid; gap: 8px; }
    .auth-link { color: var(--primary); font-weight: 700; text-decoration: none; }
  `]
})
export class ForgotPasswordPageComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]]
  });
  protected isSubmitting = false;
  protected successMessage = '';
  protected errorMessage = '';

  protected emailFieldError(label: string): string {
    return fieldError(this.form.controls.email, label);
  }

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.authService.forgotPassword({ email: this.form.value.email ?? '' }).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.successMessage = response.message;
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.message ?? 'Unable to request a password reset.';
      }
    });
  }
}
