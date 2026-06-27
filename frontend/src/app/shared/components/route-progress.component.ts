import { NgIf } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router
} from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-route-progress',
  standalone: true,
  imports: [NgIf],
  template: `
    <div class="route-progress" *ngIf="active()" role="progressbar" aria-label="Loading page" aria-busy="true">
      <span class="route-progress-bar"></span>
    </div>
  `
})
export class RouteProgressComponent implements OnDestroy {
  private readonly router = inject(Router);
  private readonly subscription: Subscription;

  protected readonly active = signal(false);

  constructor() {
    this.subscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.active.set(true);
      } else if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError
      ) {
        this.active.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}
