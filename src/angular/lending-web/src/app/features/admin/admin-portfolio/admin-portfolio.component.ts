import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  LoanProductDto,
  OriginationService,
} from '../../../core/services/origination.service';
import {
  PortfolioDailySummary,
  PortfolioService,
} from '../../../core/services/portfolio.service';

@Component({
  selector: 'app-admin-portfolio',
  imports: [ReactiveFormsModule, DecimalPipe, RouterLink],
  templateUrl: './admin-portfolio.component.html',
  styleUrl: './admin-portfolio.component.scss',
})
export class AdminPortfolioComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly portfolio = inject(PortfolioService);
  private readonly origination = inject(OriginationService);
  private readonly router = inject(Router);

  protected readonly loadingSummary = signal(false);
  protected readonly pendingSave = signal(false);
  protected readonly pendingRun = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly summary = signal<PortfolioDailySummary | null>(null);
  protected readonly products = signal<LoanProductDto[]>([]);
  protected readonly pendingProductSave = signal(false);
  protected readonly pendingActivateId = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    businessDate: [new Date().toISOString().slice(0, 10), Validators.required],
    budgetCap: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  protected readonly productForm = this.fb.nonNullable.group({
    productCode: ['STD-LOAN', [Validators.required, Validators.maxLength(64)]],
    minPrincipal: [1000, [Validators.required, Validators.min(1)]],
    maxPrincipal: [200000, [Validators.required, Validators.min(1)]],
    minTermDays: [7, [Validators.required, Validators.min(1)]],
    maxTermDays: [365, [Validators.required, Validators.min(1)]],
    interestRatePerDay: [0.0015, [Validators.required, Validators.min(0.000001)]],
    activate: [true, [Validators.required]],
  });

  ngOnInit(): void {
    this.loadSummary();
    this.loadProducts();
  }

  protected loadSummary(): void {
    this.error.set(null);
    this.success.set(null);
    this.loadingSummary.set(true);
    const date = this.form.getRawValue().businessDate;
    this.portfolio.getDailySummary(date).subscribe({
      next: (s) => {
        this.summary.set(s);
        this.loadingSummary.set(false);
      },
      error: (e: Error) => {
        this.error.set(e.message);
        this.loadingSummary.set(false);
      },
    });
  }

  protected saveBudget(): void {
    this.error.set(null);
    this.success.set(null);
    if (this.form.invalid || this.pendingSave()) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.pendingSave.set(true);
    this.portfolio
      .setDailyBudget({
        businessDate: v.businessDate,
        budgetCap: v.budgetCap!,
      })
      .subscribe({
        next: () => {
          this.pendingSave.set(false);
          this.success.set('Ліміт бюджету оновлено.');
          this.loadSummary();
        },
        error: (e: Error) => {
          this.pendingSave.set(false);
          this.error.set(e.message);
        },
      });
  }

  protected startRun(): void {
    this.error.set(null);
    this.success.set(null);
    const businessDate = this.form.controls.businessDate;
    if (businessDate.invalid || this.pendingRun()) {
      businessDate.markAsTouched();
      return;
    }
    const date = businessDate.value;
    this.pendingRun.set(true);
    this.portfolio.startRun(date).subscribe({
      next: (r) => {
        this.pendingRun.set(false);
        void this.router.navigate(['/admin/runs', r.portfolioRunId]);
      },
      error: (e: Error) => {
        this.pendingRun.set(false);
        this.error.set(e.message);
      },
    });
  }

  protected loadProducts(): void {
    this.origination.listLoanProducts().subscribe({
      next: (rows) => this.products.set(rows),
      error: (e: Error) => this.error.set(e.message),
    });
  }

  protected saveProduct(): void {
    this.error.set(null);
    this.success.set(null);
    if (this.productForm.invalid || this.pendingProductSave()) {
      this.productForm.markAllAsTouched();
      return;
    }
    const v = this.productForm.getRawValue();
    this.pendingProductSave.set(true);
    this.origination
      .createLoanProduct({
        productCode: v.productCode,
        minPrincipal: v.minPrincipal,
        maxPrincipal: v.maxPrincipal,
        minTermDays: v.minTermDays,
        maxTermDays: v.maxTermDays,
        interestRatePerDay: v.interestRatePerDay,
        activate: v.activate,
      })
      .subscribe({
        next: () => {
          this.pendingProductSave.set(false);
          this.success.set('Нову версію продукту створено.');
          this.loadProducts();
        },
        error: (e: Error) => {
          this.pendingProductSave.set(false);
          this.error.set(e.message);
        },
      });
  }

  protected activateProduct(loanProductId: string): void {
    this.error.set(null);
    this.pendingActivateId.set(loanProductId);
    this.origination.activateLoanProduct(loanProductId).subscribe({
      next: () => {
        this.pendingActivateId.set(null);
        this.success.set('Продукт активовано.');
        this.loadProducts();
      },
      error: (e: Error) => {
        this.pendingActivateId.set(null);
        this.error.set(e.message);
      },
    });
  }
}
