import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PlanService } from '../../core/services/api.services';
import { NotificationService } from '../../core/services/notification.service';
import { SubscriptionPlan } from '../../core/models/models';

@Component({
  selector: 'app-plans-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="admin-container">
      <div class="page-header">
        <div>
          <h2>Subscription Plans Management</h2>
          <p class="subtitle">Create, configure pricing, manage features, and control plan availability.</p>
        </div>
        <button class="btn btn-primary" (click)="openCreateModal()">
          + Create New Plan
        </button>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <p>Loading plans catalog...</p>
      </div>

      <div *ngIf="!isLoading && plans.length === 0" class="card empty-state">
        <p>No subscription plans found. Create your first plan above.</p>
      </div>

      <div *ngIf="!isLoading && plans.length > 0" class="card table-card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Plan Name</th>
                <th>Interval</th>
                <th>Price</th>
                <th>Features</th>
                <th>Status</th>
                <th>Stripe Price ID</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of plans">
                <td>
                  <div class="font-bold text-dark">{{ p.name }}</div>
                  <div class="font-sm text-muted">{{ p.description }}</div>
                </td>
                <td>
                  <span class="badge" [ngClass]="p.billingInterval === 2 ? 'badge-primary' : 'badge-secondary'">
                    {{ p.billingInterval === 2 ? 'Yearly' : 'Monthly' }}
                  </span>
                </td>
                <td class="font-bold text-dark">
                  \${{ p.price | number:'1.2-2' }}
                </td>
                <td>
                  <span class="features-pill" [title]="p.features.join(', ')">
                    {{ p.features.length }} features
                  </span>
                </td>
                <td>
                  <span class="badge" [ngClass]="p.isActive ? 'badge-success' : 'badge-danger'">
                    {{ p.isActive ? 'Active' : 'Archived' }}
                  </span>
                </td>
                <td>
                  <span class="font-mono font-sm text-muted">{{ p.stripePriceId || 'Pending sync' }}</span>
                </td>
                <td>
                  <div class="action-buttons">
                    <button class="btn btn-sm btn-outline" (click)="openEditModal(p)">Edit</button>
                    <button
                      class="btn btn-sm"
                      [ngClass]="p.isActive ? 'btn-danger' : 'btn-success'"
                      (click)="toggleStatus(p)">
                      {{ p.isActive ? 'Deactivate' : 'Activate' }}
                    </button>
                    <button class="btn btn-sm btn-outline text-danger" (click)="deletePlan(p)">Delete</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Plan Form Modal -->
      <div class="modal-backdrop" *ngIf="showModal">
        <div class="modal-card">
          <div class="modal-header">
            <h3>{{ editingPlanId ? 'Edit Subscription Plan' : 'Create Subscription Plan' }}</h3>
            <button class="close-btn" (click)="closeModal()">✕</button>
          </div>

          <form [formGroup]="planForm" (ngSubmit)="savePlan()">
            <div class="form-group">
              <label for="name">Plan Name *</label>
              <input id="name" type="text" class="form-control" formControlName="name" placeholder="e.g. Starter, Pro, Enterprise" />
            </div>

            <div class="form-group">
              <label for="desc">Description *</label>
              <textarea id="desc" class="form-control" formControlName="description" rows="2" placeholder="Brief plan summary"></textarea>
            </div>

            <div class="form-row" *ngIf="!editingPlanId">
              <div class="form-group col">
                <label for="price">Price (USD) *</label>
                <input id="price" type="number" step="0.01" class="form-control" formControlName="price" placeholder="19.99" />
              </div>

              <div class="form-group col">
                <label for="billingInterval">Billing Interval *</label>
                <select id="billingInterval" class="form-control" formControlName="billingInterval">
                  <option [ngValue]="1">Monthly</option>
                  <option [ngValue]="2">Yearly</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label for="features">Features (comma-separated or one per line) *</label>
              <textarea id="features" class="form-control" formControlName="featuresInput" rows="3" placeholder="Unlimited Projects, 10 Team Members, 24/7 Priority Support"></textarea>
              <small class="form-hint">Enter the feature bullet points displayed on the pricing card.</small>
            </div>

            <div class="form-group checkbox-group" *ngIf="editingPlanId">
              <label class="checkbox-label">
                <input type="checkbox" formControlName="isActive" />
                <span>Plan is Active and available for new subscriptions</span>
              </label>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-outline" (click)="closeModal()">Cancel</button>
              <button type="submit" class="btn btn-primary" [disabled]="planForm.invalid || isSaving">
                {{ isSaving ? 'Saving...' : (editingPlanId ? 'Update Plan' : 'Create Plan') }}
              </button>
            </div>
          </form>
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
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      flex-wrap: wrap;
      gap: 1rem;
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
    .action-buttons {
      display: flex;
      gap: 0.5rem;
    }
    .font-bold { font-weight: 600; }
    .text-dark { color: #0f172a; }
    .text-muted { color: #64748b; }
    .text-danger { color: #dc2626; }
    .font-sm { font-size: 0.825rem; }
    .font-mono { font-family: monospace; }
    .features-pill {
      background: #f1f5f9;
      padding: 0.25rem 0.6rem;
      border-radius: 999px;
      font-size: 0.8rem;
      cursor: help;
    }
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(15, 23, 42, 0.5);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }
    .modal-card {
      background: #ffffff;
      border-radius: 12px;
      width: 100%;
      max-width: 560px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      padding: 1.75rem;
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
    }
    .modal-header h3 {
      font-size: 1.25rem;
      font-weight: 700;
      margin: 0;
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.25rem;
      color: #94a3b8;
      cursor: pointer;
    }
    .form-group {
      margin-bottom: 1.2rem;
    }
    .form-row {
      display: flex;
      gap: 1rem;
    }
    .col { flex: 1; }
    .form-hint {
      display: block;
      margin-top: 0.25rem;
      color: #64748b;
      font-size: 0.8rem;
    }
    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      font-size: 0.9rem;
    }
    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1.75rem;
    }
    .empty-state {
      text-align: center;
      padding: 3rem;
      color: #64748b;
    }
  `]
})
export class PlansManagementComponent implements OnInit {
  plans: SubscriptionPlan[] = [];
  isLoading = false;
  showModal = false;
  editingPlanId?: number;
  isSaving = false;
  private cdr = inject(ChangeDetectorRef);

  planForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private planService: PlanService,
    private notify: NotificationService
  ) {
    this.planForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.required, Validators.maxLength(500)]],
      price: [19.99, [Validators.required, Validators.min(0.5)]],
      billingInterval: [1, [Validators.required]],
      featuresInput: ['', [Validators.required]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.loadPlans();
  }

  loadPlans(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.planService.getAllPlansForAdmin().subscribe({
      next: (data) => {
        this.plans = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to load plans: ' + (err.error?.message || err.message));
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openCreateModal(): void {
    this.editingPlanId = undefined;
    this.planForm.reset({
      name: '',
      description: '',
      price: 19.99,
      billingInterval: 1,
      featuresInput: '',
      isActive: true
    });
    this.showModal = true;
  }

  openEditModal(plan: SubscriptionPlan): void {
    this.editingPlanId = plan.id;
    this.planForm.patchValue({
      name: plan.name,
      description: plan.description,
      price: plan.price,
      billingInterval: plan.billingInterval,
      featuresInput: plan.features.join(', '),
      isActive: plan.isActive
    });
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingPlanId = undefined;
  }

  savePlan(): void {
    if (this.planForm.invalid) return;

    this.isSaving = true;
    const formVal = this.planForm.value;
    const features = formVal.featuresInput
      .split(/,|\n/)
      .map((f: string) => f.trim())
      .filter((f: string) => f.length > 0);

    if (this.editingPlanId) {
      this.planService.updatePlan(this.editingPlanId, {
        name: formVal.name,
        description: formVal.description,
        isActive: formVal.isActive,
        features: features
      }).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.notify.success('Subscription plan updated successfully.');
          this.loadPlans();
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSaving = false;
          this.notify.error('Update failed: ' + (err.error?.message || err.message));
          this.cdr.markForCheck();
        }
      });
    } else {
      this.planService.createPlan({
        name: formVal.name,
        description: formVal.description,
        price: formVal.price,
        currency: 'usd',
        billingInterval: formVal.billingInterval,
        features: features
      }).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.notify.success('Subscription plan created and synced to Stripe.');
          this.loadPlans();
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isSaving = false;
          this.notify.error('Create failed: ' + (err.error?.message || err.message));
          this.cdr.markForCheck();
        }
      });
    }
  }

  toggleStatus(plan: SubscriptionPlan): void {
    const nextStatus = !plan.isActive;
    this.planService.toggleStatus(plan.id, nextStatus).subscribe({
      next: () => {
        plan.isActive = nextStatus;
        this.notify.success(`Plan ${plan.name} is now ${nextStatus ? 'Active' : 'Archived'}.`);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notify.error('Failed to change plan status: ' + (err.error?.message || err.message));
        this.cdr.markForCheck();
      }
    });
  }

  deletePlan(plan: SubscriptionPlan): void {
    if (!confirm(`Are you sure you want to delete "${plan.name}"? Only plans with no active subscriptions can be deleted.`)) {
      return;
    }

    this.planService.deletePlan(plan.id).subscribe({
      next: () => {
        this.notify.success(`Plan "${plan.name}" was deleted successfully.`);
        this.loadPlans();
      },
      error: (err) => {
        this.notify.error('Cannot delete plan: ' + (err.error?.message || err.message));
      }
    });
  }
}
