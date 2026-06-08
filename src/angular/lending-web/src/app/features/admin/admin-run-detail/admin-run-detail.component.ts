import { DecimalPipe } from '@angular/common';
import {
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OriginationService } from '../../../core/services/origination.service';
import {
  PortfolioRunDetail,
  PortfolioSelectionRow,
  PortfolioService,
} from '../../../core/services/portfolio.service';
import {
  applicationStatusLabel,
  portfolioRunStatusLabel,
} from '../../../core/utils/domain-labels';
import { appDisplayId, runDisplayId } from '../../../core/utils/id-format';

@Component({
  selector: 'app-admin-run-detail',
  imports: [RouterLink, DecimalPipe],
  templateUrl: './admin-run-detail.component.html',
  styleUrl: './admin-run-detail.component.scss',
})
export class AdminRunDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly portfolio = inject(PortfolioService);
  private readonly origination = inject(OriginationService);

  protected readonly run = signal<PortfolioRunDetail | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly issueDraft = signal<Record<string, number>>({});
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly actionBusyId = signal<string | null>(null);

  private tick: ReturnType<typeof setInterval> | null = null;
  private runParamId: string | null = null;
  private seedDraftOnce = false;

  protected runId(id: string): string {
    return runDisplayId(id);
  }

  protected applicationId(id: string): string {
    return appDisplayId(id);
  }

  protected readonly runStatusLabel = portfolioRunStatusLabel;
  protected readonly appStatusLabel = applicationStatusLabel;

  ngOnInit(): void {
    this.runParamId = this.route.snapshot.paramMap.get('id');
    this.seedDraftOnce = false;
    if (!this.runParamId) {
      this.error.set('Некоректний ID');
      this.loading.set(false);
      return;
    }
    this.fetch(this.runParamId);
  }

  ngOnDestroy(): void {
    this.clearTick();
  }

  private clearTick(): void {
    if (this.tick != null) {
      clearInterval(this.tick);
      this.tick = null;
    }
  }

  private shouldPoll(status: string): boolean {
    return status === 'Pending';
  }

  protected onIssueAmountInput(
    applicationId: string,
    raw: string,
    fallback: number
  ): void {
    const t = raw.trim();
    if (t === '') {
      this.issueDraft.update((m) => ({ ...m, [applicationId]: fallback }));
      return;
    }
    const n = Number(t);
    if (!Number.isFinite(n)) return;
    this.issueDraft.update((m) => ({ ...m, [applicationId]: n }));
  }

  protected draftAmount(row: PortfolioSelectionRow): number {
    const d = this.issueDraft()[row.applicationId];
    return d !== undefined ? d : row.recommendedPrincipal;
  }

  protected canDecide(row: PortfolioSelectionRow): boolean {
    return row.applicationStatus === 'ManualVerificationPending';
  }

  protected approve(row: PortfolioSelectionRow): void {
    const req = row.requestedPrincipal;
    const min = row.productMinPrincipal;
    if (req == null || min == null) {
      this.actionMessage.set(
        'Немає даних заявки з Origination. Перевірте, що сервіс Origination доступний.'
      );
      return;
    }
    const amount = this.draftAmount(row);
    if (amount < min || amount > req) {
      this.actionMessage.set(
        `Сума має бути в межах ${min.toFixed(2)} … ${req.toFixed(2)}.`
      );
      return;
    }
    this.postDecision(row.applicationId, 'Approve', amount);
  }

  protected reject(row: PortfolioSelectionRow): void {
    this.postDecision(row.applicationId, 'Reject', undefined);
  }

  private postDecision(
    applicationId: string,
    decision: 'Approve' | 'Reject',
    approvedPrincipal?: number
  ): void {
    this.actionBusyId.set(applicationId);
    this.actionMessage.set(null);
    this.origination
      .applyManualDecision(applicationId, decision, undefined, approvedPrincipal)
      .subscribe({
        next: () => {
          this.actionBusyId.set(null);
          this.actionMessage.set('Дію збережено.');
          if (this.runParamId) this.fetch(this.runParamId);
        },
        error: (e: Error) => {
          this.actionBusyId.set(null);
          this.actionMessage.set(e.message);
        },
      });
  }

  private fetch(id: string): void {
    this.error.set(null);
    this.portfolio.getRun(id).subscribe({
      next: (r) => {
        this.run.set(r);
        this.loading.set(false);
        if (!this.seedDraftOnce && r.status === 'Succeeded' && r.selections.length > 0) {
          const d: Record<string, number> = {};
          for (const s of r.selections) {
            d[s.applicationId] = s.recommendedPrincipal;
          }
          this.issueDraft.set(d);
          this.seedDraftOnce = true;
        }
        this.clearTick();
        if (this.shouldPoll(r.status)) {
          this.tick = setInterval(() => {
            this.portfolio.getRun(id).subscribe({
              next: (n) => {
                this.run.set(n);
                if (!this.shouldPoll(n.status)) {
                  this.clearTick();
                  if (
                    !this.seedDraftOnce &&
                    n.status === 'Succeeded' &&
                    n.selections.length > 0
                  ) {
                    const d: Record<string, number> = {};
                    for (const s of n.selections) {
                      d[s.applicationId] = s.recommendedPrincipal;
                    }
                    this.issueDraft.set(d);
                    this.seedDraftOnce = true;
                  }
                }
              },
              error: () => {},
            });
          }, 2000);
        }
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loading.set(false);
      },
    });
  }
}
