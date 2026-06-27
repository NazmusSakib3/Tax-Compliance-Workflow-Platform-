import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'tax-compliance-theme';
  private readonly document = inject(DOCUMENT);
  private readonly themeSignal = signal<ThemeMode>(this.resolveInitialTheme());

  constructor() {
    this.applyTheme(this.themeSignal());
  }

  currentTheme(): ThemeMode {
    return this.themeSignal();
  }

  toggleTheme(): void {
    const nextTheme: ThemeMode = this.themeSignal() === 'dark' ? 'light' : 'dark';
    this.themeSignal.set(nextTheme);
    localStorage.setItem(this.storageKey, nextTheme);
    this.applyTheme(nextTheme);
  }

  private resolveInitialTheme(): ThemeMode {
    const storedTheme = localStorage.getItem(this.storageKey);
    if (storedTheme === 'light' || storedTheme === 'dark') {
      return storedTheme;
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private applyTheme(theme: ThemeMode): void {
    this.document.body.classList.toggle('dark-theme', theme === 'dark');
  }
}
