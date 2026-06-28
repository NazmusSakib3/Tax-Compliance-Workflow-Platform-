import { NgIf } from '@angular/common';
import { Component, ElementRef, HostListener, ViewChild, effect, inject } from '@angular/core';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [NgIf],
  template: `
    <div class="dialog-backdrop" *ngIf="confirmDialogService.request() as request" (click)="onBackdrop($event)">
      <div #dialogEl class="dialog page-card" role="dialog" aria-modal="true" tabindex="-1" [attr.aria-label]="request.title || 'Confirm'">
        <h2 class="section-title">{{ request.title || 'Are you sure?' }}</h2>
        <p class="section-subtitle">{{ request.message }}</p>
        <div class="dialog-actions">
          <button type="button" class="secondary-button" (click)="confirmDialogService.respond(false)">{{ request.cancelLabel || 'Cancel' }}</button>
          <button
            type="button"
            [class.danger-button]="(request.tone || 'danger') === 'danger'"
            [class.primary-button]="request.tone === 'primary'"
            (click)="confirmDialogService.respond(true)">
            {{ request.confirmLabel || 'Confirm' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dialog-backdrop {
      position: fixed;
      inset: 0;
      z-index: 900;
      display: grid;
      place-items: center;
      padding: 20px;
      background: rgba(15, 23, 42, 0.45);
      animation: dialog-fade 0.15s ease;
    }

    .dialog {
      width: min(420px, 100%);
      padding: 24px;
      display: grid;
      gap: 8px;
      box-shadow: var(--shadow-lg);
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      margin-top: 16px;
    }

    @keyframes dialog-fade {
      from { opacity: 0; }
      to { opacity: 1; }
    }
  `]
})
export class ConfirmDialogComponent {
  protected readonly confirmDialogService = inject(ConfirmDialogService);

  @ViewChild('dialogEl') private readonly dialogEl?: ElementRef<HTMLElement>;
  private previouslyFocused: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const request = this.confirmDialogService.request();
      if (request) {
        this.previouslyFocused = document.activeElement as HTMLElement | null;
        setTimeout(() => this.focusInitial());
      } else if (this.previouslyFocused) {
        this.previouslyFocused.focus?.();
        this.previouslyFocused = null;
      }
    });
  }

  protected onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.confirmDialogService.respond(false);
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.confirmDialogService.request()) {
      this.confirmDialogService.respond(false);
    }
  }

  @HostListener('document:keydown', ['$event'])
  protected onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || !this.confirmDialogService.request()) {
      return;
    }

    const focusable = this.focusableElements();
    if (!focusable.length) {
      event.preventDefault();
      this.dialogEl?.nativeElement.focus();
      return;
    }

    const first = focusable[0];
    const last = focusable.at(-1);
    const active = document.activeElement;

    if (event.shiftKey && (active === first || active === this.dialogEl?.nativeElement)) {
      event.preventDefault();
      last?.focus();
    } else if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private focusInitial(): void {
    const focusable = this.focusableElements();
    (focusable[0] ?? this.dialogEl?.nativeElement)?.focus();
  }

  private focusableElements(): HTMLElement[] {
    const root = this.dialogEl?.nativeElement;
    if (!root) {
      return [];
    }

    return Array.from(
      root.querySelectorAll<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      )
    ).filter((element) => !element.hasAttribute('disabled'));
  }
}
