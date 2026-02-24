import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  autohide: boolean;
  delay?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toasts: Toast[] = [];
  private toastSubject = new Subject<Toast[]>();
  private nextId = 1;

  toasts$ = this.toastSubject.asObservable();

  show(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info', delay: number = 5000) {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type,
      autohide: true,
      delay
    };

    this.toasts.push(toast);
    this.toastSubject.next([...this.toasts]);

    if (toast.autohide) {
      setTimeout(() => this.remove(toast.id), delay);
    }
  }

  success(message: string, delay?: number) {
    this.show(message, 'success', delay);
  }

  error(message: string, delay?: number) {
    this.show(message, 'error', delay || 7000);
  }

  warning(message: string, delay?: number) {
    this.show(message, 'warning', delay);
  }

  info(message: string, delay?: number) {
    this.show(message, 'info', delay);
  }

  remove(id: number) {
    this.toasts = this.toasts.filter(t => t.id !== id);
    this.toastSubject.next([...this.toasts]);
  }

  clear() {
    this.toasts = [];
    this.toastSubject.next([]);
  }

  getToasts(): Toast[] {
    return [...this.toasts];
  }
}
