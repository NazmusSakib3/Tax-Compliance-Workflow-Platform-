import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [NgIf],
  template: `
    <section class="empty-state" [class.align-start]="alignStart">
      <h3 class="section-title">{{ title }}</h3>
      <p class="section-subtitle" *ngIf="subtitle">{{ subtitle }}</p>
      <ng-content></ng-content>
    </section>
  `,
  styles: [`
    .align-start { text-align: left; }
  `]
})
export class EmptyStateComponent {
  @Input({ required: true }) title = '';
  @Input() subtitle = '';
  @Input() alignStart = false;
}
