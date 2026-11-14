import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card card">
        <div class="auth-header">
          <h2>Welcome Back</h2>
          <p class="text-muted">Sign in to your subscription billing account</p>
        </div>

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="email">Email Address</label>
            <input id="email" type="email" formControlName="email"
                   [class.is-invalid]="loginForm.get('email')?.touched && loginForm.get('email')?.invalid"
                   placeholder="name@company.com" />
            <div class="invalid-feedback" *ngIf="loginForm.get('email')?.touched && loginForm.get('email')?.invalid">
              Please enter a valid email address.
            </div>
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input id="password" type="password" formControlName="password"
                   [class.is-invalid]="loginForm.get('password')?.touched && loginForm.get('password')?.invalid"
                   placeholder="••••••••" />
            <div class="invalid-feedback" *ngIf="loginForm.get('password')?.touched && loginForm.get('password')?.invalid">
              Password is required.
            </div>
          </div>

          <button type="submit" class="btn btn-primary btn-block" [disabled]="loginForm.invalid || isLoading">
            {{ isLoading ? 'Signing In...' : 'Sign In' }}
          </button>
        </form>

        <div class="demo-logins">
          <p class="demo-title">Quick Demo Logins:</p>
          <div class="demo-buttons">
            <button type="button" class="btn btn-secondary btn-sm" (click)="fillAdmin()">Admin Demo</button>
            <button type="button" class="btn btn-secondary btn-sm" (click)="fillCustomer()">Customer Demo</button>
          </div>
        </div>

        <div class="auth-footer">
          Don't have an account? <a routerLink="/auth/register">Create Account</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: calc(100vh - 140px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
    }
    .auth-card {
      width: 100%;
      max-width: 420px;
    }
    .auth-header {
      text-align: center;
      margin-bottom: 1.5rem;
      h2 { font-size: 1.5rem; font-weight: 700; color: #0f172a; margin-bottom: 0.25rem; }
      .text-muted { color: #64748b; font-size: 0.875rem; }
    }
    .btn-block { width: 100%; margin-top: 0.5rem; }
    .demo-logins {
      margin-top: 1.5rem;
      padding-top: 1.25rem;
      border-top: 1px dashed #e2e8f0;
      text-align: center;
      .demo-title { font-size: 0.75rem; color: #64748b; margin-bottom: 0.5rem; }
      .demo-buttons { display: flex; gap: 0.5rem; justify-content: center; }
    }
    .auth-footer {
      text-align: center;
      margin-top: 1.25rem;
      font-size: 0.875rem;
      color: #64748b;
    }
  `]
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private notification: NotificationService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  fillAdmin(): void {
    this.loginForm.setValue({
      email: 'admin@subscriptionbilling.com',
      password: 'Admin@123456!'
    });
  }

  fillCustomer(): void {
    this.loginForm.setValue({
      email: 'customer@subscriptionbilling.com',
      password: 'Customer@123456!'
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    this.authService.login(this.loginForm.value).subscribe({
      next: res => {
        this.isLoading = false;
        this.notification.success(`Welcome back, ${res.user.firstName}!`);
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        if (returnUrl) {
          this.router.navigateByUrl(returnUrl);
        } else if (res.user.roles.includes('Admin')) {
          this.router.navigate(['/admin/dashboard']);
        } else {
          this.router.navigate(['/customer/dashboard']);
        }
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}
