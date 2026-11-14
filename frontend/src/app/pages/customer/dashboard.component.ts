import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SubscriptionService, PaymentService, InvoiceService, SimulationService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { Subscription, Payment, Invoice } from '../../core/models/models';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container">
      <div class="welcome-header">
        <div>
          <h2>Welcome back, {{ user?.firstName || 'Subscriber' }}! 👋</h2>
          <p class="subtitle">Manage your subscription, monitor billing cycles, and download tax invoices.</p>
        </div>
        <div class="header-actions">
          <a routerLink="/plans" class="btn btn-primary">Browse All Plans</a>
        </div>
      </div>

      <!-- Quick Summary Cards -->
      <div class="metrics-grid">
        <!-- Active Subscription Card -->
        <div class="card metric-card">
          <div class="metric-icon plan-icon">⭐</div>
          <div class="metric-info">
            <span class="metric-label">Active Plan</span>
            <h3 class="metric-value">{{ subscription?.plan?.name || 'No Active Plan' }}</h3>
            <span *ngIf="subscription" [ngClass]="getStatusBadge(subscription.status)">
              {{ getStatusLabel(subscription.status) }}
            </span>
          </div>
          <div class="metric-footer" *ngIf="subscription">
            <span>Renews on: {{ subscription.currentPeriodEndUtc | date:'mediumDate' }}</span>
          </div>
        </div>

        <!-- Latest Payment Card -->
        <div class="card metric-card">
          <div class="metric-icon payment-icon">💳</div>
          <div class="metric-info">
            <span class="metric-label">Latest Payment</span>
            <h3 class="metric-value">
              {{ latestPayment ? ('$' + (latestPayment.amount | number:'1.2-2')) : 'No payments' }}
            </h3>
            <span *ngIf="latestPayment" class="badge badge-success">
              {{ latestPayment.paymentDateUtc | date:'shortDate' }}
            </span>
          </div>
          <div class="metric-footer">
            <a routerLink="/payments" class="metric-link">View payment history &rarr;</a>
          </div>
        </div>

        <!-- Invoices Card -->
        <div class="card metric-card">
          <div class="metric-icon invoice-icon">🧾</div>
          <div class="metric-info">
            <span class="metric-label">Latest Invoice</span>
            <h3 class="metric-value">
              {{ latestInvoice ? latestInvoice.invoiceNumber : 'None' }}
            </h3>
            <span *ngIf="latestInvoice" class="badge badge-primary">
              {{ latestInvoice.total | currency:'USD':'symbol':'1.2-2' }}
            </span>
          </div>
          <div class="metric-footer">
            <a routerLink="/invoices" class="metric-link">View all invoices &rarr;</a>
          </div>
        </div>
      </div>

      <!-- Main Subscription Details -->
      <div class="card subscription-overview" *ngIf="subscription">
        <div class="overview-header">
          <div>
            <h3>Current Subscription Details</h3>
            <p class="text-muted">Billed {{ subscription.plan.billingInterval === 2 ? 'yearly' : 'monthly' }} via Stripe</p>
          </div>
          <div class="overview-actions">
            <a routerLink="/subscription/my" class="btn btn-outline">Manage Subscription</a>
          </div>
        </div>

        <div class="plan-details-grid">
          <div class="detail-item">
            <span class="detail-label">Plan Price</span>
            <span class="detail-val font-bold">\${{ subscription.plan.price | number:'1.2-2' }} / {{ subscription.plan.billingInterval === 2 ? 'year' : 'month' }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">Current Period Start</span>
            <span class="detail-val">{{ subscription.currentPeriodStartUtc | date:'mediumDate' }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">Current Period End</span>
            <span class="detail-val">{{ subscription.currentPeriodEndUtc | date:'mediumDate' }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">Auto-Renew</span>
            <span class="detail-val" [class.text-danger]="subscription.cancelAtPeriodEnd">
              {{ subscription.cancelAtPeriodEnd ? 'Cancels at period end' : 'Active (Auto-renews)' }}
            </span>
          </div>
        </div>

        <div class="features-summary">
          <span class="features-title">Included Features:</span>
          <div class="features-tags">
            <span *ngFor="let f of subscription.plan.features" class="feature-tag">✓ {{ f }}</span>
          </div>
        </div>
      </div>

      <!-- No Subscription Banner -->
      <div class="card empty-sub-card" *ngIf="!subscription && !isLoading">
        <div class="empty-sub-content">
          <span class="sub-icon">🚀</span>
          <h3>Unlock Full Access to All Capabilities</h3>
          <p>You do not have an active subscription yet. Select a plan to experience flexible billing and automatic PDF invoicing.</p>
          <a routerLink="/plans" class="btn btn-primary btn-lg">Explore Pricing Plans</a>
        </div>
      </div>

      <!-- Developer Simulation Card (For Local Interview Demo) -->
      <div class="card simulation-card" *ngIf="user">
        <div class="sim-header">
          <span class="sim-badge">Portfolio Demo Feature</span>
          <h4>Webhook Simulation Mode</h4>
        </div>
        <p class="sim-desc">
          When running locally without a live Stripe CLI webhook forwarding tunnel, click below to trigger a backend 
          event simulation. This will generate a real paid invoice, record an audit log, and send a renewal confirmation.
        </p>
        <div class="sim-actions">
          <button class="btn btn-sm btn-secondary" [disabled]="isSimulating" (click)="triggerSimulation(1)">
            Simulate Monthly Renewal Webhook
          </button>
          <button class="btn btn-sm btn-secondary" [disabled]="isSimulating" (click)="triggerSimulation(2)">
            Simulate Annual Upgrade Webhook
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1100px;
      margin: 2rem auto;
      padding: 0 1rem;
    }
    .welcome-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      flex-wrap: wrap;
      gap: 1rem;
    }
    .welcome-header h2 {
      font-size: 1.85rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.25rem;
    }
    .subtitle {
      color: #64748b;
      font-size: 0.95rem;
    }
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.25rem;
      margin-bottom: 2rem;
    }
    .metric-card {
      position: relative;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
    }
    .metric-icon {
      font-size: 1.75rem;
      position: absolute;
      top: 1.25rem;
      right: 1.25rem;
      background: #f1f5f9;
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 10px;
    }
    .metric-label {
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #64748b;
      font-weight: 600;
    }
    .metric-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0.35rem 0 0.5rem;
    }
    .metric-footer {
      margin-top: 1.25rem;
      padding-top: 0.75rem;
      border-top: 1px solid #f1f5f9;
      font-size: 0.825rem;
      color: #64748b;
    }
    .metric-link {
      color: #2563eb;
      text-decoration: none;
      font-weight: 600;
    }
    .metric-link:hover {
      text-decoration: underline;
    }
    .subscription-overview {
      margin-bottom: 2rem;
      padding: 1.75rem;
    }
    .overview-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
      gap: 1rem;
    }
    .overview-header h3 {
      font-size: 1.25rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.25rem;
    }
    .plan-details-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
      padding: 1.25rem;
      background: #f8fafc;
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }
    .detail-item {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .detail-label {
      font-size: 0.8rem;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .detail-val {
      font-size: 1rem;
      color: #1e293b;
    }
    .font-bold {
      font-weight: 600;
    }
    .text-danger {
      color: #dc2626;
      font-weight: 600;
    }
    .features-summary {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .features-title {
      font-size: 0.85rem;
      font-weight: 600;
      color: #475569;
    }
    .features-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
    .feature-tag {
      background: #e0f2fe;
      color: #0369a1;
      padding: 0.3rem 0.65rem;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 500;
    }
    .empty-sub-card {
      text-align: center;
      padding: 3.5rem 2rem;
      margin-bottom: 2rem;
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
    }
    .sub-icon {
      font-size: 3rem;
      display: block;
      margin-bottom: 1rem;
    }
    .empty-sub-content h3 {
      font-size: 1.5rem;
      color: #0f172a;
      margin-bottom: 0.5rem;
    }
    .empty-sub-content p {
      max-width: 520px;
      margin: 0 auto 1.5rem;
      color: #64748b;
    }
    .simulation-card {
      border: 1px dashed #cbd5e1;
      background: #fafafa;
      padding: 1.5rem;
    }
    .sim-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.5rem;
    }
    .sim-badge {
      background: #e2e8f0;
      color: #334155;
      font-size: 0.7rem;
      font-weight: 700;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      text-transform: uppercase;
    }
    .sim-header h4 {
      font-size: 1rem;
      color: #1e293b;
      margin: 0;
    }
    .sim-desc {
      font-size: 0.85rem;
      color: #64748b;
      line-height: 1.4;
      margin-bottom: 1rem;
    }
    .sim-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }
    .text-muted {
      color: #64748b;
    }
  `]
})
export class CustomerDashboardComponent implements OnInit {
  get user() {
    return this.authService.currentUser();
  }
  subscription?: Subscription | null;
  latestPayment?: Payment;
  latestInvoice?: Invoice;
  isLoading = true;
  isSimulating = false;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private authService: AuthService,
    private subscriptionService: SubscriptionService,
    private paymentService: PaymentService,
    private invoiceService: InvoiceService,
    private simulationService: SimulationService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.subscriptionService.getMySubscription().subscribe({
      next: (sub) => {
        this.subscription = sub;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });

    this.paymentService.getMyPayments(1, 1).subscribe({
      next: (res) => {
        if (res.items.length > 0) {
          this.latestPayment = res.items[0];
          this.cdr.markForCheck();
        }
      }
    });

    this.invoiceService.getMyInvoices(1, 1).subscribe({
      next: (res) => {
        if (res.items.length > 0) {
          this.latestInvoice = res.items[0];
          this.cdr.markForCheck();
        }
      }
    });
  }

  triggerSimulation(planId: number): void {
    const current = this.authService.currentUser();
    if (!current) return;
    this.isSimulating = true;
    this.simulationService.simulatePayment(current.id, planId).subscribe({
      next: (res) => {
        this.isSimulating = false;
        this.notify.success('Webhook simulation processed successfully! Refreshed records.');
        this.loadData();
      },
      error: (err) => {
        this.isSimulating = false;
        this.notify.error('Simulation failed: ' + (err.error?.message || err.message));
      }
    });
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
