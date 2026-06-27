import { AbstractControl, ValidationErrors } from '@angular/forms';

export function fieldError(
  control: AbstractControl | null | undefined,
  label: string,
  formErrors?: ValidationErrors | null
): string {
  if (!control?.touched) {
    return '';
  }

  if (control.errors?.['required']) {
    return `${label} is required.`;
  }

  if (control.errors?.['email']) {
    return 'Enter a valid email address.';
  }

  if (control.errors?.['minlength']) {
    return `${label} must be at least ${control.errors['minlength'].requiredLength} characters.`;
  }

  if (control.errors?.['maxlength']) {
    return `${label} must be ${control.errors['maxlength'].requiredLength} characters or fewer.`;
  }

  if (!control.errors && formErrors?.['passwordMismatch']) {
    return 'Passwords must match.';
  }

  if (control.errors) {
    return `${label} needs attention.`;
  }

  return '';
}
