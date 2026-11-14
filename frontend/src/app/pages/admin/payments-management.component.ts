import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { Payment, PagedResult } from '../../core/models/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-payments-management',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="admin-container">
      <div class="page-header">
        <div>
          <h2>Platform Payment Ledger</h2>
          <p class="subtitle">Search, inspect and audit all customer payment transactions and gateway charges.</p>
        </div>
        <div class="filter-bar">
          <input
            type="text"
            class="form-control search-input"
            placeholder="Search email or Stripe Tx..."
            [(ngModel)]="searchQuery"
            (keyup.enter)="onFilterChange()" />

          <select class="form-control status-dropdown" [(ngModel)]="selectedStatus" (change)="onFilterChange()">
            <option [ngValue]="undefined">All Statuses</option>
            <option [ngValue]="2">Succeeded</option>
            <option [ngValue]="1">Pending</option>
            <option [ngValue]="3">Failed</option>
            <option [ngValue]="4">Refunded</option>
          </select>

          <button class="btn btn-outline btn-sm" (click)="onFilterChange()">Search</button>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Retrieving transaction ledger...</p>
      </div>

      <div *ngIf="!isLoading && pagedData?.items?.length === 0" class="card empty-state">
        <p>No payment records match your search criteria.</p>
      </div>

      <div *ngIf="!isLoading && pagedData && pagedData.items.length > 0" class="card table-card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Tx ID</th>
                <th>Customer</th>
                <th>Invoice #</th>
                <th>Amount</th>
                <th>Method</th>
                <th>Status</th>
                <th>Settled Date (UTC)</th>
                <th>Gateway Notes</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of pagedData.items">
                <td>
                  <span class="font-mono font-sm text-muted">{{ p.stripePaymentIntentId || ('TX-' + p.id) }}</span>
                </td>
                <td>
                  <div class="font-bold text-dark">{{ p.userEmail }}</div>
                  <div class="font-sm text-muted">User ID: {{ p.userId }}</div>
                </td>
                <td>
                  <span class="font-mono font-bold">{{ p.invoiceNumber || '—' }}</span>
                </td>
                <td class="font-bold text-dark">
                  \${{ p.amount | number:'1.2-2' }} <span class="font-sm text-muted">{{ p.currency | uppercase }}</span>
                </td>
                <td>
                  <span class="payment-method-pill">💳 {{ p.paymentMethodSummary }}</span>
                </td>
                <td>
                  <span [ngClass]="getStatusBadge(p.status)">
                    {{ getStatusLabel(p.status) }}
                  </span>
                </td>
                <td class="text-muted font-sm">
                  {{ p.paymentDateUtc | date:'medium' }}
                </td>
                <td>
                  <span *ngIf="p.failureMessage" class="text-danger font-sm" [title]="p.failureMessage">
                    ⚠️ {{ p.failureMessage }}
                  </span>
                  <span *ngIf="!p.failureMessage" class="text-muted font-sm">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-pagination
          [pageNumber]="pagedData.pageNumber"
          [totalPages]="pagedData.totalPages"
          [totalCount]="pagedData.totalCount"
          (pageChange)="onPageChange($event)">
        </app-pagination>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      max-width: 1200px;
      margin: 2rem auto;
      padding: 0 1rem;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      flex-wrap: wrap;
      gap: 1rem;
    }
    .page-header h2 {
      font-size: 1.85rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.25rem;
    }
    .subtitle {
      color: #64748b;
      font-size: 0.95rem;
    }
    .filter-bar {
      display: flex;
      gap: 0.75rem;
      align-items: center;
      flex-wrap: wrap;
    }
    .search-input {
      width: 240px;
      padding: 0.45rem 0.75rem;
      font-size: 0.875rem;
    }
    .status-dropdown {
      min-width: 140px;
      padding: 0.45rem 0.75rem;
      font-size: 0.875rem;
    }
    .table-card {
      padding: 0;
      overflow: hidden;
    }
    .table-responsive {
      overflow-x: auto;
    }
    .table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }
    .table th {
      background: #f8fafc;
      color: #475569;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 0.85rem 1.25rem;
      border-bottom: 1px solid #e2e8f0;
    }
    .table td {
      padding: 1rem 1.25rem;
      border-bottom: 1px solid #f1f5f9;
      font-size: 0.9rem;
      vertical-align: middle;
    }
    .table tr:last-child td {
      border-bottom: none;
    }
    .font-bold { font-weight: 600; }
    .text-dark { color: #0f172a; }
    .text-muted { color: #64748b; }
    .text-danger { color: #dc2626; }
    .font-sm { font-size: 0.825rem; }
    .font-mono { font-family: monospace; }
    .payment-method-pill {
      background: #f1f5f9;
      color: #334155;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-size: 0.8rem;
    }
    .empty-state, .loading-state {
      text-align: center;
      padding: 3rem;
      color: #64748b;
    }
  `]
})
export class PaymentsManagementComponent implements OnInit {
  pagedData?: PagedResult<Payment>;
  currentPage = 1;
  pageSize = 10;
  selectedStatus?: number;
  searchQuery = '';
  isLoading = false;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private paymentService: PaymentService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadPayments();
  }

  loadPayments(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.paymentService.getAllPayments(this.currentPage, this.pageSize, this.selectedStatus, this.searchQuery).subscribe({
      next: (res) => {
        this.pagedData = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to load payments: ' + (err.error?.message || err.message));
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadPayments();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadPayments();
  }

  getStatusBadge(status: number): string {
    switch (status) {
      case 2: return 'badge badge-success';
      case 1: return 'badge badge-warning';
      case 3: return 'badge badge-danger';
      case 4: return 'badge badge-secondary';
      default: return 'badge badge-secondary';
    }
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 1: return 'Pending';
      case 2: return 'Succeeded';
      case 3: return 'Failed';
      case 4: return 'Refunded';
      default: return 'Unknown';
    }
  }
}
