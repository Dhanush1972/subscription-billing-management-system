import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  text: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  toasts = signal<ToastMessage[]>([]);

  show(text: string, type: 'success' | 'error' | 'warning' | 'info' = 'info', durationMs = 4000): void {
    const id = Math.random().toString(36).substring(2, 9);
    const toast: ToastMessage = { id, type, text };

    this.toasts.update(current => [...current, toast]);

    setTimeout(() => {
      this.dismiss(id);
    }, durationMs);
  }

  success(text: string): void {
    this.show(text, 'success');
  }

  error(text: string): void {
    this.show(text, 'error', 6000);
  }

  warning(text: string): void {
    this.show(text, 'warning');
  }

  dismiss(id: string): void {
    this.toasts.update(current => current.filter(t => t.id !== id));
  }
}
