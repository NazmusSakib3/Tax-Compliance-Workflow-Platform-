import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

export type SummaryCardTone = 'default' | 'danger' | 'warning' | 'success' | 'primary';

@Component({
  selector: 'app-summary-card',
  standalone: true,
  imports: [RouterLink],
  template: `
    <article class="card" [class]="'tone-' + tone" [class.clickable]="link">
      <div class="card-accent"></div>
      <div class="card-body">
        <p class="label">{{ label }}</p>
        <h3 class="value">{{ value }}</h3>
        <a *ngIf="link" [routerLink]="link" [queryParams]="queryParams" class="card-link">{{ linkLabel }}</a>
      </div>
    </article>
  `,
  styles: [`
    .card {
      position: relative;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      overflow: hidden;
      box-shadow: var(--shadow);
      height: 100%;
      transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
    }

    .card-accent {
      height: 4px;
      background: var(--border);
    }

    .tone-danger .card-accent { background: var(--danger); }
    .tone-warning .card-accent { background: var(--warning); }
    .tone-success .card-accent { background: var(--success); }
    .tone-primary .card-accent { background: var(--primary); }
    .tone-default .card-accent { background: var(--text-muted); }

    .card-body {
      padding: 20px 22px 22px;
    }

    .card.clickable:hover {
      border-color: color-mix(in srgb, var(--primary) 35%, var(--border));
      transform: translateY(-2px);
      box-shadow: var(--shadow-lg);
    }

    .label {
      margin: 0 0 8px;
      color: var(--text-muted);
      font-size: 0.875rem;
      font-weight: 500;
    }

    .value {
      margin: 0;
      font-size: 2.25rem;
      font-weight: 700;
      letter-spacing: -0.03em;
      line-height: 1.1;
    }

    .tone-danger .value { color: var(--danger); }
    .tone-warning .value { color: var(--warning); }
    .tone-success .value { color: var(--success); }
    .tone-primary .value { color: var(--primary); }

    .card-link {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      margin-top: 14px;
      color: var(--primary);
      text-decoration: none;
      font-weight: 600;
      font-size: 0.875rem;
    }

    .card-link::after {
      content: "→";
      transition: transform 0.15s ease;
    }

    .card.clickable:hover .card-link::after {
      transform: translateX(3px);
    }
  `]
})
export class SummaryCardComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) value = 0;
  @Input() link = '';
  @Input() linkLabel = 'View tasks';
  @Input() queryParams: Record<string, string> | null = null;
  @Input() tone: SummaryCardTone = 'default';
}
