import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UiNotificationService {
  private readonly _error = signal<string | null>(null);
  readonly error = this._error.asReadonly();
  private hideTimer: ReturnType<typeof setTimeout> | null = null;

  showError(message: string): void {
    const normalized = message.trim();
    if (!normalized) return;
    this._error.set(normalized);

    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
    }

    this.hideTimer = setTimeout(() => {
      this._error.set(null);
      this.hideTimer = null;
    }, 7000);
  }

  dismissError(): void {
    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }
    this._error.set(null);
  }
}
