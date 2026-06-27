import { FormControl, Validators } from '@angular/forms';
import { fieldError } from './form-field-error.util';

describe('fieldError', () => {
  function touched(control: FormControl): FormControl {
    control.markAsTouched();
    return control;
  }

  it('returns empty string when control is missing or untouched', () => {
    expect(fieldError(null, 'Name')).toBe('');
    expect(fieldError(undefined, 'Name')).toBe('');
    expect(fieldError(new FormControl('', Validators.required), 'Name')).toBe('');
  });

  it('reports required errors', () => {
    const control = touched(new FormControl('', Validators.required));
    expect(fieldError(control, 'Email')).toBe('Email is required.');
  });

  it('reports email errors', () => {
    const control = touched(new FormControl('not-an-email', Validators.email));
    expect(fieldError(control, 'Email')).toBe('Enter a valid email address.');
  });

  it('reports minlength and maxlength errors with the required length', () => {
    const minControl = touched(new FormControl('ab', Validators.minLength(5)));
    expect(fieldError(minControl, 'Password')).toBe('Password must be at least 5 characters.');

    const maxControl = touched(new FormControl('abcdef', Validators.maxLength(3)));
    expect(fieldError(maxControl, 'Code')).toBe('Code must be 3 characters or fewer.');
  });

  it('reports a password mismatch form-level error when the control itself is valid', () => {
    const control = touched(new FormControl('value'));
    expect(fieldError(control, 'Confirm password', { passwordMismatch: true })).toBe(
      'Passwords must match.'
    );
  });

  it('falls back to a generic message for other control errors', () => {
    const control = touched(new FormControl('value', () => ({ custom: true })));
    expect(fieldError(control, 'Field')).toBe('Field needs attention.');
  });
});
