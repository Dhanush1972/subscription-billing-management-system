import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { Invoice, PagedResult } from '../../core/models/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-invoices-management',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="admin-container">
      <div class="page-header">
        <div>
          <h2>Invoice & Billing Records</h2>
          <p class="subtitle">Search, view line-items, and download generated PDF invoices across all customer accounts.</p>
        </div>
        <div class="filter-bar">
          <input
            type="text"
            class="form-control search-input"
            placeholder="Search invoice # or email..."
            [(ngModel)]="searchQuery"
            (keyup.enter)="onFilterChange()" />

          <select class="form-control status-dropdown" [(ngModel)]="selectedStatus" (change)="onFilterChange()">
            <option [ngValue]="undefined">All Statuses</option>
            <option [ngValue]="3">Paid</option>
            <option [ngValue]="2">Open</option>
            <option [ngValue]="1">Draft</option>
            <option [ngValue]="4">Void</option>
          </select>

          <button class="btn btn-outline btn-sm" (click)="onFilterChange()">Filter</button>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading invoice records...</p>
      </div>

      <div *ngIf="!isLoading && pagedData?.items?.length === 0" class="card empty-state">
        <p>No invoices found matching your query.</p>
      </div>

      <div *ngIf="!isLoading && pagedData && pagedData.items.length > 0" class="card table-card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Invoice #</th>
                <th>Customer</th>
                <th>Plan</th>
                <th>Billing Period</th>
                <th>Issue Date</th>
                <th>Subtotal</th>
                <th>Tax</th>
                <th>Total</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let inv of pagedData.items">
                <td>
                  <span class="font-mono font-bold">{{ inv.invoiceNumber }}</span>
                </td>
                <td>
                  <div class="font-bold text-dark">{{ inv.userEmail }}</div>
                  <div class="font-sm text-muted">ID: {{ inv.userId }}</div>
                </td>
                <td>
                  <span class="plan-badge">{{ inv.planName || 'Standard' }}</span>
                </td>
                <td class="font-sm text-muted">
                  {{ inv.billingPeriod }}
                </td>
                <td class="text-muted font-sm">
                  {{ inv.invoiceDateUtc | date:'mediumDate' }}
                </td>
                <td class="text-muted">
                  \${{ inv.subtotal | number:'1.2-2' }}
                </td>
                <td class="text-muted">
                  \${{ inv.tax | number:'1.2-2' }}
                </td>
                <td class="font-bold text-dark">
                  \${{ inv.total | number:'1.2-2' }}
                </td>
                <td>
                  <span [ngClass]="getStatusBadge(inv.status)">
                    {{ getStatusLabel(inv.status) }}
                  </span>
                </td>
                <td>
                  <button
                    class="btn btn-sm btn-outline download-btn"
                    [disabled]="downloadingId === inv.id"
                    (click)="downloadPdf(inv)">
                    <span *ngIf="downloadingId === inv.id">⏳...</span>
                    <span *ngIf="downloadingId !== inv.id">📥 PDF</span>
                  </button>
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
    .plan-badge {
      background: #e0f2fe;
      color: #0369a1;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-size: 0.8rem;
      font-weight: 500;
    }
    .font-bold { font-weight: 600; }
    .text-dark { color: #0f172a; }
    .text-muted { color: #64748b; }
    .font-sm { font-size: 0.825rem; }
    .font-mono { font-family: monospace; }
    .download-btn {
      white-space: nowrap;
      font-size: 0.8rem;
      padding: 0.25rem 0.5rem;
    }
    .empty-state, .loading-state {
      text-align: center;
      padding: 3rem;
      color: #64748b;
    }
  `]
})
export class InvoicesManagementComponent implements OnInit {
  pagedData?: PagedResult<Invoice>;
  currentPage = 1;
  pageSize = 10;
  selectedStatus?: number;
  searchQuery = '';
  isLoading = false;
  downloadingId?: number;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private invoiceService: InvoiceService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.invoiceService.getAllInvoices(this.currentPage, this.pageSize, this.selectedStatus, this.searchQuery).subscribe({
      next: (res) => {
        this.pagedData = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to load invoices: ' + (err.error?.message || err.message));
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadInvoices();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadInvoices();
  }

  downloadPdf(invoice: Invoice): void {
    this.downloadingId = invoice.id;
    this.cdr.markForCheck();
    this.invoiceService.downloadPdf(invoice.id).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${invoice.invoiceNumber}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.downloadingId = undefined;
        this.notify.success(`Downloaded ${invoice.invoiceNumber}.pdf`);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.downloadingId = undefined;
        this.notify.error('Could not download invoice: ' + (err.error?.message || err.message));
        this.cdr.markForCheck();
      }
    });
  }

  getStatusBadge(status: number): string {
    switch (status) {
      case 3: return 'badge badge-success';
      case 2: return 'badge badge-warning';
      case 1: return 'badge badge-secondary';
      case 4: return 'badge badge-danger';
      default: return 'badge badge-secondary';
    }
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 1: return 'Draft';
      case 2: return 'Open';
      case 3: return 'Paid';
      case 4: return 'Void';
      default: return 'Unknown';
    }
  }
}
