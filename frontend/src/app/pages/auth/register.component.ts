import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card card">
        <div class="auth-header">
          <a routerLink="/" class="auth-logo-link" title="SubBill Home">
            <img src="images/logo.svg" alt="SubBill Logo" class="auth-logo" />
          </a>
          <h2>Create Your Account</h2>
          <p class="text-muted">Start managing and browsing subscription plans</p>
        </div>

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
          <div class="form-row">
            <div class="form-group">
              <label for="firstName">First Name</label>
              <input id="firstName" type="text" formControlName="firstName"
                     [class.is-invalid]="registerForm.get('firstName')?.touched && registerForm.get('firstName')?.invalid"
                     placeholder="John" />
            </div>
            <div class="form-group">
              <label for="lastName">Last Name</label>
              <input id="lastName" type="text" formControlName="lastName"
                     [class.is-invalid]="registerForm.get('lastName')?.touched && registerForm.get('lastName')?.invalid"
                     placeholder="Doe" />
            </div>
          </div>

          <div class="form-group">
            <label for="email">Email Address</label>
            <input id="email" type="email" formControlName="email"
                   [class.is-invalid]="registerForm.get('email')?.touched && registerForm.get('email')?.invalid"
                   placeholder="john@example.com" />
            <div class="invalid-feedback" *ngIf="registerForm.get('email')?.touched && registerForm.get('email')?.invalid">
              Please enter a valid email.
            </div>
          </div>

          <div class="form-group">
            <label for="password">Password (min 8 chars, 1 uppercase, 1 number, 1 special)</label>
            <input id="password" type="password" formControlName="password"
                   [class.is-invalid]="registerForm.get('password')?.touched && registerForm.get('password')?.invalid"
                   placeholder="••••••••" />
            <div class="invalid-feedback" *ngIf="registerForm.get('password')?.touched && registerForm.get('password')?.invalid">
              Password must be 8+ characters and contain uppercase, number, and special character.
            </div>
          </div>

          <button type="submit" class="btn btn-primary btn-block" [disabled]="registerForm.invalid || isLoading">
            {{ isLoading ? 'Creating Account...' : 'Get Started' }}
          </button>
        </form>

        <div class="auth-footer">
          Already have an account? <a routerLink="/auth/login">Sign In</a>
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
      max-width: 480px;
    }
    .auth-header {
      text-align: center;
      margin-bottom: 1.5rem;
      .auth-logo-link { display: inline-block; margin-bottom: 0.75rem; text-decoration: none; }
      .auth-logo { height: 64px; width: auto; margin: 0 auto; display: block; }
      h2 { font-size: 1.5rem; font-weight: 700; color: #0f172a; margin-bottom: 0.25rem; }
      .text-muted { color: #64748b; font-size: 0.875rem; }
    }
    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }
    .btn-block { width: 100%; margin-top: 0.5rem; }
    .auth-footer {
      text-align: center;
      margin-top: 1.25rem;
      font-size: 0.875rem;
      color: #64748b;
    }
  `]
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private notification: NotificationService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).+$/)
      ]]
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading = true;
    this.authService.register(this.registerForm.value).subscribe({
      next: res => {
        this.isLoading = false;
        this.notification.success(`Account created successfully! Welcome, ${res.user.firstName}!`);
        this.router.navigate(['/customer/dashboard']);
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}
