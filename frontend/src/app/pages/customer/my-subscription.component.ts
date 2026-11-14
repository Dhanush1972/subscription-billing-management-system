import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SubscriptionService, PlanService, SimulationService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';
import { Subscription, SubscriptionPlan } from '../../core/models/models';

@Component({
  selector: 'app-my-subscription',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container">
      <div class="page-header">
        <h1>My Subscription</h1>
        <p class="subtitle">Manage your active billing tier, renewals, and lifecycle</p>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading subscription details...</p>
      </div>

      <div *ngIf="!isLoading && !subscription" class="empty-state card">
        <div class="empty-icon">📦</div>
        <h3>No Active Subscription</h3>
        <p>You are not currently subscribed to any plan. Choose a plan to unlock all features.</p>
        <a routerLink="/plans" class="btn btn-primary">Browse Subscription Plans</a>
      </div>

      <div *ngIf="!isLoading && subscription" class="sub-layout">
        <!-- Cancellation Banner -->
        <div *ngIf="subscription.cancelAtPeriodEnd" class="alert alert-warning">
          <span>⚠️ Your subscription is scheduled to cancel on <strong>{{ subscription.currentPeriodEndUtc | date:'mediumDate' }}</strong>. You still have access until then.</span>
          <button class="btn btn-secondary btn-sm" (click)="onReactivate()">Reactivate Subscription</button>
        </div>

        <!-- Main Card -->
        <div class="card sub-card">
          <div class="sub-card-header">
            <div>
              <span class="badge" [ngClass]="getStatusBadgeClass(subscription.status)">
                {{ getStatusText(subscription.status) }}
              </span>
              <h2>{{ subscription.plan.name }} Plan</h2>
            </div>
            <div class="price-tag">
              <span class="amount">\${{ subscription.plan.price | number:'1.2-2' }}</span>
              <span class="interval">/ {{ subscription.plan.billingInterval === 2 ? 'year' : 'month' }}</span>
            </div>
          </div>

          <div class="details-grid">
            <div class="detail-item">
              <span class="label">Current Period Start</span>
              <span class="value">{{ subscription.currentPeriodStartUtc | date:'mediumDate' }}</span>
            </div>
            <div class="detail-item">
              <span class="label">Next Renewal / Expiration</span>
              <span class="value">{{ subscription.currentPeriodEndUtc | date:'mediumDate' }}</span>
            </div>
            <div class="detail-item">
              <span class="label">Stripe Subscription ID</span>
              <span class="value code">{{ subscription.stripeSubscriptionId || 'N/A' }}</span>
            </div>
            <div class="detail-item">
              <span class="label">Auto-Renewal</span>
              <span class="value">{{ subscription.cancelAtPeriodEnd ? 'Disabled (Cancels at end)' : 'Active' }}</span>
            </div>
          </div>

          <div class="plan-features">
            <h4>Included Plan Features:</h4>
            <ul>
              <li *ngFor="let feat of subscription.plan.features">✓ {{ feat }}</li>
            </ul>
          </div>

          <div class="card-actions">
            <button class="btn btn-primary" (click)="openChangePlanModal()">Change / Upgrade Plan</button>
            <button *ngIf="!subscription.cancelAtPeriodEnd" class="btn btn-danger" (click)="onCancelSubscription()">Cancel Subscription</button>
          </div>
        </div>

        <!-- Developer Simulation Card -->
        <div class="card sim-card">
          <h4>🧪 Developer & Interview Demo Tools</h4>
          <p>Simulate an automatic renewal webhook cycle (triggers real webhook parsing, invoice generation, and payment history):</p>
          <button class="btn btn-secondary btn-sm" [disabled]="isSimulating" (click)="onSimulateRenewal()">
            {{ isSimulating ? 'Processing Webhook...' : 'Simulate Successful Renewal Webhook' }}
          </button>
        </div>
      </div>

      <!-- Change Plan Modal -->
      <div class="modal-backdrop" *ngIf="showPlanModal">
        <div class="modal-card card">
          <div class="modal-header">
            <h3>Switch Subscription Plan</h3>
            <button class="modal-close" (click)="showPlanModal = false">&times;</button>
          </div>
          <p class="modal-desc">Select a target tier. Upgrades apply immediately with proration credit.</p>

          <div class="modal-plans-list">
            <div *ngFor="let p of availablePlans"
                 class="plan-choice"
                 [class.selected]="selectedPlanId === p.id"
                 (click)="selectedPlanId = p.id">
              <div class="plan-choice-info">
                <strong>{{ p.name }}</strong>
                <span>\${{ p.price | number:'1.2-2' }} / {{ p.billingInterval === 2 ? 'year' : 'month' }}</span>
              </div>
              <span *ngIf="p.id === subscription?.planId" class="badge badge-neutral">Current</span>
            </div>
          </div>

          <div class="modal-actions">
            <button class="btn btn-secondary" (click)="showPlanModal = false">Cancel</button>
            <button class="btn btn-primary"
                    [disabled]="!selectedPlanId || selectedPlanId === subscription?.planId || isModifying"
                    (click)="confirmPlanChange()">
              {{ isModifying ? 'Updating...' : 'Confirm Plan Switch' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      margin: 2rem 0;
      h1 { font-size: 1.875rem; font-weight: 700; color: #0f172a; margin-bottom: 0.25rem; }
      .subtitle { color: #64748b; font-size: 0.9375rem; }
    }
    .empty-state {
      text-align: center;
      padding: 3rem 1.5rem;
      .empty-icon { font-size: 3rem; margin-bottom: 1rem; }
      h3 { font-size: 1.25rem; font-weight: 700; margin-bottom: 0.5rem; }
      p { color: #64748b; margin-bottom: 1.5rem; }
    }
    .alert {
      padding: 1rem 1.25rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      font-size: 0.875rem;
      &-warning { background: #fffbeb; border: 1px solid #fef3c7; color: #92400e; }
    }
    .sub-card {
      padding: 2rem;
    }
    .sub-card-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
      h2 { font-size: 1.5rem; font-weight: 700; color: #0f172a; margin-top: 0.5rem; }
      .price-tag {
        text-align: right;
        .amount { font-size: 2rem; font-weight: 800; color: #0f172a; }
        .interval { color: #64748b; font-size: 0.875rem; }
      }
    }
    .details-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.25rem;
      padding: 1.25rem;
      background: #f8fafc;
      border-radius: 8px;
      margin-bottom: 1.5rem;
      .detail-item {
        .label { display: block; font-size: 0.75rem; color: #64748b; margin-bottom: 0.25rem; }
        .value { font-weight: 600; color: #1e293b; font-size: 0.9375rem; }
        .code { font-family: monospace; font-size: 0.8rem; }
      }
    }
    .plan-features {
      margin-bottom: 2rem;
      h4 { font-size: 0.875rem; font-weight: 600; margin-bottom: 0.75rem; color: #475569; }
      ul { list-style: none; display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 0.5rem; }
      li { font-size: 0.875rem; color: #334155; }
    }
    .card-actions {
      display: flex;
      gap: 1rem;
    }
    .sim-card {
      margin-top: 1.5rem;
      background: #f8fafc;
      border: 1px dashed #cbd5e1;
      h4 { font-size: 1rem; margin-bottom: 0.5rem; color: #0f172a; }
      p { font-size: 0.8125rem; color: #64748b; margin-bottom: 1rem; }
    }
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; width: 100vw; height: 100vh;
      background: rgba(15, 23, 42, 0.5);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }
    .modal-card {
      width: 90%;
      max-width: 480px;
      background: #ffffff;
      padding: 1.75rem;
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
      h3 { font-size: 1.25rem; font-weight: 700; color: #0f172a; }
      .modal-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #64748b; }
    }
    .modal-desc { font-size: 0.875rem; color: #64748b; margin-bottom: 1.25rem; }
    .modal-plans-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      margin-bottom: 1.5rem;
    }
    .plan-choice {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.875rem 1rem;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.15s ease;
      &:hover { border-color: #94a3b8; background: #f8fafc; }
      &.selected { border-color: #2563eb; background: #eff6ff; }
      .plan-choice-info {
        display: flex;
        flex-direction: column;
        strong { color: #0f172a; font-size: 0.9375rem; }
        span { color: #64748b; font-size: 0.8125rem; }
      }
    }
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }
  `]
})
export class MySubscriptionComponent implements OnInit {
  subscription: Subscription | null = null;
  availablePlans: SubscriptionPlan[] = [];
  isLoading = true;
  isModifying = false;
  isSimulating = false;
  showPlanModal = false;
  selectedPlanId: number | null = null;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private subscriptionService: SubscriptionService,
    private planService: PlanService,
    private simulationService: SimulationService,
    private authService: AuthService,
    private notification: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadSubscription();
  }

  loadSubscription(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.subscriptionService.getMySubscription().subscribe({
      next: sub => {
        this.subscription = sub;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openChangePlanModal(): void {
    this.planService.getActivePlans().subscribe({
      next: plans => {
        this.availablePlans = plans;
        this.selectedPlanId = this.subscription?.planId ?? null;
        this.showPlanModal = true;
        this.cdr.markForCheck();
      }
    });
  }

  confirmPlanChange(): void {
    if (!this.selectedPlanId) return;

    this.isModifying = true;
    this.subscriptionService.upgradeOrDowngrade(this.selectedPlanId).subscribe({
      next: updated => {
        this.isModifying = false;
        this.showPlanModal = false;
        this.subscription = updated;
        this.notification.success(`Switched plan to ${updated.plan.name} successfully!`);
        this.cdr.markForCheck();
      },
      error: () => {
        this.isModifying = false;
        this.cdr.markForCheck();
      }
    });
  }

  onCancelSubscription(): void {
    if (!confirm('Are you sure you want to cancel your subscription? It will remain active until the end of the current billing cycle.')) {
      return;
    }

    this.subscriptionService.cancelSubscription(false).subscribe({
      next: canceled => {
        this.subscription = canceled;
        this.notification.warning('Subscription scheduled for cancellation at the end of the current billing period.');
      }
    });
  }

  onReactivate(): void {
    this.subscriptionService.reactivateSubscription().subscribe({
      next: reactivated => {
        this.subscription = reactivated;
        this.notification.success('Subscription reactivated! Auto-renewal is now active.');
      }
    });
  }

  onSimulateRenewal(): void {
    const user = this.authService.currentUser();
    if (!user || !this.subscription) return;

    this.isSimulating = true;
    this.simulationService.simulatePayment(user.id, this.subscription.planId).subscribe({
      next: () => {
        this.isSimulating = false;
        this.notification.success('Simulated renewal webhook processed! Invoice generated and subscription updated.');
        this.loadSubscription();
      },
      error: () => {
        this.isSimulating = false;
      }
    });
  }

  getStatusBadgeClass(status: number): string {
    switch (status) {
      case 2: return 'badge-active';
      case 3: return 'badge-pastdue';
      case 4: return 'badge-canceled';
      default: return 'badge-neutral';
    }
  }

  getStatusText(status: number): string {
    switch (status) {
      case 2: return 'ACTIVE';
      case 3: return 'PAST DUE';
      case 4: return 'CANCELED';
      default: return 'INCOMPLETE';
    }
  }
}
