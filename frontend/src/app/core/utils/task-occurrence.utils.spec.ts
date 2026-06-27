import { taskOccurrenceStatusClass, taskOccurrenceStatusLabel } from './task-occurrence.utils';

describe('task-occurrence.utils', () => {
  it('maps known statuses to labels', () => {
    expect(taskOccurrenceStatusLabel(1)).toBe('Draft');
    expect(taskOccurrenceStatusLabel(3)).toBe('In Progress');
    expect(taskOccurrenceStatusLabel(6)).toBe('Cancelled');
  });

  it('falls back to Unknown for out-of-range status labels', () => {
    expect(taskOccurrenceStatusLabel(99)).toBe('Unknown');
    expect(taskOccurrenceStatusLabel(-1)).toBe('Unknown');
  });

  it('maps known statuses to css classes', () => {
    expect(taskOccurrenceStatusClass(2)).toBe('status-pending');
    expect(taskOccurrenceStatusClass(4)).toBe('status-completed');
    expect(taskOccurrenceStatusClass(5)).toBe('status-overdue');
  });

  it('falls back to status-draft for out-of-range status classes', () => {
    expect(taskOccurrenceStatusClass(99)).toBe('status-draft');
    expect(taskOccurrenceStatusClass(-1)).toBe('status-draft');
  });
});
