import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { NgIf } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { defaultAuthenticatedRoute } from '../../core/utils/role.utils';
import { fieldError } from '../../core/utils/form-field-error.util';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, RouterLink],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.css'
})
export class LoginPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loginForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    mfaCode: ['']
  });

  protected isSubmitting = false;
  protected errorMessage = '';
  protected mfaRequired = false;

  protected loginFieldError(controlName: 'email' | 'password' | 'mfaCode', label: string): string {
    return fieldError(this.loginForm.get(controlName), label);
  }

  protected clearMfaChallenge(): void {
    this.mfaRequired = false;
    this.errorMessage = '';
    this.loginForm.controls.mfaCode.reset('');
    this.loginForm.controls.mfaCode.clearValidators();
    this.loginForm.controls.mfaCode.updateValueAndValidity();
  }

  protected submit(): void {
    if (this.loginForm.invalid || this.isSubmitting) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    const value = this.loginForm.getRawValue();

    this.authService.login({
      email: value.email ?? '',
      password: value.password ?? '',
      mfaCode: this.mfaRequired ? value.mfaCode ?? '' : undefined
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        void this.router.navigate([defaultAuthenticatedRoute(this.authService)]);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        if (error.error?.data?.requiresMfa) {
          this.mfaRequired = true;
          this.loginForm.controls.mfaCode.setValidators([Validators.required]);
          this.loginForm.controls.mfaCode.updateValueAndValidity();
          this.errorMessage = '';
          return;
        }

        this.errorMessage = error.error?.message ?? 'Login failed. Please verify your credentials.';
      }
    });
  }
}
