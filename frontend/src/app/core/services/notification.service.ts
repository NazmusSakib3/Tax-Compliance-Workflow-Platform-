import { Injectable, signal } from '@angular/core';

export type NotificationTone = 'success' | 'error' | 'info';

export interface Notification {
  id: number;
  tone: NotificationTone;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private static nextId = 0;
  private readonly defaultDurationMs = 4000;

  readonly notifications = signal<Notification[]>([]);

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message, 6000);
  }

  info(message: string): void {
    this.push('info', message);
  }

  dismiss(id: number): void {
    this.notifications.update((items) => items.filter((item) => item.id !== id));
  }

  private push(tone: NotificationTone, message: string, durationMs = this.defaultDurationMs): void {
    const id = NotificationService.nextId++;
    this.notifications.update((items) => [...items, { id, tone, message }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }
}
