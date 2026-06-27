import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginResponse } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  let fixture: ComponentFixture<LoginPageComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  const session: LoginResponse = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
    userId: 'user-1',
    email: 'mfa-user@example.com',
    displayName: 'MFA User',
    roles: ['Admin']
  };

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['login', 'hasRole']);
    authService.hasRole.and.callFake((role: string) => role === 'Admin');

    await TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(LoginPageComponent);
    fixture.detectChanges();
  });

  it('starts with empty credentials and links to password recovery', () => {
    const emailInput = fixture.nativeElement.querySelector('input[type="email"]') as HTMLInputElement;
    const passwordInput = fixture.nativeElement.querySelector('input[type="password"]') as HTMLInputElement;
    const forgotLink = fixture.nativeElement.querySelector('a[href="/forgot-password"]') as HTMLAnchorElement | null;

    expect(emailInput.value).toBe('');
    expect(passwordInput.value).toBe('');
    expect(forgotLink?.textContent).toContain('Forgot password');
    expect(fixture.nativeElement.textContent).not.toContain('Development sign-in');
    expect(fixture.nativeElement.textContent).not.toContain('Admin123!');
  });

  it('prompts for an MFA code after a password-only login challenge', fakeAsync(() => {
    authService.login.and.returnValues(
      throwError(() => new HttpErrorResponse({
        status: 401,
        error: {
          message: 'A valid MFA code is required.',
          data: { requiresMfa: true, email: 'mfa-user@example.com' }
        }
      })),
      of(session)
    );

    const component = fixture.componentInstance as unknown as {
      loginForm: { setValue: (value: object) => void; patchValue: (value: object) => void };
      submit: () => void;
    };

    component.loginForm.setValue({
      email: 'mfa-user@example.com',
      password: 'CorrectHorseBatteryStaple1!',
      mfaCode: ''
    });
    component.submit();
    tick();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Enter your authenticator code');

    component.loginForm.patchValue({ mfaCode: '123456' });
    component.submit();
    tick();
    fixture.detectChanges();

    expect(authService.login.calls.argsFor(1)[0]).toEqual({
      email: 'mfa-user@example.com',
      password: 'CorrectHorseBatteryStaple1!',
      mfaCode: '123456'
    });
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  }));
});
