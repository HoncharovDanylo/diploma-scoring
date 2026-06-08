import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, UserProfile } from '../../../core/services/auth.service';
import { OriginationService } from '../../../core/services/origination.service';

@Component({
  selector: 'app-application-new',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './application-new.component.html',
  styleUrl: './application-new.component.scss',
})
export class ApplicationNewComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly origination = inject(OriginationService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly profile = signal<UserProfile | null>(null);
  protected readonly profileError = signal<string | null>(null);
  protected readonly productCode = signal<string>('STD-LOAN');
  protected readonly productVersion = signal<number>(1);
  protected readonly minTermDays = signal<number>(7);
  protected readonly maxTermDays = signal<number>(365);
  protected readonly minPrincipal = signal<number>(1000);
  protected readonly maxPrincipal = signal<number>(200000);
  protected readonly interestRatePerDay = signal<number>(0.0015);

  protected readonly form = this.fb.nonNullable.group({
    requestedPrincipal: [
      1000,
      [Validators.required, Validators.min(1000), Validators.max(200000)],
    ],
    requestedTermDays: [
      30,
      [Validators.required, Validators.min(7), Validators.max(365)],
    ],
    purpose: ['', Validators.maxLength(500)],
  });

  protected readonly repaymentAmount = computed(() => {
    const principal = this.principalValue();
    const days = this.termDaysValue();
    const rate = this.interestRatePerDay();
    return Math.round(principal * (1 + rate * days) * 100) / 100;
  });
  protected readonly principalValue = toSignal(
    this.form.controls.requestedPrincipal.valueChanges,
    { initialValue: this.form.controls.requestedPrincipal.value ?? 0 }
  );
  protected readonly termDaysValue = toSignal(
    this.form.controls.requestedTermDays.valueChanges,
    { initialValue: this.form.controls.requestedTermDays.value ?? 0 }
  );

  async ngOnInit(): Promise<void> {
    if (this.auth.isAdmin()) {
      this.error.set('Адміністратор не може подавати кредитні заявки.');
      return;
    }

    try {
      this.profile.set(await this.auth.getMe());
    } catch (e) {
      this.profileError.set(
        e instanceof Error ? e.message : 'Не вдалося завантажити профіль'
      );
    }

    this.origination.getCreateConfig().subscribe({
      next: (cfg) => {
        this.productCode.set(cfg.productCode);
        this.productVersion.set(cfg.productVersion);
        this.minPrincipal.set(cfg.minPrincipal);
        this.maxPrincipal.set(cfg.maxPrincipal);
        this.minTermDays.set(cfg.minTermDays);
        this.maxTermDays.set(cfg.maxTermDays);
        this.interestRatePerDay.set(cfg.interestRatePerDay);

        if (this.form.controls.requestedPrincipal.value == null) {
          this.form.controls.requestedPrincipal.setValue(cfg.minPrincipal);
        }
        if (this.form.controls.requestedTermDays.value == null) {
          this.form.controls.requestedTermDays.setValue(cfg.minTermDays);
        }

        this.form.controls.requestedTermDays.setValidators([
          Validators.required,
          Validators.min(cfg.minTermDays),
          Validators.max(cfg.maxTermDays),
        ]);
        this.form.controls.requestedPrincipal.setValidators([
          Validators.required,
          Validators.min(cfg.minPrincipal),
          Validators.max(cfg.maxPrincipal),
        ]);
        this.form.controls.requestedPrincipal.updateValueAndValidity();
        this.form.controls.requestedTermDays.updateValueAndValidity();
      },
      error: () => {
        this.form.controls.requestedPrincipal.setValue(this.minPrincipal());
        this.form.controls.requestedTermDays.setValue(this.minTermDays());
      },
    });
  }

  protected employmentLabel(code: string | null | undefined): string {
    if (!code) return '—';
    const m: Record<string, string> = {
      Employed: 'Працівник',
      SelfEmployed: 'ФОП / самозайнятий',
      Unemployed: 'Безробітний / без зайнятості',
      Student: 'Студент',
      Retired: 'Пенсіонер',
    };
    return m[code] ?? code;
  }

  protected async submit(): Promise<void> {
    this.error.set(null);
    if (this.form.invalid || this.pending()) {
      this.form.markAllAsTouched();
      return;
    }
    this.pending.set(true);
    const v = this.form.getRawValue();
    const requestedPrincipal = Number(v.requestedPrincipal);
    const requestedTermDays = Number(v.requestedTermDays);
    if (!Number.isFinite(requestedPrincipal) || !Number.isFinite(requestedTermDays)) {
      this.error.set('Некоректні параметри заявки.');
      this.pending.set(false);
      return;
    }
    this.origination
      .create({
        requestedPrincipal,
        requestedTermDays: Math.trunc(requestedTermDays),
        purpose: v.purpose || undefined,
      })
      .subscribe({
        next: (r) => {
          this.pending.set(false);
          void this.router.navigate(['/applications', r.applicationId]);
        },
        error: (e: Error) => {
          this.error.set(e.message);
          this.pending.set(false);
        },
      });
  }
}
