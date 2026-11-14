import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { Invoice, PagedResult } from '../../core/models/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-invoice-history',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="container">
      <div class="page-header">
        <div>
          <h2>Invoices & Receipts</h2>
          <p class="subtitle">Download official VAT/Tax PDF receipts generated automatically for your account.</p>
        </div>
        <div class="filter-controls">
          <label for="statusSelect">Filter Status:</label>
          <select id="statusSelect" class="form-control status-dropdown" [(ngModel)]="selectedStatus" (change)="onFilterChange()">
            <option [ngValue]="undefined">All Invoices</option>
            <option [ngValue]="3">Paid</option>
            <option [ngValue]="2">Open</option>
            <option [ngValue]="1">Draft</option>
            <option [ngValue]="4">Void</option>
          </select>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading invoice records...</p>
      </div>

      <div *ngIf="!isLoading && pagedData?.items?.length === 0" class="empty-state card">
        <div class="empty-icon">🧾</div>
        <h3>No invoices found</h3>
        <p>No invoices have been generated yet for your subscriptions.</p>
      </div>

      <div *ngIf="!isLoading && pagedData && pagedData.items.length > 0" class="card table-card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Invoice #</th>
                <th>Plan</th>
                <th>Billing Period</th>
                <th>Issued Date</th>
                <th>Total</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let inv of pagedData.items">
                <td>
                  <span class="font-mono font-bold">{{ inv.invoiceNumber }}</span>
                </td>
                <td>
                  <span class="plan-name">{{ inv.planName || 'Standard' }}</span>
                </td>
                <td class="font-sm text-muted">
                  {{ inv.billingPeriod }}
                </td>
                <td class="text-muted">
                  {{ inv.invoiceDateUtc | date:'mediumDate' }}
                </td>
                <td class="font-bold">
                  {{ inv.currency | uppercase }} \${{ inv.total | number:'1.2-2' }}
                </td>
                <td>
                  <span [ngClass]="getStatusBadgeClass(inv.status)">
                    {{ getStatusLabel(inv.status) }}
                  </span>
                </td>
                <td>
                  <button
                    class="btn btn-sm btn-outline download-btn"
                    [disabled]="downloadingId === inv.id"
                    (click)="downloadPdf(inv)">
                    <span *ngIf="downloadingId === inv.id">⏳ Preparing...</span>
                    <span *ngIf="downloadingId !== inv.id">📥 Download PDF</span>
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
    .container {
      max-width: 1100px;
      margin: 2rem auto;
      padding: 0 1rem;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
      gap: 1rem;
    }
    .page-header h2 {
      font-size: 1.75rem;
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
    .filter-controls label {
      font-size: 0.875rem;
      color: #475569;
      font-weight: 500;
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
    .font-mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      font-size: 0.85rem;
    }
    .font-bold {
      font-weight: 600;
      color: #0f172a;
    }
    .font-sm {
      font-size: 0.85rem;
    }
    .text-muted {
      color: #64748b;
    }
    .plan-name {
      font-weight: 500;
      color: #334155;
    }
    .download-btn {
      white-space: nowrap;
      font-size: 0.8rem;
      padding: 0.35rem 0.65rem;
    }
    .loading-state, .empty-state {
      text-align: center;
      padding: 3rem 1.5rem;
    }
    .empty-icon {
      font-size: 2.5rem;
      margin-bottom: 0.75rem;
    }
    .empty-state h3 {
      font-size: 1.25rem;
      margin-bottom: 0.5rem;
    }
    .empty-state p {
      color: #64748b;
    }
  `]
})
export class InvoiceHistoryComponent implements OnInit {
  pagedData?: PagedResult<Invoice>;
  currentPage = 1;
  pageSize = 10;
  selectedStatus?: number;
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
    this.invoiceService.getMyInvoices(this.currentPage, this.pageSize, this.selectedStatus).subscribe({
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
        this.notify.error('Could not download PDF invoice: ' + (err.error?.message || err.message));
        this.cdr.markForCheck();
      }
    });
  }

  getStatusBadgeClass(status: number): string {
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
