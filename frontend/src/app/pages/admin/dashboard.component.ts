import { Component, OnInit, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { DashboardService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { DashboardMetrics, RevenueChartPoint, RecentActivity } from '../../core/models/models';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="admin-container">
      <div class="dashboard-header">
        <div>
          <h2>System Administration Dashboard</h2>
          <p class="subtitle">Platform-wide subscription metrics, revenue analytics, and transaction operations.</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-outline btn-sm" (click)="loadAllData()" [disabled]="isLoading">
            🔄 Refresh Metrics
          </button>
        </div>
      </div>

      <div *ngIf="isLoading && !metrics" class="loading-state">
        <p>Calculating platform metrics and revenue aggregate...</p>
      </div>

      <!-- KPI Summary Cards -->
      <div class="metrics-grid" *ngIf="metrics">
        <!-- MRR -->
        <div class="card metric-card">
          <div class="metric-top">
            <span class="metric-label">Monthly Recurring Revenue (MRR)</span>
            <span class="metric-icon mrr-icon">📈</span>
          </div>
          <h3 class="metric-number">\${{ metrics.monthlyRecurringRevenue | number:'1.2-2' }}</h3>
          <span class="metric-subtext text-success">Normalized recurring billing</span>
        </div>

        <!-- Total Revenue -->
        <div class="card metric-card">
          <div class="metric-top">
            <span class="metric-label">Total Lifetime Revenue</span>
            <span class="metric-icon rev-icon">💰</span>
          </div>
          <h3 class="metric-number">\${{ metrics.totalRevenue | number:'1.2-2' }}</h3>
          <span class="metric-subtext">All time settled invoices</span>
        </div>

        <!-- Active Subscriptions -->
        <div class="card metric-card">
          <div class="metric-top">
            <span class="metric-label">Active Subscriptions</span>
            <span class="metric-icon sub-icon">👥</span>
          </div>
          <h3 class="metric-number">{{ metrics.activeSubscriptions }}</h3>
          <span class="metric-subtext">
            Out of {{ metrics.totalCustomers }} registered customers ({{ metrics.canceledSubscriptions }} canceled)
          </span>
        </div>

        <!-- Payment Health -->
        <div class="card metric-card">
          <div class="metric-top">
            <span class="metric-label">Payment Processing</span>
            <span class="metric-icon pay-icon">⚡</span>
          </div>
          <div class="payment-stats">
            <div>
              <span class="badge badge-success">{{ metrics.successfulPaymentsCount }} Succeeded</span>
            </div>
            <div *ngIf="metrics.failedPaymentsCount > 0">
              <span class="badge badge-danger">{{ metrics.failedPaymentsCount }} Failed</span>
            </div>
          </div>
          <span class="metric-subtext">Automated Stripe reconciliation</span>
        </div>
      </div>

      <!-- Analytics Chart and Activity Split -->
      <div class="analytics-row">
        <!-- Revenue Trend Chart -->
        <div class="card chart-card">
          <div class="card-title-bar">
            <h3>Revenue & Growth Trends (Last 6 Months)</h3>
            <span class="chart-legend-badge">Live DB Query</span>
          </div>
          <div class="chart-container">
            <canvas #revenueChartCanvas></canvas>
          </div>
        </div>

        <!-- Recent Activity Feed -->
        <div class="card activity-card">
          <div class="card-title-bar">
            <h3>Recent System Activity</h3>
            <a routerLink="/admin/payments" class="font-sm text-link">View all &rarr;</a>
          </div>

          <div *ngIf="activities.length === 0" class="empty-feed">
            <p>No recent activity recorded yet.</p>
          </div>

          <ul class="activity-list" *ngIf="activities.length > 0">
            <li *ngFor="let item of activities" class="activity-item">
              <div class="activity-marker" [ngClass]="getActivityClass(item.type)"></div>
              <div class="activity-content">
                <div class="activity-desc">{{ item.description }}</div>
                <div class="activity-meta">
                  <span *ngIf="item.amount" class="activity-amount font-bold">
                    \${{ item.amount | number:'1.2-2' }}
                  </span>
                  <span class="activity-time text-muted">
                    {{ item.timestampUtc | date:'short' }}
                  </span>
                </div>
              </div>
            </li>
          </ul>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      max-width: 1200px;
      margin: 2rem auto;
      padding: 0 1rem;
    }
    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      flex-wrap: wrap;
      gap: 1rem;
    }
    .dashboard-header h2 {
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
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 1.25rem;
      margin-bottom: 2rem;
    }
    .metric-card {
      padding: 1.5rem;
    }
    .metric-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.75rem;
    }
    .metric-label {
      font-size: 0.8rem;
      text-transform: uppercase;
      font-weight: 600;
      color: #64748b;
      letter-spacing: 0.05em;
    }
    .metric-icon {
      font-size: 1.35rem;
      background: #f8fafc;
      padding: 0.35rem 0.6rem;
      border-radius: 8px;
    }
    .metric-number {
      font-size: 2rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.35rem;
    }
    .metric-subtext {
      font-size: 0.8rem;
      color: #64748b;
    }
    .payment-stats {
      display: flex;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
    }
    .analytics-row {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 1.5rem;
    }
    @media (max-width: 900px) {
      .analytics-row {
        grid-template-columns: 1fr;
      }
    }
    .chart-card, .activity-card {
      padding: 1.5rem;
    }
    .card-title-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.25rem;
      border-bottom: 1px solid #f1f5f9;
      padding-bottom: 0.75rem;
    }
    .card-title-bar h3 {
      font-size: 1.15rem;
      font-weight: 600;
      color: #1e293b;
      margin: 0;
    }
    .chart-legend-badge {
      font-size: 0.75rem;
      background: #e2e8f0;
      color: #334155;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-weight: 600;
    }
    .chart-container {
      position: relative;
      height: 320px;
      width: 100%;
    }
    .activity-list {
      list-style: none;
      padding: 0;
      margin: 0;
      max-height: 330px;
      overflow-y: auto;
    }
    .activity-item {
      display: flex;
      gap: 0.75rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid #f8fafc;
    }
    .activity-item:last-child {
      border-bottom: none;
    }
    .activity-marker {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-top: 0.4rem;
      flex-shrink: 0;
    }
    .marker-payment { background: #16a34a; }
    .marker-sub { background: #2563eb; }
    .marker-danger { background: #dc2626; }
    .activity-content {
      flex: 1;
    }
    .activity-desc {
      font-size: 0.875rem;
      color: #1e293b;
      line-height: 1.35;
      margin-bottom: 0.25rem;
    }
    .activity-meta {
      display: flex;
      justify-content: space-between;
      font-size: 0.775rem;
    }
    .activity-amount {
      color: #16a34a;
    }
    .empty-feed {
      text-align: center;
      padding: 2rem 0;
      color: #94a3b8;
      font-size: 0.9rem;
    }
    .font-sm { font-size: 0.85rem; }
    .font-bold { font-weight: 600; }
    .text-link {
      color: #2563eb;
      text-decoration: none;
    }
    .text-link:hover { text-decoration: underline; }
    .text-muted { color: #64748b; }
    .text-success { color: #16a34a; }
    .loading-state {
      text-align: center;
      padding: 2rem;
      color: #64748b;
    }
  `]
})
export class AdminDashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('revenueChartCanvas') chartCanvas?: ElementRef<HTMLCanvasElement>;
  
  metrics?: DashboardMetrics;
  chartPoints: RevenueChartPoint[] = [];
  activities: RecentActivity[] = [];
  isLoading = false;
  private chart?: Chart;

  private cdr = inject(ChangeDetectorRef);

  constructor(
    private dashboardService: DashboardService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadAllData();
  }

  ngAfterViewInit(): void {
    if (this.chartPoints.length > 0) {
      this.renderChart();
    }
  }

  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.destroy();
    }
  }

  loadAllData(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.dashboardService.getMetrics().subscribe({
      next: (m) => {
        this.metrics = m;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to load dashboard metrics: ' + (err.error?.message || err.message));
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });

    this.dashboardService.getRevenueChart(6).subscribe({
      next: (points) => {
        this.chartPoints = points;
        this.renderChart();
        this.cdr.markForCheck();
      }
    });

    this.dashboardService.getRecentActivity(10).subscribe({
      next: (acts) => {
        this.activities = acts;
        this.cdr.markForCheck();
      }
    });
  }

  renderChart(): void {
    if (!this.chartCanvas || this.chartPoints.length === 0) return;

    if (this.chart) {
      this.chart.destroy();
    }

    const labels = this.chartPoints.map(p => p.period);
    const revenueData = this.chartPoints.map(p => p.revenue);
    const subData = this.chartPoints.map(p => p.subscriptionsCount);

    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [
          {
            type: 'bar',
            label: 'Monthly Settled Revenue ($)',
            data: revenueData,
            backgroundColor: 'rgba(37, 99, 235, 0.75)',
            borderColor: 'rgb(37, 99, 235)',
            borderWidth: 1,
            yAxisID: 'y'
          },
          {
            type: 'line',
            label: 'Active Subscriptions',
            data: subData,
            borderColor: 'rgb(16, 185, 129)',
            backgroundColor: 'rgba(16, 185, 129, 0.15)',
            fill: false,
            tension: 0.3,
            yAxisID: 'y1'
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
            labels: {
              boxWidth: 14,
              font: { size: 12 }
            }
          }
        },
        scales: {
          y: {
            type: 'linear',
            display: true,
            position: 'left',
            beginAtZero: true,
            ticks: {
              callback: (val) => '$' + val
            },
            grid: {
              color: '#f1f5f9'
            }
          },
          y1: {
            type: 'linear',
            display: true,
            position: 'right',
            beginAtZero: true,
            grid: {
              drawOnChartArea: false
            }
          },
          x: {
            grid: {
              color: '#f8fafc'
            }
          }
        }
      }
    });
  }

  getActivityClass(type: string): string {
    if (type?.toLowerCase().includes('payment')) return 'marker-payment';
    if (type?.toLowerCase().includes('sub')) return 'marker-sub';
    return 'marker-danger';
  }
}
