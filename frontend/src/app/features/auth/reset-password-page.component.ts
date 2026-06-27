import { NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { fieldError } from '../../core/utils/form-field-error.util';

@Component({
  selector: 'app-reset-password-page',
  standalone: true,
  imports: [NgIf, ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-page">
      <form class="auth-card page-card" [formGroup]="form" (ngSubmit)="submit()">
        <div>
          <p class="eyebrow">Password recovery</p>
          <h2 class="section-title">Reset your password</h2>
          <p class="section-subtitle">
            Paste the reset token from your recovery message, then choose a new password.
          </p>
        </div>

        <label>
          <span>Email</span>
          <input type="email" formControlName="email" placeholder="user@company.com">
          <p class="inline-field-error" *ngIf="resetFieldError('email', 'Email')">{{ resetFieldError('email', 'Email') }}</p>
        </label>

        <label>
          <span>Reset token</span>
          <textarea formControlName="token" rows="4" placeholder="Paste the reset token"></textarea>
          <p class="inline-field-error" *ngIf="resetFieldError('token', 'Reset token')">{{ resetFieldError('token', 'Reset token') }}</p>
        </label>

        <label>
          <span>New password</span>
          <input type="password" formControlName="newPassword" placeholder="Minimum 8 characters">
          <p class="inline-field-error" *ngIf="resetFieldError('newPassword', 'New password')">{{ resetFieldError('newPassword', 'New password') }}</p>
        </label>

        <label>
          <span>Confirm new password</span>
          <input type="password" formControlName="confirmPassword" placeholder="Repeat the new password">
          <p class="inline-field-error" *ngIf="resetFieldError('confirmPassword', 'Password confirmation')">{{ resetFieldError('confirmPassword', 'Password confirmation') }}</p>
        </label>

        <p class="status-message" *ngIf="successMessage">{{ successMessage }}</p>
        <p class="status-message error-message" *ngIf="errorMessage">{{ errorMessage }}</p>

        <button type="submit" class="primary-button" [disabled]="isSubmitting">
          {{ isSubmitting ? 'Resetting...' : 'Reset password' }}
        </button>
        <a class="auth-link" routerLink="/login">Back to sign in</a>
      </form>
    </section>
  `,
  styles: [`
    .auth-page { display: grid; place-items: center; padding: 20px; }
    .auth-card { width: min(580px, 100%); padding: 32px; display: grid; gap: 18px; }
    label { display: grid; gap: 8px; }
    textarea { resize: vertical; }
    .auth-link { color: var(--primary); font-weight: 700; text-decoration: none; }
  `]
})
export class ResetPasswordPageComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  protected readonly form = this.formBuilder.group({
    email: [this.route.snapshot.queryParamMap.get('email') ?? '', [Validators.required, Validators.email]],
    token: [this.route.snapshot.queryParamMap.get('token') ?? '', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: [this.matchingPasswords] });
  protected isSubmitting = false;
  protected successMessage = '';
  protected errorMessage = '';

  protected resetFieldError(
    controlName: 'email' | 'token' | 'newPassword' | 'confirmPassword',
    label: string
  ): string {
    const formErrors = controlName === 'confirmPassword' ? this.form.errors : null;
    return fieldError(this.form.controls[controlName], label, formErrors);
  }

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';
    const value = this.form.getRawValue();

    this.authService.resetPassword({
      email: value.email ?? '',
      token: value.token ?? '',
      newPassword: value.newPassword ?? ''
    }).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.successMessage = response.message;
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.message ?? 'Unable to reset the password.';
      }
    });
  }

  private matchingPasswords(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return newPassword && confirmPassword && newPassword !== confirmPassword
      ? { passwordMismatch: true }
      : null;
  }
}
