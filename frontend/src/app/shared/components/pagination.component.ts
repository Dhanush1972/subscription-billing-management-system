import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="pagination-wrapper" *ngIf="totalPages > 1">
      <div class="pagination-info">
        Showing page {{ pageNumber }} of {{ totalPages }} ({{ totalCount }} items)
      </div>
      <div class="pagination-buttons">
        <button class="btn btn-secondary btn-sm"
                [disabled]="!hasPrevious"
                (click)="onPageChange(pageNumber - 1)">
          Previous
        </button>
        <span class="page-current">{{ pageNumber }}</span>
        <button class="btn btn-secondary btn-sm"
                [disabled]="!hasNext"
                (click)="onPageChange(pageNumber + 1)">
          Next
        </button>
      </div>
    </div>
  `,
  styles: [`
    .pagination-wrapper {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 1.25rem;
      padding-top: 1rem;
      border-top: 1px solid #e2e8f0;
      padding-left: 1rem;
      padding-right: 1rem;
    }
    .pagination-info {
      font-size: 0.8125rem;
      color: #64748b;
    }
    .pagination-buttons {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .page-current {
      font-size: 0.875rem;
      font-weight: 600;
      color: #1e293b;
      padding: 0 0.5rem;
    }
  `]
})
export class PaginationComponent {
  @Input() pageNumber = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;

  get hasPrevious(): boolean {
    return this.pageNumber > 1;
  }

  get hasNext(): boolean {
    return this.pageNumber < this.totalPages;
  }

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageChanged = this.pageChange;

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.pageChange.emit(newPage);
    }
  }
}
