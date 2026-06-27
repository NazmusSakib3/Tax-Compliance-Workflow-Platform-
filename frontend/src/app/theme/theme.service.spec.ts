import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.removeItem('tax-compliance-theme');
    document.body.classList.remove('dark-theme');

    TestBed.configureTestingModule({});
  });

  it('should toggle the body class and persist the selected theme', () => {
    const service = TestBed.inject(ThemeService);

    service.toggleTheme();

    expect(localStorage.getItem('tax-compliance-theme')).toBe(service.currentTheme());
    expect(document.body.classList.contains('dark-theme')).toBe(service.currentTheme() === 'dark');
  });
});
