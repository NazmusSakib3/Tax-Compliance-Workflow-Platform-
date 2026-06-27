import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  imports: [NgFor, NgIf],
  template: `
    <section class="loading-skeleton" role="status" aria-live="polite">
      <span class="sr-only">{{ title }}</span>
      <p class="skeleton-title" *ngIf="title" aria-hidden="true">{{ title }}</p>
      <p class="skeleton-subtitle" *ngIf="subtitle" aria-hidden="true">{{ subtitle }}</p>
      <div class="skeleton-rows" aria-hidden="true">
        <div class="skeleton-line" *ngFor="let row of rowsArray"></div>
      </div>
    </section>
  `,
  styles: [`
    .loading-skeleton {
      padding: 24px;
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      background: var(--surface);
    }

    .skeleton-title {
      margin: 0 0 6px;
      font-size: 1rem;
      font-weight: 600;
    }

    .skeleton-subtitle {
      margin: 0 0 18px;
      color: var(--text-muted);
      font-size: 0.9375rem;
    }

    .skeleton-rows {
      display: grid;
      gap: 12px;
    }
  `]
})
export class LoadingStateComponent {
  @Input() title = 'Loading';
  @Input() subtitle = '';
  @Input() rows = 4;

  protected get rowsArray(): number[] {
    return Array.from({ length: Math.max(1, this.rows) }, (_, index) => index);
  }
}
