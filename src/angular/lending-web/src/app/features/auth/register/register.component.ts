import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    displayName: ['', Validators.maxLength(200)],
    dateOfBirth: ['', Validators.required],
    monthlyIncome: [''],
    employmentStatus: ['', Validators.required],
    taxIdMasked: ['', Validators.maxLength(32)],
    phoneNumber: ['', Validators.maxLength(20)],
  });

  protected async submit(): Promise<void> {
    this.error.set(null);
    if (this.form.invalid || this.pending()) {
      this.form.markAllAsTouched();
      return;
    }
    this.pending.set(true);
    try {
      const v = this.form.getRawValue();
      const inc = v.monthlyIncome?.toString().trim();
      const monthlyIncome =
        inc === '' || inc === undefined ? undefined : Number(inc);
      await this.auth.register({
        email: v.email,
        password: v.password,
        displayName: v.displayName || undefined,
        dateOfBirth: v.dateOfBirth,
        monthlyIncome:
          monthlyIncome === undefined || Number.isNaN(monthlyIncome)
            ? undefined
            : monthlyIncome,
        employmentStatus: v.employmentStatus,
        taxIdMasked: v.taxIdMasked || undefined,
        phoneNumber: v.phoneNumber?.trim() || undefined,
      });
      await this.router.navigateByUrl('/applications');
    } catch (e) {
      this.error.set(
        e instanceof Error ? e.message : 'Помилка реєстрації'
      );
    } finally {
      this.pending.set(false);
    }
  }
}
