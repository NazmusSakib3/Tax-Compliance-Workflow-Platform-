import { NgFor } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [NgFor],
  template: `
    <nav class="pagination" aria-label="Pagination">
      <p class="pagination-summary">{{ summary }}</p>

      <label class="page-size">
        <span>Per page</span>
        <select [value]="pageSize" (change)="onPageSizeChange($event)">
          <option *ngFor="let size of pageSizeOptions" [value]="size">{{ size }}</option>
        </select>
      </label>

      <div class="pagination-controls">
        <button type="button" class="secondary-button" [disabled]="page <= 1" (click)="goTo(page - 1)">Previous</button>
        <span class="page-indicator">Page {{ page }} of {{ totalPages }}</span>
        <button type="button" class="secondary-button" [disabled]="page >= totalPages" (click)="goTo(page + 1)">Next</button>
      </div>
    </nav>
  `,
  styles: [`
    .pagination {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      margin-top: 20px;
      padding-top: 16px;
      border-top: 1px solid var(--border);
      color: var(--text-muted);
      font-size: 0.9375rem;
    }

    .pagination-summary {
      margin: 0;
    }

    .page-size {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      margin: 0;
    }

    .page-size span {
      white-space: nowrap;
    }

    .page-size select {
      width: auto;
      padding: 6px 10px;
    }

    .pagination-controls {
      display: inline-flex;
      align-items: center;
      gap: 12px;
    }

    .page-indicator {
      white-space: nowrap;
    }
  `]
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;
  @Input() pageSize = 25;
  @Input() pageSizeOptions: number[] = [10, 25, 50, 100];

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  protected get summary(): string {
    if (this.totalCount <= 0) {
      return 'No records';
    }

    const start = (this.page - 1) * this.pageSize + 1;
    const end = Math.min(this.page * this.pageSize, this.totalCount);
    return `Showing ${start}-${end} of ${this.totalCount}`;
  }

  protected goTo(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) {
      return;
    }

    this.pageChange.emit(page);
  }

  protected onPageSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (value && value !== this.pageSize) {
      this.pageSizeChange.emit(value);
    }
  }
}
