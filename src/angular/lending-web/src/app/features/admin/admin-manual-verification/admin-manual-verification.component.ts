import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OriginationService, ManualVerificationItem } from '../../../core/services/origination.service';
import { appDisplayId } from '../../../core/utils/id-format';

@Component({
  selector: 'app-admin-manual-verification',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './admin-manual-verification.component.html',
  styleUrl: './admin-manual-verification.component.scss',
})
export class AdminManualVerificationComponent implements OnInit {
  private readonly origination = inject(OriginationService);

  protected readonly items = signal<ManualVerificationItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly approveAmounts = signal<Record<string, number | undefined>>({});

  protected appId(id: string): string {
    return appDisplayId(id);
  }

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.origination.getManualPending().subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.loading.set(false);
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loading.set(false);
      },
    });
  }

  protected onApproveAmountInput(id: string, raw: string): void {
    const t = raw.trim();
    if (t === '') {
      this.approveAmounts.update((m) => {
        const next = { ...m };
        delete next[id];
        return next;
      });
      return;
    }
    const n = Number(t);
    if (!Number.isFinite(n)) return;
    this.approveAmounts.update((m) => ({ ...m, [id]: n }));
  }

  protected decide(item: ManualVerificationItem, decision: 'Approve' | 'Reject'): void {
    const amount =
      decision === 'Approve' ? this.approveAmounts()[item.applicationId] : undefined;
    this.origination
      .applyManualDecision(item.applicationId, decision, undefined, amount)
      .subscribe({
        next: () => {
          this.approveAmounts.update((m) => {
            const next = { ...m };
            delete next[item.applicationId];
            return next;
          });
          this.reload();
        },
        error: (e: Error) => this.error.set(e.message),
      });
  }
}
