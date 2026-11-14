export interface UserProfile {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  stripeCustomerId?: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface SubscriptionPlanPrice {
  id: number;
  subscriptionPlanId: number;
  billingInterval: number; // 1: Monthly, 2: Yearly
  amount: number;
  currency: string;
  stripePriceId?: string;
  isActive: boolean;
}

export interface SubscriptionPlanFeature {
  id: number;
  subscriptionPlanId: number;
  featureName: string;
  featureValue: string;
  displayOrder: number;
}

export interface SubscriptionPlan {
  id: number;
  name: string;
  description: string;
  price: number;
  currency: string;
  billingInterval: number; // 1: Monthly, 2: Yearly
  stripePriceId?: string;
  stripeProductId?: string;
  features: string[];
  isActive: boolean;
  isPopular?: boolean;
  prices?: SubscriptionPlanPrice[];
  planFeatures?: SubscriptionPlanFeature[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface Subscription {
  id: number;
  userId: number;
  userEmail: string;
  planId: number;
  plan: SubscriptionPlan;
  subscriptionPlanPriceId?: number;
  stripeSubscriptionId?: string;
  status: number; // 1: Incomplete, 2: Active, 3: PastDue, 4: Canceled
  currentPeriodStartUtc: string;
  currentPeriodEndUtc: string;
  cancelAtPeriodEnd: boolean;
  canceledAtUtc?: string;
  createdAtUtc: string;
}

export interface Payment {
  id: number;
  userId: number;
  userEmail: string;
  subscriptionId?: number;
  invoiceId?: number;
  invoiceNumber?: string;
  stripePaymentIntentId?: string;
  amount: number;
  currency: string;
  paymentMethodSummary: string;
  status: number; // 1: Pending, 2: Succeeded, 3: Failed, 4: Refunded
  failureMessage?: string;
  paymentDateUtc: string;
}

export interface Invoice {
  id: number;
  userId: number;
  userEmail: string;
  subscriptionId?: number;
  planName?: string;
  invoiceNumber: string;
  stripeInvoiceId?: string;
  billingPeriod: string;
  invoiceDateUtc: string;
  dueDateUtc: string;
  subtotal: number;
  tax: number;
  total: number;
  currency: string;
  status: number; // 1: Draft, 2: Open, 3: Paid, 4: Void
  hasPdf: boolean;
  createdAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface DashboardMetrics {
  totalCustomers: number;
  activeSubscriptions: number;
  canceledSubscriptions: number;
  monthlyRecurringRevenue: number;
  totalRevenue: number;
  successfulPaymentsCount: number;
  failedPaymentsCount: number;
}

export interface RevenueChartPoint {
  period: string;
  revenue: number;
  subscriptionsCount: number;
}

export interface RecentActivity {
  type: string;
  description: string;
  amount?: number;
  status: string;
  timestampUtc: string;
}

export interface UserManagement {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  stripeCustomerId?: string;
  isActive: boolean;
  roles: string[];
  createdAtUtc: string;
}
