import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      <div *ngFor="let toast of notificationService.toasts()"
           class="toast-item"
           [ngClass]="'toast-' + toast.type">
        <span class="toast-icon">
          {{ toast.type === 'success' ? '✔' : toast.type === 'error' ? '✖' : 'ℹ' }}
        </span>
        <span class="toast-text">{{ toast.text }}</span>
        <button class="toast-close" (click)="notificationService.dismiss(toast.id)">&times;</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      bottom: 1.5rem;
      right: 1.5rem;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      max-width: 400px;
    }
    .toast-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.875rem 1rem;
      border-radius: 8px;
      font-size: 0.875rem;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
      animation: slideIn 0.25s ease forwards;
    }
    @keyframes slideIn {
      from { transform: translateX(100%); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
    .toast-success { background: #10b981; color: #ffffff; }
    .toast-error { background: #ef4444; color: #ffffff; }
    .toast-warning { background: #f59e0b; color: #ffffff; }
    .toast-info { background: #2563eb; color: #ffffff; }
    .toast-close {
      background: none;
      border: none;
      color: inherit;
      font-size: 1.25rem;
      cursor: pointer;
      margin-left: auto;
      opacity: 0.8;
      &:hover { opacity: 1; }
    }
  `]
})
export class ToastComponent {
  constructor(public notificationService: NotificationService) {}
}
