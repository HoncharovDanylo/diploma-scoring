import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { PortfolioService } from '../../../core/services/portfolio.service';

@Component({
  selector: 'app-admin-budget',
  imports: [ReactiveFormsModule],
  templateUrl: './admin-budget.component.html',
  styleUrl: './admin-budget.component.scss',
})
export class AdminBudgetComponent {
  private readonly fb = inject(FormBuilder);
  private readonly portfolio = inject(PortfolioService);

  protected readonly pending = signal(false);
  protected readonly success = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    businessDate: [
      new Date().toISOString().slice(0, 10),
      Validators.required,
    ],
    budgetCap: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  protected save(): void {
    this.error.set(null);
    this.success.set(false);
    if (this.form.invalid || this.pending()) {
      this.form.markAllAsTouched();
      return;
    }
    this.pending.set(true);
    const v = this.form.getRawValue();
    this.portfolio
      .setDailyBudget({
        businessDate: v.businessDate!,
        budgetCap: v.budgetCap!,
      })
      .subscribe({
        next: () => {
          this.success.set(true);
          this.pending.set(false);
        },
        error: (e: Error) => {
          this.error.set(e.message);
          this.pending.set(false);
        },
      });
  }
}
