import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PlanService, SubscriptionService } from '../../core/services/api.services';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { SubscriptionPlan, SubscriptionPlanPrice, SubscriptionPlanFeature } from '../../core/models/models';

@Component({
  selector: 'app-plans-catalog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <div class="catalog-header">
        <h1>Transparent, Predictable Pricing</h1>
        <p class="subtitle">Choose the plan that powers your team. Upgrade, downgrade, or cancel anytime.</p>

        <!-- Monthly / Yearly Billing Toggle -->
        <div class="billing-toggle-container">
          <div class="billing-toggle">
            <button
              class="toggle-btn"
              [class.active]="selectedInterval === 1"
              (click)="selectedInterval = 1">
              Monthly Billing
            </button>
            <button
              class="toggle-btn"
              [class.active]="selectedInterval === 2"
              (click)="selectedInterval = 2">
              Annual Billing
              <span class="save-badge">SAVE ~17%</span>
            </button>
          </div>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading subscription plans...</p>
      </div>

      <div *ngIf="!isLoading && plans.length === 0" class="empty-state card">
        <p>No active plans available right now. Please check back later.</p>
      </div>

      <div class="pricing-grid" *ngIf="!isLoading && plans.length > 0">
        <div
          *ngFor="let plan of plans"
          class="pricing-card card"
          [class.featured]="plan.isPopular">

          <!-- Popular badge driven strictly by database isPopular property -->
          <div *ngIf="plan.isPopular" class="popular-tag">MOST POPULAR</div>

          <div class="plan-header">
            <h3 class="plan-name">{{ plan.name }}</h3>
            <p class="plan-desc">{{ plan.description }}</p>
          </div>

          <!-- Dynamic Price based on selected billing interval -->
          <div class="plan-price">
            <span class="currency">{{ getCurrencySymbol(plan) }}</span>
            <span class="amount">{{ getDisplayAmount(plan) | number:'1.0-2' }}</span>
            <span class="interval">/ {{ selectedInterval === 2 ? 'year' : 'month' }}</span>
          </div>

          <!-- Highlight Limits (Users, Projects, Storage) if structured features exist -->
          <div class="limits-bar" *ngIf="getLimitFeatures(plan).length > 0">
            <div *ngFor="let lim of getLimitFeatures(plan)" class="limit-badge">
              <span class="limit-label">{{ formatLimitLabel(lim.featureName) }}:</span>
              <span class="limit-val">{{ lim.featureValue }}</span>
            </div>
          </div>

          <!-- Feature Checklist -->
          <ul class="features-list">
            <li *ngFor="let feat of getChecklistFeatures(plan)">
              <span class="check-icon">✓</span>
              <span>{{ feat }}</span>
            </li>
          </ul>

          <div class="card-action">
            <button
              class="btn btn-primary btn-block"
              [disabled]="isSubscribing"
              (click)="onSubscribe(plan)">
              {{ isSubscribing ? 'Processing...' : 'Subscribe Now' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .catalog-header {
      text-align: center;
      margin: 2.5rem 0 2.5rem;
      h1 { font-size: 2.35rem; font-weight: 800; color: #0f172a; margin-bottom: 0.5rem; }
      .subtitle { color: #64748b; font-size: 1.1rem; max-width: 600px; margin: 0 auto; }
    }
    .billing-toggle-container {
      display: flex;
      justify-content: center;
      margin-top: 2rem;
    }
    .billing-toggle {
      display: inline-flex;
      background: #e2e8f0;
      padding: 0.35rem;
      border-radius: 9999px;
      gap: 0.25rem;
    }
    .toggle-btn {
      border: none;
      background: transparent;
      padding: 0.55rem 1.4rem;
      border-radius: 9999px;
      font-size: 0.9rem;
      font-weight: 600;
      color: #475569;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      transition: all 0.2s ease;
      &.active {
        background: #ffffff;
        color: #0f172a;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
      }
    }
    .save-badge {
      background: #dcfce7;
      color: #166534;
      font-size: 0.7rem;
      font-weight: 700;
      padding: 0.15rem 0.45rem;
      border-radius: 9999px;
    }
    .pricing-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 2rem;
      align-items: stretch;
      margin-bottom: 3rem;
    }
    .pricing-card {
      display: flex;
      flex-direction: column;
      position: relative;
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 14px;
      padding: 2.25rem;
      transition: all 0.2s ease;
      &.featured {
        border-color: #2563eb;
        border-width: 2px;
        box-shadow: 0 12px 28px -5px rgba(37, 99, 235, 0.18);
        transform: translateY(-4px);
      }
    }
    .popular-tag {
      position: absolute;
      top: -13px;
      left: 50%;
      transform: translateX(-50%);
      background: #2563eb;
      color: #ffffff;
      font-size: 0.72rem;
      font-weight: 800;
      padding: 0.3rem 0.9rem;
      border-radius: 9999px;
      letter-spacing: 0.06em;
      box-shadow: 0 4px 6px rgba(37, 99, 235, 0.2);
    }
    .plan-header {
      margin-bottom: 1.25rem;
      .plan-name { font-size: 1.35rem; font-weight: 700; color: #0f172a; margin-bottom: 0.4rem; }
      .plan-desc { color: #64748b; font-size: 0.88rem; line-height: 1.4; min-height: 40px; }
    }
    .plan-price {
      margin-bottom: 1.5rem;
      display: flex;
      align-items: baseline;
      .currency { font-size: 1.75rem; font-weight: 700; color: #0f172a; margin-right: 0.15rem; }
      .amount { font-size: 2.75rem; font-weight: 800; color: #0f172a; line-height: 1; }
      .interval { color: #64748b; font-size: 0.9rem; margin-left: 0.5rem; font-weight: 500; }
    }
    .limits-bar {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      background: #f8fafc;
      padding: 0.85rem 1rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }
    .limit-badge {
      display: flex;
      justify-content: space-between;
      font-size: 0.825rem;
      .limit-label { color: #64748b; font-weight: 500; }
      .limit-val { color: #0f172a; font-weight: 700; }
    }
    .features-list {
      list-style: none;
      margin-bottom: 2rem;
      flex-grow: 1;
      padding: 0;
      li {
        display: flex;
        align-items: flex-start;
        gap: 0.75rem;
        margin-bottom: 0.75rem;
        font-size: 0.875rem;
        color: #334155;
        line-height: 1.35;
        .check-icon { color: #10b981; font-weight: bold; font-size: 0.95rem; }
      }
    }
    .btn-block { width: 100%; padding: 0.75rem; font-size: 0.95rem; font-weight: 600; border-radius: 8px; }
    .loading-state, .empty-state { text-align: center; padding: 3rem; color: #64748b; }
  `]
})
export class PlansCatalogComponent implements OnInit {
  plans: SubscriptionPlan[] = [];
  selectedInterval: 1 | 2 = 1; // 1: Monthly, 2: Yearly
  isLoading = true;
  isSubscribing = false;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private planService: PlanService,
    private subscriptionService: SubscriptionService,
    private authService: AuthService,
    private notification: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.planService.getActivePlans().subscribe({
      next: data => {
        this.plans = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  getDisplayAmount(plan: SubscriptionPlan): number {
    const price = this.getSelectedPrice(plan);
    if (price) {
      return price.amount;
    }
    // Fallback: if annual selected and no yearly price record, calculate 10x monthly
    if (this.selectedInterval === 2 && plan.billingInterval === 1) {
      return plan.price * 10;
    }
    return plan.price;
  }

  getSelectedPrice(plan: SubscriptionPlan): SubscriptionPlanPrice | undefined {
    if (plan.prices && plan.prices.length > 0) {
      return plan.prices.find(p => p.billingInterval === this.selectedInterval && p.isActive);
    }
    return undefined;
  }

  getCurrencySymbol(plan: SubscriptionPlan): string {
    const price = this.getSelectedPrice(plan);
    const curr = (price?.currency || plan.currency || '').toUpperCase();
    if (curr === 'INR') return '₹';
    if (curr === 'EUR') return '€';
    if (curr === 'GBP') return '£';
    return '$';
  }

  getLimitFeatures(plan: SubscriptionPlan): SubscriptionPlanFeature[] {
    if (!plan.planFeatures) return [];
    return plan.planFeatures.filter(f =>
      ['MaxUsers', 'MaxProjects', 'Storage'].includes(f.featureName)
    );
  }

  formatLimitLabel(key: string): string {
    switch (key) {
      case 'MaxUsers': return '👤 Maximum Users';
      case 'MaxProjects': return '📁 Maximum Projects';
      case 'Storage': return '💾 Cloud Storage';
      default: return key;
    }
  }

  getChecklistFeatures(plan: SubscriptionPlan): string[] {
    if (plan.planFeatures && plan.planFeatures.length > 0) {
      return plan.planFeatures
        .filter(f => !['MaxUsers', 'MaxProjects', 'Storage'].includes(f.featureName))
        .map(f => f.featureValue === 'Included' || f.featureValue === 'true' ? f.featureName : `${f.featureName} (${f.featureValue})`);
    }
    return plan.features || [];
  }

  onSubscribe(plan: SubscriptionPlan): void {
    if (!this.authService.isAuthenticated()) {
      this.notification.warning('Please log in or register before subscribing.');
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: '/plans' } });
      return;
    }

    this.isSubscribing = true;
    const origin = window.location.origin;
    const successUrl = `${origin}/customer/checkout-status?status=success`;
    const cancelUrl = `${origin}/plans`;
    const selectedPrice = this.getSelectedPrice(plan);

    this.subscriptionService.createCheckoutSession(
      plan.id,
      successUrl,
      cancelUrl,
      this.selectedInterval,
      selectedPrice?.id
    ).subscribe({
      next: session => {
        this.isSubscribing = false;
        if (session.checkoutUrl.startsWith('http')) {
          window.location.href = session.checkoutUrl;
        } else {
          this.router.navigateByUrl(session.checkoutUrl);
        }
      },
      error: () => {
        this.isSubscribing = false;
      }
    });
  }
}
