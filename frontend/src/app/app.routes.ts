import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './core/guards/guards';

export const routes: Routes = [
  // Public routes
  {
    path: 'plans',
    loadComponent: () => import('./pages/customer/plans-catalog.component').then(m => m.PlansCatalogComponent)
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./pages/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./pages/auth/register.component').then(m => m.RegisterComponent)
  },

  // Customer routes (Protected by authGuard)
  {
    path: 'customer/dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/customer/dashboard.component').then(m => m.CustomerDashboardComponent)
  },
  {
    path: 'customer/my-subscription',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/customer/my-subscription.component').then(m => m.MySubscriptionComponent)
  },
  {
    path: 'customer/payments',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/customer/payment-history.component').then(m => m.PaymentHistoryComponent)
  },
  {
    path: 'customer/invoices',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/customer/invoice-history.component').then(m => m.InvoiceHistoryComponent)
  },
  {
    path: 'checkout/status',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/customer/checkout-status.component').then(m => m.CheckoutStatusComponent)
  },

  // Aliases for convenience
  { path: 'dashboard', redirectTo: 'customer/dashboard', pathMatch: 'full' },
  { path: 'subscription/my', redirectTo: 'customer/my-subscription', pathMatch: 'full' },
  { path: 'payments', redirectTo: 'customer/payments', pathMatch: 'full' },
  { path: 'invoices', redirectTo: 'customer/invoices', pathMatch: 'full' },

  // Admin routes (Protected by authGuard + adminGuard)
  {
    path: 'admin/dashboard',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/dashboard.component').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'admin/plans',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/plans-management.component').then(m => m.PlansManagementComponent)
  },
  {
    path: 'admin/subscriptions',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/subscriptions-management.component').then(m => m.SubscriptionsManagementComponent)
  },
  {
    path: 'admin/payments',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/payments-management.component').then(m => m.PaymentsManagementComponent)
  },
  {
    path: 'admin/invoices',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/invoices-management.component').then(m => m.InvoicesManagementComponent)
  },
  {
    path: 'admin/users',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/users-management.component').then(m => m.UsersManagementComponent)
  },

  // Default redirect
  { path: '', redirectTo: 'plans', pathMatch: 'full' },
  { path: '**', redirectTo: 'plans' }
];
