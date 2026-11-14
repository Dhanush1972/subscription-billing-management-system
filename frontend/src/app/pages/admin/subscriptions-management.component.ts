import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubscriptionService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { Subscription, PagedResult } from '../../core/models/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-subscriptions-management',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="admin-container">
      <div class="page-header">
        <div>
          <h2>Customer Subscriptions</h2>
          <p class="subtitle">Monitor recurring customer subscription states, renewal dates, and Stripe synchronization.</p>
        </div>
        <div class="filter-controls">
          <label for="statusFilter">Status:</label>
          <select id="statusFilter" class="form-control status-dropdown" [(ngModel)]="selectedStatus" (change)="onFilterChange()">
            <option [ngValue]="undefined">All Statuses</option>
            <option [ngValue]="2">Active</option>
            <option [ngValue]="1">Incomplete</option>
            <option [ngValue]="3">Past Due</option>
            <option [ngValue]="4">Canceled</option>
          </select>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading subscriptions...</p>
      </div>

      <div *ngIf="!isLoading && pagedData?.items?.length === 0" class="card empty-state">
        <p>No customer subscriptions found matching the filter criteria.</p>
      </div>

      <div *ngIf="!isLoading && pagedData && pagedData.items.length > 0" class="card table-card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Customer</th>
                <th>Plan</th>
                <th>Billing Interval</th>
                <th>Status</th>
                <th>Current Period End</th>
                <th>Renewal Behavior</th>
                <th>Stripe Sub ID</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let s of pagedData.items">
                <td>
                  <div class="font-bold text-dark">{{ s.userEmail }}</div>
                  <div class="font-sm text-muted">ID: {{ s.userId }}</div>
                </td>
                <td>
                  <span class="plan-badge">{{ s.plan.name }}</span>
                </td>
                <td>
                  <span class="text-muted">
                    \${{ s.plan.price | number:'1.2-2' }} / {{ s.plan.billingInterval === 2 ? 'yr' : 'mo' }}
                  </span>
                </td>
                <td>
                  <span [ngClass]="getStatusBadge(s.status)">
                    {{ getStatusLabel(s.status) }}
                  </span>
                </td>
                <td>
                  <span class="font-mono">{{ s.currentPeriodEndUtc | date:'mediumDate' }}</span>
                </td>
                <td>
                  <span *ngIf="s.cancelAtPeriodEnd" class="badge badge-warning">
                    Cancels at end
                  </span>
                  <span *ngIf="!s.cancelAtPeriodEnd && s.status === 2" class="badge badge-success">
                    Auto-renews
                  </span>
                  <span *ngIf="s.status === 4" class="badge badge-secondary">
                    Terminated
                  </span>
                </td>
                <td>
                  <span class="font-mono font-sm text-muted">{{ s.stripeSubscriptionId || '—' }}</span>
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
    .filter-controls {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .status-dropdown {
      min-width: 150px;
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
    .plan-badge {
      background: #e0f2fe;
      color: #0369a1;
      padding: 0.25rem 0.6rem;
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.825rem;
    }
    .font-bold { font-weight: 600; }
    .text-dark { color: #0f172a; }
    .text-muted { color: #64748b; }
    .font-sm { font-size: 0.825rem; }
    .font-mono { font-family: monospace; }
    .empty-state, .loading-state {
      text-align: center;
      padding: 3rem;
      color: #64748b;
    }
  `]
})
export class SubscriptionsManagementComponent implements OnInit {
  pagedData?: PagedResult<Subscription>;
  currentPage = 1;
  pageSize = 10;
  selectedStatus?: number;
  isLoading = false;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private subscriptionService: SubscriptionService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadSubscriptions();
  }

  loadSubscriptions(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.subscriptionService.getAllSubscriptions(this.currentPage, this.pageSize, this.selectedStatus).subscribe({
      next: (res) => {
        this.pagedData = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to load subscriptions: ' + (err.error?.message || err.message));
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadSubscriptions();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadSubscriptions();
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
      case 1: return 'Incomplete';
      case 2: return 'Active';
      case 3: return 'Past Due';
      case 4: return 'Canceled';
      default: return 'Unknown';
    }
  }
}
