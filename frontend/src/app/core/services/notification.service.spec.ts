import { fakeAsync, tick } from '@angular/core/testing';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    service = new NotificationService();
  });

  it('queues success, error and info notifications with the right tone', () => {
    service.success('saved');
    service.error('boom');
    service.info('heads up');

    const items = service.notifications();
    expect(items.length).toBe(3);
    expect(items[0]).toEqual(jasmine.objectContaining({ tone: 'success', message: 'saved' }));
    expect(items[1]).toEqual(jasmine.objectContaining({ tone: 'error', message: 'boom' }));
    expect(items[2]).toEqual(jasmine.objectContaining({ tone: 'info', message: 'heads up' }));
  });

  it('removes a notification by id when dismissed', () => {
    service.success('first');
    const id = service.notifications()[0].id;

    service.dismiss(id);

    expect(service.notifications().length).toBe(0);
  });

  it('auto-dismisses notifications after their duration elapses', fakeAsync(() => {
    service.info('temporary');
    expect(service.notifications().length).toBe(1);

    tick(4000);

    expect(service.notifications().length).toBe(0);
  }));
});
