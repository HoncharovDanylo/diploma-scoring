import { DecimalPipe } from '@angular/common';
import {
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import {
  ApplicationDetail,
  OriginationService,
} from '../../../core/services/origination.service';
import {
  applicationStatusLabel,
  scoringAttemptStatusLabel,
  scoringDecisionLabel,
  shapDirectionLabel,
} from '../../../core/utils/domain-labels';
import { appDisplayId } from '../../../core/utils/id-format';

@Component({
  selector: 'app-application-detail',
  imports: [RouterLink, DecimalPipe],
  templateUrl: './application-detail.component.html',
  styleUrl: './application-detail.component.scss',
})
export class ApplicationDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly origination = inject(OriginationService);
  protected readonly auth = inject(AuthService);

  protected readonly app = signal<ApplicationDetail | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);
  private pollId: ReturnType<typeof setInterval> | null = null;

  protected appId(id: string): string {
    return appDisplayId(id);
  }

  protected readonly statusLabel = applicationStatusLabel;
  protected readonly attemptStatusLabel = scoringAttemptStatusLabel;
  protected readonly decisionLabel = scoringDecisionLabel;
  protected readonly shapDirection = shapDirectionLabel;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Некоректний ідентифікатор');
      this.loading.set(false);
      return;
    }
    this.fetch(id);
  }

  ngOnDestroy(): void {
    this.clearPoll();
  }

  private clearPoll(): void {
    if (this.pollId != null) {
      clearInterval(this.pollId);
      this.pollId = null;
    }
  }

  private fetch(id: string): void {
    this.error.set(null);
    this.origination.getById(id).subscribe({
      next: (a) => {
        this.app.set(a);
        this.loading.set(false);
        this.clearPoll();
        if (a.status === 'ScoringPending') {
          this.pollId = setInterval(() => {
            this.origination.getById(id).subscribe({
              next: (b) => {
                this.app.set(b);
                if (b.status !== 'ScoringPending') {
                  this.clearPoll();
                }
              },
              error: () => {},
            });
          }, 5000);
        }
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loading.set(false);
      },
    });
  }
}
