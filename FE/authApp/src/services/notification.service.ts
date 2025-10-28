import { Injectable } from '@angular/core';

export interface NotificationMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  duration?: number;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notifications: NotificationMessage[] = [];
  private readonly defaultDuration = 5000; // 5 seconds

  constructor() {}

  success(title: string, message: string, duration?: number): void {
    this.addNotification('success', title, message, duration);
  }

  error(title: string, message: string, duration?: number): void {
    this.addNotification('error', title, message, duration);
  }

  warning(title: string, message: string, duration?: number): void {
    this.addNotification('warning', title, message, duration);
  }

  info(title: string, message: string, duration?: number): void {
    this.addNotification('info', title, message, duration);
  }

  private addNotification(
    type: 'success' | 'error' | 'warning' | 'info',
    title: string,
    message: string,
    duration?: number
  ): void {
    const notification: NotificationMessage = {
      id: this.generateId(),
      type,
      title,
      message,
      duration: duration || this.defaultDuration,
      timestamp: new Date()
    };

    this.notifications.push(notification);

    // Auto-remove notification after duration
    if (notification.duration && notification.duration > 0) {
      setTimeout(() => {
        this.removeNotification(notification.id);
      }, notification.duration);
    }
  }

  removeNotification(id: string): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  getAllNotifications(): NotificationMessage[] {
    return [...this.notifications];
  }

  clearAll(): void {
    this.notifications = [];
  }

  private generateId(): string {
    return Date.now().toString() + Math.random().toString(36).substr(2, 9);
  }
}