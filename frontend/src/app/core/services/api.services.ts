import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SubscriptionPlan,
  Subscription,
  Payment,
  Invoice,
  PagedResult,
  DashboardMetrics,
  RevenueChartPoint,
  RecentActivity,
  UserManagement
} from '../models/models';

const API_BASE = '/api';

@Injectable({ providedIn: 'root' })
export class PlanService {
  constructor(private http: HttpClient) {}

  getActivePlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${API_BASE}/subscription-plans`);
  }

  getAllPlansForAdmin(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${API_BASE}/plans/admin`);
  }

  getPlanById(id: number): Observable<SubscriptionPlan> {
    return this.http.get<SubscriptionPlan>(`${API_BASE}/plans/${id}`);
  }

  createPlan(plan: { name: string; description: string; price: number; currency: string; billingInterval: number; features: string[] }): Observable<SubscriptionPlan> {
    return this.http.post<SubscriptionPlan>(`${API_BASE}/plans`, plan);
  }

  updatePlan(id: number, plan: { name: string; description: string; isActive: boolean; features: string[] }): Observable<SubscriptionPlan> {
    return this.http.put<SubscriptionPlan>(`${API_BASE}/plans/${id}`, plan);
  }

  toggleStatus(id: number, isActive: boolean): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${API_BASE}/plans/${id}/status`, isActive);
  }

  deletePlan(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${API_BASE}/plans/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  constructor(private http: HttpClient) {}

  getMySubscription(): Observable<Subscription | null> {
    return this.http.get<Subscription | null>(`${API_BASE}/subscriptions/my`);
  }

  getAllSubscriptions(pageNumber = 1, pageSize = 10, status?: number): Observable<PagedResult<Subscription>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status !== undefined) params = params.set('status', status);
    return this.http.get<PagedResult<Subscription>>(`${API_BASE}/subscriptions/admin`, { params });
  }

  createCheckoutSession(planId: number, successUrl: string, cancelUrl: string, billingInterval?: number, priceId?: number): Observable<{ sessionId: string; checkoutUrl: string }> {
    return this.http.post<{ sessionId: string; checkoutUrl: string }>(`${API_BASE}/subscriptions/checkout-session`, {
      planId,
      successUrl,
      cancelUrl,
      billingInterval,
      priceId
    });
  }

  upgradeOrDowngrade(newPlanId: number): Observable<Subscription> {
    return this.http.post<Subscription>(`${API_BASE}/subscriptions/upgrade`, { newPlanId });
  }

  cancelSubscription(immediately = false): Observable<Subscription> {
    return this.http.post<Subscription>(`${API_BASE}/subscriptions/cancel`, { immediately });
  }

  reactivateSubscription(): Observable<Subscription> {
    return this.http.post<Subscription>(`${API_BASE}/subscriptions/reactivate`, {});
  }
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  constructor(private http: HttpClient) {}

  getMyPayments(pageNumber = 1, pageSize = 10, status?: number): Observable<PagedResult<Payment>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status !== undefined) params = params.set('status', status);
    return this.http.get<PagedResult<Payment>>(`${API_BASE}/payments/my`, { params });
  }

  getAllPayments(pageNumber = 1, pageSize = 10, status?: number, search?: string): Observable<PagedResult<Payment>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status !== undefined) params = params.set('status', status);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Payment>>(`${API_BASE}/payments/admin`, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  constructor(private http: HttpClient) {}

  getMyInvoices(pageNumber = 1, pageSize = 10, status?: number): Observable<PagedResult<Invoice>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status !== undefined) params = params.set('status', status);
    return this.http.get<PagedResult<Invoice>>(`${API_BASE}/invoices/my`, { params });
  }

  getAllInvoices(pageNumber = 1, pageSize = 10, status?: number, search?: string): Observable<PagedResult<Invoice>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status !== undefined) params = params.set('status', status);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Invoice>>(`${API_BASE}/invoices/admin`, { params });
  }

  downloadPdf(id: number): Observable<Blob> {
    return this.http.get(`${API_BASE}/invoices/${id}/download`, {
      responseType: 'blob'
    });
  }
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getMetrics(): Observable<DashboardMetrics> {
    return this.http.get<DashboardMetrics>(`${API_BASE}/dashboard/metrics`);
  }

  getRevenueChart(months = 6): Observable<RevenueChartPoint[]> {
    return this.http.get<RevenueChartPoint[]>(`${API_BASE}/dashboard/revenue-chart?months=${months}`);
  }

  getRecentActivity(limit = 10): Observable<RecentActivity[]> {
    return this.http.get<RecentActivity[]>(`${API_BASE}/dashboard/recent-activity?limit=${limit}`);
  }
}

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private http: HttpClient) {}

  getAllUsers(pageNumber = 1, pageSize = 10): Observable<PagedResult<UserManagement>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<UserManagement>>(`${API_BASE}/users`, { params });
  }

  updateUserStatus(id: number, isActive: boolean): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${API_BASE}/users/${id}/status`, { isActive });
  }
}

@Injectable({ providedIn: 'root' })
export class SimulationService {
  constructor(private http: HttpClient) {}

  simulatePayment(userId: number, planId: number): Observable<any> {
    return this.http.post(`${API_BASE}/simulation/simulate-payment`, { userId, planId });
  }
}
