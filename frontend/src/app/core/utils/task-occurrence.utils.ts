const TASK_OCCURRENCE_STATUS_LABELS = [
  'Unknown',
  'Draft',
  'Pending',
  'In Progress',
  'Completed',
  'Overdue',
  'Cancelled'
];

export function taskOccurrenceStatusLabel(status: number): string {
  return TASK_OCCURRENCE_STATUS_LABELS[status] ?? 'Unknown';
}

const TASK_OCCURRENCE_STATUS_CLASSES = [
  'status-draft',
  'status-draft',
  'status-pending',
  'status-in-progress',
  'status-completed',
  'status-overdue',
  'status-cancelled'
];

export function taskOccurrenceStatusClass(status: number): string {
  return TASK_OCCURRENCE_STATUS_CLASSES[status] ?? 'status-draft';
}
