import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthenticatedUser } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { AccountSecurityPageComponent } from './account-security-page.component';

describe('AccountSecurityPageComponent', () => {
  let fixture: ComponentFixture<AccountSecurityPageComponent>;
  let authService: jasmine.SpyObj<AuthService>;

  const disabledUser: AuthenticatedUser = {
    userId: 'user-1',
    email: 'user@example.com',
    displayName: 'User Example',
    roles: ['Admin'],
    isMfaEnabled: false
  };

  const enabledUser: AuthenticatedUser = {
    ...disabledUser,
    isMfaEnabled: true
  };

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', [
      'getCurrentUser',
      'setupMfa',
      'enableMfa',
      'disableMfa'
    ]);

    await TestBed.configureTestingModule({
      imports: [AccountSecurityPageComponent],
      providers: [
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();
  });

  it('sets up and enables MFA with an authenticator code', () => {
    authService.getCurrentUser.and.returnValues(of(disabledUser), of(enabledUser));
    authService.setupMfa.and.returnValue(of({
      sharedKey: 'ABC123',
      authenticatorUri: 'otpauth://totp/user@example.com?secret=ABC123'
    }));
    authService.enableMfa.and.returnValue(of({
      success: true,
      message: 'MFA enabled.',
      data: {}
    }));

    createComponent();
    clickButton('Set up MFA');
    fixture.detectChanges();

    expect(authService.setupMfa).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('ABC123');

    setInput('input[formControlName="setupCode"]', '123456');
    submitForm('.setup-form');
    fixture.detectChanges();

    expect(authService.enableMfa).toHaveBeenCalledWith({ code: '123456' });
    expect(fixture.nativeElement.textContent).toContain('MFA is enabled');
  });

  it('disables MFA after verifying the current authenticator code', () => {
    authService.getCurrentUser.and.returnValues(of(enabledUser), of(disabledUser));
    authService.disableMfa.and.returnValue(of({
      success: true,
      message: 'MFA disabled.',
      data: {}
    }));

    createComponent();
    setInput('input[formControlName="disableCode"]', '654321');
    submitForm('.disable-form');
    fixture.detectChanges();

    expect(authService.disableMfa).toHaveBeenCalledWith({ code: '654321' });
    expect(fixture.nativeElement.textContent).toContain('MFA is disabled');
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(AccountSecurityPageComponent);
    fixture.detectChanges();
  }

  function clickButton(text: string): void {
    const button = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((candidate) => (candidate as HTMLButtonElement).textContent?.includes(text)) as HTMLButtonElement;
    button.click();
  }

  function setInput(selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submitForm(selector: string): void {
    const form = fixture.nativeElement.querySelector(selector) as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
  }
});
