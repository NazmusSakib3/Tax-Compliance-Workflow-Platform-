import { NgClass, NgFor } from '@angular/common';
import { Component, inject } from '@angular/core';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [NgFor, NgClass],
  template: `
    <div class="toast-stack" aria-live="polite" aria-atomic="false">
      <div
        *ngFor="let toast of notificationService.notifications()"
        class="toast"
        [ngClass]="'toast-' + toast.tone"
        role="status">
        <span class="toast-message">{{ toast.message }}</span>
        <button type="button" class="toast-close" aria-label="Dismiss notification" (click)="notificationService.dismiss(toast.id)">×</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-stack {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 1000;
      display: flex;
      flex-direction: column;
      gap: 10px;
      max-width: min(360px, calc(100vw - 40px));
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 14px;
      border-radius: var(--radius-md);
      border: 1px solid var(--border);
      background: var(--surface);
      box-shadow: var(--shadow-lg);
      animation: toast-in 0.18s ease;
    }

    .toast-message {
      flex: 1;
      font-size: 0.9375rem;
      line-height: 1.4;
    }

    .toast::before {
      content: "";
      width: 4px;
      align-self: stretch;
      border-radius: 999px;
      background: var(--text-muted);
    }

    .toast-success::before { background: var(--success); }
    .toast-error::before { background: var(--danger); }
    .toast-info::before { background: var(--info); }

    .toast-close {
      padding: 0;
      width: 22px;
      height: 22px;
      line-height: 1;
      border: 0;
      border-radius: var(--radius-sm);
      background: transparent;
      color: var(--text-muted);
      font-size: 1.1rem;
      cursor: pointer;
    }

    .toast-close:hover:not(:disabled) {
      transform: none;
      color: var(--text);
    }

    @keyframes toast-in {
      from { opacity: 0; transform: translateY(-6px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class ToastContainerComponent {
  protected readonly notificationService = inject(NotificationService);
}
