import { convertToParamMap, provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ResetPasswordPageComponent } from './reset-password-page.component';

describe('ResetPasswordPageComponent', () => {
  let fixture: ComponentFixture<ResetPasswordPageComponent>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['resetPassword']);

    await TestBed.configureTestingModule({
      imports: [ResetPasswordPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({
                email: 'reset-user@example.com',
                token: 'reset-token'
              })
            }
          }
        },
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ResetPasswordPageComponent);
    fixture.detectChanges();
  });

  it('prefills reset link values and submits a matching new password', () => {
    authService.resetPassword.and.returnValue(of({
      success: true,
      message: 'Password reset successful.',
      data: {}
    }));

    expect((fixture.nativeElement.querySelector('input[type="email"]') as HTMLInputElement).value).toBe('reset-user@example.com');

    setInput('input[formControlName="newPassword"]', 'NewPassword123!');
    setInput('input[formControlName="confirmPassword"]', 'NewPassword123!');
    submitForm();
    fixture.detectChanges();

    expect(authService.resetPassword).toHaveBeenCalledWith({
      email: 'reset-user@example.com',
      token: 'reset-token',
      newPassword: 'NewPassword123!'
    });
    expect(fixture.nativeElement.textContent).toContain('Password reset successful');
  });

  it('does not submit when password confirmation does not match', () => {
    setInput('input[formControlName="newPassword"]', 'NewPassword123!');
    setInput('input[formControlName="confirmPassword"]', 'DifferentPassword123!');
    submitForm();
    fixture.detectChanges();

    expect(authService.resetPassword).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Passwords must match');
  });

  function setInput(selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submitForm(): void {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
  }
});
