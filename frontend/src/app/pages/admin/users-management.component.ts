import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { UserManagement, PagedResult } from '../../core/models/models';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-users-management',
  standalone: true,
  imports: [CommonModule, PaginationComponent],
  template: `
    <div class="admin-container">
      <div class="page-header">
        <div>
          <h2>User Accounts & Security</h2>
          <p class="subtitle">Review registered customer and staff accounts, assign roles, and toggle access permissions.</p>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading accounts...</p>
      </div>

      <div *ngIf="!isLoading && pagedData?.items?.length === 0" class="card empty-state">
        <p>No user accounts found.</p>
      </div>

      <div *ngIf="!isLoading && pagedData && pagedData.items.length > 0" class="card table-card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>User</th>
                <th>Roles</th>
                <th>Stripe Customer ID</th>
                <th>Registered Date</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let u of pagedData.items">
                <td>
                  <div class="font-bold text-dark">{{ u.firstName }} {{ u.lastName }}</div>
                  <div class="font-sm text-muted">{{ u.email }}</div>
                </td>
                <td>
                  <span *ngFor="let r of u.roles" class="role-pill" [ngClass]="r === 'Admin' ? 'role-admin' : 'role-customer'">
                    {{ r }}
                  </span>
                </td>
                <td>
                  <span class="font-mono font-sm text-muted">{{ u.stripeCustomerId || '—' }}</span>
                </td>
                <td class="text-muted font-sm">
                  {{ u.createdAtUtc | date:'mediumDate' }}
                </td>
                <td>
                  <span class="badge" [ngClass]="u.isActive ? 'badge-success' : 'badge-danger'">
                    {{ u.isActive ? 'Active' : 'Suspended' }}
                  </span>
                </td>
                <td>
                  <button
                    class="btn btn-sm"
                    [ngClass]="u.isActive ? 'btn-danger' : 'btn-success'"
                    [disabled]="isUpdatingId === u.id"
                    (click)="toggleUserStatus(u)">
                    {{ u.isActive ? 'Suspend' : 'Activate' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <app-pagination
          [pageNumber]="pagedData.pageNumber"
          [totalPages]="pagedData.totalPages"
          [totalCount]="pagedData.totalCount"
          (pageChange)="onPageChange($event)">
        </app-pagination>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      max-width: 1200px;
      margin: 2rem auto;
      padding: 0 1rem;
    }
    .page-header {
      margin-bottom: 2rem;
    }
    .page-header h2 {
      font-size: 1.85rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.25rem;
    }
    .subtitle {
      color: #64748b;
      font-size: 0.95rem;
    }
    .table-card {
      padding: 0;
      overflow: hidden;
    }
    .table-responsive {
      overflow-x: auto;
    }
    .table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }
    .table th {
      background: #f8fafc;
      color: #475569;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 0.85rem 1.25rem;
      border-bottom: 1px solid #e2e8f0;
    }
    .table td {
      padding: 1rem 1.25rem;
      border-bottom: 1px solid #f1f5f9;
      font-size: 0.9rem;
      vertical-align: middle;
    }
    .table tr:last-child td {
      border-bottom: none;
    }
    .role-pill {
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.2rem 0.5rem;
      border-radius: 999px;
      margin-right: 0.35rem;
    }
    .role-admin {
      background: #fef3c7;
      color: #b45309;
    }
    .role-customer {
      background: #e0f2fe;
      color: #0369a1;
    }
    .font-bold { font-weight: 600; }
    .text-dark { color: #0f172a; }
    .text-muted { color: #64748b; }
    .font-sm { font-size: 0.825rem; }
    .font-mono { font-family: monospace; }
    .empty-state, .loading-state {
      text-align: center;
      padding: 3rem;
      color: #64748b;
    }
  `]
})
export class UsersManagementComponent implements OnInit {
  pagedData?: PagedResult<UserManagement>;
  currentPage = 1;
  pageSize = 10;
  isLoading = false;
  isUpdatingId?: number;
  private cdr = inject(ChangeDetectorRef);

  constructor(
    private userService: UserService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.userService.getAllUsers(this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        this.pagedData = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to load users: ' + (err.error?.message || err.message));
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadUsers();
  }

  toggleUserStatus(user: UserManagement): void {
    const nextStatus = !user.isActive;
    this.isUpdatingId = user.id;
    this.cdr.markForCheck();

    this.userService.updateUserStatus(user.id, nextStatus).subscribe({
      next: () => {
        user.isActive = nextStatus;
        this.isUpdatingId = undefined;
        this.notify.success(`User ${user.email} is now ${nextStatus ? 'Active' : 'Suspended'}.`);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isUpdatingId = undefined;
        this.notify.error('Failed to update user status: ' + (err.error?.message || err.message));
        this.cdr.markForCheck();
      }
    });
  }
}
