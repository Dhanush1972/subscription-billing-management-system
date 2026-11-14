import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="navbar">
      <div class="nav-container">
        <a routerLink="/" class="brand">
          <span class="brand-icon">⚡</span>
          <span class="brand-name">SubBill</span>
        </a>

        <div class="nav-links">
          <!-- Public / Common Links -->
          <a routerLink="/plans" routerLinkActive="active" class="nav-item">Plans</a>

          <!-- Customer Links -->
          <ng-container *ngIf="authService.isAuthenticated() && !authService.isAdmin()">
            <a routerLink="/customer/dashboard" routerLinkActive="active" class="nav-item">Dashboard</a>
            <a routerLink="/customer/my-subscription" routerLinkActive="active" class="nav-item">My Subscription</a>
            <a routerLink="/customer/payments" routerLinkActive="active" class="nav-item">Payments</a>
            <a routerLink="/customer/invoices" routerLinkActive="active" class="nav-item">Invoices</a>
          </ng-container>

          <!-- Admin Links -->
          <ng-container *ngIf="authService.isAdmin()">
            <a routerLink="/admin/dashboard" routerLinkActive="active" class="nav-item">Admin Dashboard</a>
            <a routerLink="/admin/plans" routerLinkActive="active" class="nav-item">Manage Plans</a>
            <a routerLink="/admin/subscriptions" routerLinkActive="active" class="nav-item">Subscriptions</a>
            <a routerLink="/admin/payments" routerLinkActive="active" class="nav-item">Payments</a>
            <a routerLink="/admin/invoices" routerLinkActive="active" class="nav-item">Invoices</a>
            <a routerLink="/admin/users" routerLinkActive="active" class="nav-item">Users</a>
          </ng-container>
        </div>

        <div class="nav-actions">
          <ng-container *ngIf="authService.isAuthenticated(); else unauthBlock">
            <div class="user-pill">
              <span class="user-badge" [class.admin-badge]="authService.isAdmin()">
                {{ authService.isAdmin() ? 'ADMIN' : 'CUSTOMER' }}
              </span>
              <span class="user-email">{{ authService.currentUser()?.email }}</span>
            </div>
            <button class="btn btn-secondary btn-sm" (click)="onLogout()">Log Out</button>
          </ng-container>
          <ng-template #unauthBlock>
            <a routerLink="/auth/login" class="btn btn-secondary btn-sm">Log In</a>
            <a routerLink="/auth/register" class="btn btn-primary btn-sm">Get Started</a>
          </ng-template>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      background: #ffffff;
      border-bottom: 1px solid #e2e8f0;
      position: sticky;
      top: 0;
      z-index: 100;
      box-shadow: 0 1px 3px rgba(0,0,0,0.02);
    }
    .nav-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0.75rem 1rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1.5rem;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: 700;
      font-size: 1.25rem;
      color: #0f172a;
      text-decoration: none;
      .brand-icon { font-size: 1.4rem; }
    }
    .nav-links {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }
    .nav-item {
      font-size: 0.875rem;
      color: #64748b;
      font-weight: 500;
      padding: 0.5rem 0.75rem;
      border-radius: 6px;
      text-decoration: none;
      transition: all 0.15s ease;
      &:hover { color: #0f172a; background: #f8fafc; }
      &.active { color: #2563eb; background: #eff6ff; font-weight: 600; }
    }
    .nav-actions {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .user-pill {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      padding: 0.25rem 0.625rem;
      border-radius: 9999px;
      font-size: 0.8rem;
    }
    .user-badge {
      font-size: 0.65rem;
      font-weight: 700;
      padding: 0.15rem 0.4rem;
      background: #e2e8f0;
      color: #475569;
      border-radius: 4px;
      &.admin-badge {
        background: #fee2e2;
        color: #ef4444;
      }
    }
    .user-email {
      color: #334155;
      font-weight: 500;
    }
  `]
})
export class NavbarComponent {
  constructor(public authService: AuthService, private router: Router) {}

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
