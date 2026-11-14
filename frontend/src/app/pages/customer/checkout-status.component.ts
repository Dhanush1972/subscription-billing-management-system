import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';

@Component({
  selector: 'app-checkout-status',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container">
      <div class="card status-card">
        <div *ngIf="isSuccess" class="state-content">
          <div class="status-icon success">✓</div>
          <h2>Payment Checkout Initiated!</h2>
          <p class="summary-text">
            Thank you! Your payment checkout has been processed with Stripe.
          </p>

          <div class="alert-box">
            <div class="alert-title">🔒 Production Architecture Note:</div>
            <p>
              In accordance with Zero-Trust Security standards, your subscription is activated strictly upon 
              cryptographic verification of the incoming <strong>invoice.paid</strong> webhook from Stripe. 
              Your account records are synchronizing in real time.
            </p>
          </div>

          <div class="action-buttons">
            <a routerLink="/subscription/my" class="btn btn-primary">Go to My Subscription</a>
            <a routerLink="/invoices" class="btn btn-outline">View Invoices</a>
          </div>
        </div>

        <div *ngIf="!isSuccess" class="state-content">
          <div class="status-icon canceled">✕</div>
          <h2>Checkout Cancelled</h2>
          <p class="summary-text">
            Your Stripe checkout session was cancelled. No charges were made to your account.
          </p>

          <div class="action-buttons">
            <a routerLink="/plans" class="btn btn-primary">Browse Available Plans</a>
            <a routerLink="/dashboard" class="btn btn-outline">Return to Dashboard</a>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .container {
      max-width: 680px;
      margin: 4rem auto;
      padding: 0 1rem;
    }
    .status-card {
      text-align: center;
      padding: 3rem 2rem;
    }
    .status-icon {
      width: 72px;
      height: 72px;
      line-height: 72px;
      border-radius: 50%;
      font-size: 2.25rem;
      margin: 0 auto 1.5rem;
      font-weight: bold;
    }
    .status-icon.success {
      background: #dcfce7;
      color: #15803d;
    }
    .status-icon.canceled {
      background: #fee2e2;
      color: #b91c1c;
    }
    h2 {
      font-size: 1.85rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.75rem;
    }
    .summary-text {
      color: #475569;
      font-size: 1.05rem;
      margin-bottom: 2rem;
    }
    .alert-box {
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-left: 4px solid #3b82f6;
      border-radius: 6px;
      padding: 1.25rem;
      text-align: left;
      margin-bottom: 2rem;
    }
    .alert-title {
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 0.35rem;
      font-size: 0.95rem;
    }
    .alert-box p {
      color: #475569;
      font-size: 0.9rem;
      line-height: 1.5;
      margin: 0;
    }
    .action-buttons {
      display: flex;
      justify-content: center;
      gap: 1rem;
      flex-wrap: wrap;
    }
  `]
})
export class CheckoutStatusComponent implements OnInit {
  isSuccess = true;
  sessionId?: string;

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.isSuccess = params['status'] !== 'cancel';
      this.sessionId = params['session_id'];
    });
  }
}
