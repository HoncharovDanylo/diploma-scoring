import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { PortfolioService } from '../../../core/services/portfolio.service';

@Component({
  selector: 'app-admin-runs',
  imports: [ReactiveFormsModule],
  templateUrl: './admin-runs.component.html',
  styleUrl: './admin-runs.component.scss',
})
export class AdminRunsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly portfolio = inject(PortfolioService);
  private readonly router = inject(Router);

  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    businessDate: [
      new Date().toISOString().slice(0, 10),
      Validators.required,
    ],
  });

  protected startRun(): void {
    this.error.set(null);
    if (this.form.invalid || this.pending()) {
      this.form.markAllAsTouched();
      return;
    }
    this.pending.set(true);
    this.portfolio
      .startRun(this.form.getRawValue().businessDate)
      .subscribe({
        next: (r) => {
          this.pending.set(false);
          void this.router.navigate(['/admin/runs', r.portfolioRunId]);
        },
        error: (e: Error) => {
          this.error.set(e.message);
          this.pending.set(false);
        },
      });
  }
}
